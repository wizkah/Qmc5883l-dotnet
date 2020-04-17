using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Qmc5883l
{
    public class Qmc5883lCalibration
    {
        private readonly float FieldStrength_G;

        public Qmc5883lCalibration(float fieldStrength_G =1f) => FieldStrength_G = fieldStrength_G;

        Vector b = new DenseVector(new[] { 0f, 0, 0 });
        Matrix A_1 = (Matrix)Matrix.Build.DenseDiagonal(3, 3, 1f);

        public async Task Calibrate(Qmc5883l sensor, CancellationToken cancellationToken)
        {
            (b, A_1) = await CalibrationTask(sensor, cancellationToken, FieldStrength_G);
        }

        public Vector Correct(Vector d)
        {
            return correct(d, A_1, b);
        }

        private static Vector correct(Vector d, Matrix A_1, Vector b)
        {
            var corrected = (Vector)(A_1 * (d - b));
            return corrected;
        }

        private static Vector cross(Vector left, Vector right)
        {
            Vector result = new DenseVector(3);
            result[0] = left[1] * right[2] - left[2] * right[1];
            result[1] = -left[0] * right[2] + left[2] * right[0];
            result[2] = left[0] * right[1] - left[1] * right[0];

            return result;
        }

        private static async Task<(Vector b, Matrix A_1)>
        CalibrationTask(Qmc5883l sensor, CancellationToken cancellationToken, float FieldStrength_G)
        => await Task.Run(() => { return calibrate(sensor, cancellationToken, FieldStrength_G); });

        private static (Vector b, Matrix A_1) calibrate(Qmc5883l sensor, CancellationToken cancellationToken, float FieldStrength_G)
        {
            addDataToCalibrationMeasurementsUntilCanceled(sensor, out var calibrationMeasurements, cancellationToken);

            calculateCalibration(FieldStrength_G, calibrationMeasurements, out Vector b, out Matrix A_1);

            return (b: b, A_1: A_1);
        }

        private static void addDataToCalibrationMeasurementsUntilCanceled(Qmc5883l sensor, out List<Vector> calibrationMeasurements, CancellationToken cancellationToken)
        {
            var list = new List<Vector>();

            using (var subscription =
            sensor.Observable.Subscribe(onNext: (data) => list.Add(data)))
            {
                while (cancellationToken.IsCancellationRequested is false)
                {
                    Thread.Sleep(200);
                }
            }

            calibrationMeasurements = list;
        }

        private static void calculateCalibration(float F, List<Vector> sL, out Vector b, out Matrix A_1)
        {
            var s = Matrix.Build.DenseOfColumnVectors(sL);
            var (M, n, d) = ellipsoid_fit((Matrix)s);

            var M_1 = M.Inverse();
            b = (Vector)(-M_1 * n);
            var M_c = M.ToComplex32();
            var M_evd = M_c.Evd();
            var D = M_evd.D;
            var T = M_evd.EigenVectors;
            var sqrt_M = T * D.PointwiseSqrt() * T.Inverse();

            var A_1_c = F / ((n.ToRowMatrix() * M_1 * n - d).ToComplex32().PointwiseSqrt()[0]) * sqrt_M;
            A_1 = (Matrix)A_1_c.Real();
            Console.WriteLine($@"Calibration:");
            Console.WriteLine($@"b: {b.ToString()}");
            Console.WriteLine($@"A^-1: {A_1.ToString()}");
        }

        private static (Matrix M, MathNet.Numerics.LinearAlgebra.Single.Vector n, float d) ellipsoid_fit(Matrix s)
        {
            //     ''' Estimate ellipsoid parameters from a set of points.

            //     Parameters
            //     ----------
            //     s: array_like
            //      The samples(M, N) where M = 3(x, y, z) and N = number of samples.

            //     Returns
            //     ------ -
            //     M, n, d: array_like, array_like, float
            //      The ellipsoid parameters M, n, d.

            //    References
            //    ----------.. [1] Qingde Li; Griffiths, J.G., "Least squares ellipsoid specific
            //        fitting," in Geometric Modeling and Processing, 2004.
            //        Proceedings, vol., no., pp.335 - 340, 2004
            // '''

            var D = Matrix.Build.DenseOfRowVectors(
                s.Row(0).PointwiseMultiply(s.Row(0)),
                s.Row(1).PointwiseMultiply(s.Row(1)),
                s.Row(2).PointwiseMultiply(s.Row(2)),
                2f * s.Row(1).PointwiseMultiply(s.Row(2)),
                2f * s.Row(0).PointwiseMultiply(s.Row(2)),
                2f * s.Row(0).PointwiseMultiply(s.Row(1)),
                2f * s.Row(0),
                2f * s.Row(1),
                2f * s.Row(2),
                MathNet.Numerics.LinearAlgebra.Single.Vector.Build.DenseOfEnumerable(Enumerable.Repeat(1f, s.ColumnCount))
            );

            var S = D * D.Transpose();
            var S_11 = S.SubMatrix(0, 6, 0, 6);
            var S_12 = S.SubMatrix(0, 6, 6, 4);
            var S_21 = S.SubMatrix(6, 4, 0, 6);
            var S_22 = S.SubMatrix(6, 4, 6, 4);

            var C = Matrix.Build.DenseOfArray(new float[,] {{-1, 1, 1, 0, 0, 0},
                      { 1, -1,  1,  0,  0,  0},
                      { 1,  1, -1,  0,  0,  0},
                      { 0,  0,  0, -4,  0,  0},
                      { 0,  0,  0,  0, -4,  0},
                      { 0,  0,  0,  0,  0, -4}});

            var E = C.Inverse() * (S_11 - (S_12 * (S_22.Inverse() * S_12.Transpose())));

            var evd = E.Evd();
            var E_w = evd.EigenValues;
            var E_v = evd.EigenVectors;

            var maxValIndex = E_w.Real().MaximumIndex();
            var v_1 = E_v.Column(maxValIndex);

            if (v_1[0] < 0) v_1 = -v_1;

            var v_2 = -S_22.Inverse() * S_12.Transpose() * v_1;

            var M = (Matrix)Matrix.Build.DenseOfArray(
                new float[,]{
                    {v_1[0], v_1[3], v_1[4]},
                    {v_1[3], v_1[1], v_1[5]},
                    {v_1[4], v_1[5], v_1[2]},
                    });

            var n = (MathNet.Numerics.LinearAlgebra.Single.Vector)MathNet.Numerics.LinearAlgebra.Single.Vector.Build.DenseOfArray(
                new float[]
                {
                v_2[0], v_2[1], v_2[2]
                }
            );

            var d = v_2[3];

            return (M: M, n: n, d: d);
        }

    }
}

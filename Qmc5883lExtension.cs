using System;
using System.Collections.Generic;
using System.Device.I2c;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Qmc5883l
{
    public static class Qmc5883lExtension
    {

        public static double Declination(this Vector vector)
        {
            double deg = Math.Atan2(vector[1], vector[0]) * 180 / Math.PI;

            if (deg < 0)
            {
                deg += 360;
            }

            return deg;
        }

        private static double Inclination(this Vector vector)
        {
            Vector normal = (Vector)Vector.Build.DenseOfArray(new[] { 0, 0, 1f });

            var arg = Math.Abs(normal * vector.Normalize(2));
            double deg = Math.Asin(arg) * 180 / Math.PI;

            if (deg < 0)
            {
                deg += 360;
            }

            return deg;
        }

    }
}

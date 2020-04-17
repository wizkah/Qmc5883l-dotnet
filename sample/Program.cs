using System;
using System.Device.I2c;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Qmc5883l.Calibration
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            I2cConnectionSettings settings = new I2cConnectionSettings(1, Qmc5883l.DefaultI2cAddress);
            I2cDevice device = I2cDevice.Create(settings);

            using (Qmc5883l sensor = new Qmc5883l(device, Range.Range2))
            {
                CancellationTokenSource source = new CancellationTokenSource();
                var cancellationToken = source.Token;

                Qmc5883lCalibration calibration;

                using (var calibrationSubscription = sensor.Observable.Select((d, i) => (raw: d, count: i)).Subscribe(onNext: printout))
                {

                    var c = sensor.Calibrate(cancellationToken, 0.48599f);
                    var cancel = Console.ReadKey();
                    source.Cancel();
                    calibration = await c;
                }

                var compass = new Qmc5883lCompass(2f + 46f / 60f);

                using (var subscription = sensor.Observable
                .Select(d =>
                {
                    var corrected = calibration.Correct(d);
                    var compHeading = compass.CorrectedDeclination(corrected);
                    return (raw: d, corrected: corrected, compass: compHeading);
                })
                .Subscribe(onNext: printout))
                {
                    Console.ReadKey();
                }
            }
        }

        private static void printout((Vector raw, int count) obj)
        {
            Console.WriteLine($"Calibration #{obj.count}: {obj.raw.ToString()} \t\t {obj.raw.Declination()}");
            Console.WriteLine();
            Console.WriteLine();
        }

        private static void printout((Vector raw, Vector corrected, float compass) obj)
        {
            Console.WriteLine($"Raw: {obj.raw.ToString()} \t\t {obj.raw.Declination()}");
            Console.WriteLine($"Corrected: {obj.corrected.ToString()}\t {obj.corrected.Declination()}");
            Console.WriteLine($"Compass heading: {obj.compass.ToString("0.00")} °");
            Console.WriteLine();
            Console.WriteLine();
        }

        private static void onNext(Vector obj)
        {
            Console.WriteLine($"Heading: {obj.ToString()}");
        }
    }
}

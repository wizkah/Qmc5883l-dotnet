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
    public class Qmc5883lCompass
    {

        float Declination;
        public Qmc5883lCompass(float Declination)
        {
            this.Declination = Declination;
        }

        public float CorrectedDeclination(Vector vector)
        {
            float deg = (float)(Math.Atan2(vector[1], vector[0]) * 180 / Math.PI);
            deg += Declination;

            if (deg < 0)
            {
                deg += 360;
            }

            return deg;
        }
    }
}

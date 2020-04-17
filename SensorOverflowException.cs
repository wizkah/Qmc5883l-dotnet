using System;

namespace Qmc5883l
{
    public class SensorOverflowException : Exception
    {
        public SensorOverflowException() : base("Sensor overflow")
        {

        }
    }
}
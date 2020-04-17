using System;

namespace Qmc5883l
{
    public enum Mode : byte
    {
        Standby = 0b_0000_0000,
        Continuous = 0b_0000_0001,
    }
}
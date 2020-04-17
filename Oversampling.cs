using System;

namespace Qmc5883l
{
    public enum Oversampling : byte
    {
        Oversampling512 = 0b_0000_0000,
        Oversampling256 = 0b_0100_0000,
        Oversampling128 = 0b_1000_0000,
        Oversampling64 = 0b_1100_0000,
    }
}
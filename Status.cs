using System;

namespace Qmc5883l
{
    [Flags]
    public enum Status : byte
    {
        Data_Ready = 0b_0000_0001,
        Overlfow = 0b_0000_0010,
        Data_Skip = 0b_0000_0100

    }
}
using System;

namespace Qmc5883l
{
    public enum Range : byte
    {
        Range2 = 0b_0000_0000,
        Range8 = 0b_0001_0000,
    }
}
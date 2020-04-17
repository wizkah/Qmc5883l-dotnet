using System;

namespace Qmc5883l
{
    public enum OutputRate : byte
    {
        Rate10 = 0b_0000_0000,
        Rate50 = 0b_0000_0100,
        Rate100 = 0b_0000_1000,
        Rate200 = 0b_0000_1100,
    }
}
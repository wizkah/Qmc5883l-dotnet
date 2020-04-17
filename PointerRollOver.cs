using System;

namespace Qmc5883l
{
    [Flags]
    public enum PointerRollOver : byte
    {
        Enabled = 0b_0100_0000,
        Disabled = 0b_0000_0000,
    }
}
using System;

namespace Qmc5883l
{
    [Flags]
    public enum InterruptPin : byte
    {
        Enabled = 0b_0000_0000,
        Disabled = 0b_0000_0001,
    }
}
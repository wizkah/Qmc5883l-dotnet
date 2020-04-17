using System;

namespace Qmc5883l
{
    internal enum Register : byte
    {
        QMC_X_LSB_REG_ADDR = 0x00,
        QMC_Y_LSB_REG_ADDR = 0x02,
        QMC_Z_LSB_REG_ADDR = 0x04,
        QMC_STATUS_REG_ADDR = 0x06,
        QMC_T_LSB_REG_ADDR = 0x07,
        QMC_CONTROL_1_REG_ADDR = 0x09,
        QMC_CONTROL_2_REG_ADDR = 0x0a,
        QMC_SET_RESET_PERIOD_REG_ADDR = 0x0b,
        QMC_CHIP_ID_REG_ADDR = 0x0d,
    }
}

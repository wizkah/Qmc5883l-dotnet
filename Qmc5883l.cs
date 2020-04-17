using System;
using System.Buffers.Binary;
using System.Device.I2c;
using System.Threading;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Qmc5883l
{

    public class Qmc5883l : IDisposable
    {
        public const byte DefaultI2cAddress = 0x0D;
        private const byte SoftReset = 0b_1000_0000;

        private readonly byte _interruptPin;
        private readonly byte _outputRate;
        private readonly byte _oversampling;
        private readonly byte _pointerRollOver;
        private readonly byte _range;

        public bool Active { get; private set; } = false;


        private I2cDevice _i2cDevice;

        public Vector DirectionVector => ReadDirectionVector();

        public byte Rate => _outputRate;

        public Status DeviceStatus => GetStatus();

        public Qmc5883lObservable Observable
        {
            get
            {
                if (_observable != null)
                {
                    return _observable;
                }
                else
                {
                    _observable = new Qmc5883lObservable(this);
                    return _observable;
                };
            }
        }
        Qmc5883lObservable _observable;

        public Qmc5883l(
            I2cDevice i2CDevice,
            Range range = Range.Range2,
            OutputRate outputRate = OutputRate.Rate10,
            Oversampling oversampling = Oversampling.Oversampling512,
            InterruptPin interruptPin = InterruptPin.Disabled,
            PointerRollOver pointerRollOver = PointerRollOver.Disabled
        )
        {
            _i2cDevice = i2CDevice;
            _range = (byte)range;
            _outputRate = (byte)outputRate;
            _oversampling = (byte)oversampling;
            _interruptPin = (byte)interruptPin;
            _pointerRollOver = (byte)pointerRollOver;

            Initialize();
        }

        private void Initialize()
        {
            WakeUp();
        }

        public void WakeUp()
        {
            Span<byte> reset = stackalloc byte[]
            {
                (byte)Register.QMC_CONTROL_2_REG_ADDR,
                SoftReset
            };
            _i2cDevice.Write(reset);

            Span<byte> resetRegister = stackalloc byte[]
            {
                (byte)Register.QMC_SET_RESET_PERIOD_REG_ADDR,
                0x01
            };
            _i2cDevice.Write(resetRegister);

            Span<byte> control2 = stackalloc byte[]
            {
                (byte)Register.QMC_CONTROL_2_REG_ADDR,
                (byte)(_pointerRollOver | _interruptPin)
            };
            _i2cDevice.Write(control2);

            Span<byte> control1 = stackalloc byte[]
            {
                (byte)Register.QMC_CONTROL_1_REG_ADDR,
                (byte)((byte)Mode.Continuous | _range | _outputRate | _oversampling)
            };
            _i2cDevice.Write(control1);

            Active = true;
        }

        public Vector ReadDirectionVector()
        {
            Span<byte> xRead = stackalloc byte[2];
            Span<byte> yRead = stackalloc byte[2];
            Span<byte> zRead = stackalloc byte[2];

            _i2cDevice.WriteByte((byte)Register.QMC_X_LSB_REG_ADDR);
            _i2cDevice.Read(xRead);
            _i2cDevice.WriteByte((byte)Register.QMC_Y_LSB_REG_ADDR);
            _i2cDevice.Read(yRead);
            _i2cDevice.WriteByte((byte)Register.QMC_Z_LSB_REG_ADDR);
            _i2cDevice.Read(zRead);

            short x = BinaryPrimitives.ReadInt16LittleEndian(xRead);
            short y = BinaryPrimitives.ReadInt16LittleEndian(yRead);
            short z = BinaryPrimitives.ReadInt16LittleEndian(zRead);

            return new DenseVector(new float[] { x, y, z });
        }

        public Status GetStatus()
        {
            _i2cDevice.WriteByte((byte)Register.QMC_STATUS_REG_ADDR);
            byte status = _i2cDevice.ReadByte();

            return (Status)status;
        }

        public void Dispose()
        {
            if (_observable != null) _observable.Dispose();

            Standby();

            if (_i2cDevice != null)
            {
                _i2cDevice?.Dispose();
                _i2cDevice = null;
            }

        }

        public void Standby()
        {
            Span<byte> control1 = stackalloc byte[]
            {
                (byte)Register.QMC_CONTROL_1_REG_ADDR,
                (byte)Mode.Standby
            };
            _i2cDevice.Write(control1);

            Active = false;
        }

        public async Task<Qmc5883lCalibration> Calibrate(CancellationToken cancellationToken, float FieldStrength_G = 1)
        {
            var calib = new Qmc5883lCalibration(FieldStrength_G);
            await calib.Calibrate(this, cancellationToken);
            return calib;
        }
    }
}
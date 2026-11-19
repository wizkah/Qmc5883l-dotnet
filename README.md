# Qmc5883l-dotnet

[![Build](https://github.com/wizkah/Qmc5883l-dotnet/actions/workflows/build.yml/badge.svg)](https://github.com/wizkah/Qmc5883l-dotnet/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/github/license/wizkah/Qmc5883l-dotnet)](LICENSE)

.NET device binding for the **QMC5883L** 3-axis magnetometer (digital compass), plus tools for calibration and heading calculation.

- Uses `System.Device.I2c`, and follows the conventions of the `Iot.Device.*` bindings as closely as possible.
- Exposes the sensor as an `IObservable<Vector>` (Rx), so readings can be filtered and combined with LINQ.
- Includes a magnetometer calibration (hard/soft-iron correction) based on [this article by Teslabs](https://teslabs.com/articles/magnetometer-calibration/).
- Tested on a Raspberry Pi 4.

## Requirements

- A .NET runtime compatible with `netstandard2.0` (the library) and `System.Device.Gpio` 1.0.0.
- A QMC5883L breakout board wired to an I²C bus (default address `0x0D`).
- On Linux / Raspberry Pi: I²C enabled (`sudo raspi-config` → Interface Options → I2C).

## Build

```sh
dotnet build Qmc5883l-dotnet.sln -c Release
```

## Usage

```csharp
using System.Device.I2c;
using Qmc5883l;

var settings = new I2cConnectionSettings(busId: 1, Qmc5883l.Qmc5883l.DefaultI2cAddress);
using var sensor = new Qmc5883l.Qmc5883l(I2cDevice.Create(settings), Range.Range2);

var raw = sensor.ReadDirectionVector();   // MathNet Vector (x, y, z)
```

### Configuration

The constructor takes optional parameters; defaults in bold.

| Parameter        | Values                                                      |
|------------------|-------------------------------------------------------------|
| `range`          | **`Range2`** (±2 G), `Range8` (±8 G)                         |
| `outputRate`     | **`Rate10`**, `Rate50`, `Rate100`, `Rate200` (Hz)            |
| `oversampling`   | **`Oversampling512`**, `256`, `128`, `64`                    |
| `interruptPin`   | **`Disabled`**, enabled                                      |
| `pointerRollOver`| **`Disabled`**, enabled                                      |

### Streaming readings

```csharp
using (sensor.Observable.Subscribe(v => Console.WriteLine(v)))
{
    Console.ReadKey();
}
```

### Calibration and heading

Rotate the sensor through all orientations while calibrating, then apply the result to every reading:

```csharp
var cts = new CancellationTokenSource();
var calibrationTask = sensor.Calibrate(cts.Token, fieldStrength_G: 0.48599f); // local field strength
Console.ReadKey();                // keep rotating the sensor, press a key when done
cts.Cancel();
var calibration = await calibrationTask;

var compass = new Qmc5883lCompass(declination: 2f + 46f / 60f);  // local magnetic declination in degrees
var heading = compass.CorrectedDeclination(calibration.Correct(sensor.ReadDirectionVector()));
```

Local field strength and declination for your location can be looked up at the [NOAA magnetic field calculators](https://www.ngdc.noaa.gov/geomag/calculators/magcalc.shtml).

A complete, runnable example is in [`sample/`](sample/Program.cs).

## Contributing

Issues and pull requests are welcome. Please see [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE) © Alexander Benedix

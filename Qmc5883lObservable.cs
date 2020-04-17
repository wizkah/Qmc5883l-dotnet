using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Qmc5883l
{
    public class Qmc5883lObservable : IObservable<Vector>, IDisposable
    {

        Qmc5883l sensor;
        private List<IObserver<Vector>> observers = new List<IObserver<Vector>>();

        private Qmc5883lObservable()
        {
            Task.Run(CheckStatusAndReadVector);
        }
        public Qmc5883lObservable(Qmc5883l qmc5883lSensor) : this()
        {
            sensor = qmc5883lSensor;
        }

        IDisposable IObservable<Vector>.Subscribe(IObserver<Vector> observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
            return new Unsubscriber(observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<Vector>> _observers;
            private IObserver<Vector> _observer;

            public Unsubscriber(List<IObserver<Vector>> observers, IObserver<Vector> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }

        private void CheckStatusAndReadVector()
        {
            while (disposed is false)
            {
                if (observers.Any() && sensor != null && sensor.Active)
                {
                    var status = sensor.GetStatus();
                    if (status.HasFlag(Status.Overlfow))
                    {
                        throw new SensorOverflowException();
                    }
                    if (status.HasFlag(Status.Data_Skip))
                    {
                        var trash = sensor.ReadDirectionVector();
                        continue;
                    }
                    if (status.HasFlag(Status.Data_Ready))
                    {
                        var data = sensor.ReadDirectionVector();
                        Task.Run(
                            () =>
                            observers.ForEach(
                                o => o.OnNext(data)));

                    }
                    Thread.Sleep(GetSleepTime(sensor.Rate));
                }
                else
                {
                    Thread.Sleep(2000);
                }
            }
            canDispose = true;
        }

        private static int GetSleepTime(byte _outputRate)
        {
            switch ((OutputRate)_outputRate)
            {
                default:
                case OutputRate.Rate10:
                    return 100;
                case OutputRate.Rate50:
                    return 20;
                case OutputRate.Rate100:
                    return 10;
                case OutputRate.Rate200:
                    return 5;
            }
        }

        public void Dispose()
        {
            disposed = true;

            while(canDispose is false)
            {
                Thread.Sleep(50);
            }

            foreach (var observer in observers)
                observer.OnCompleted();

            observers.Clear();

        }

        bool disposed = false;
        bool canDispose = false;
    }
}
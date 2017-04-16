using AdafruitClassLibrary;
using RaspiMidiUwp.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RaspiMidiUwp.Utilities
{
    public class ObservableMcp23017 : Mcp23017, IObservable<ushort>
    {
        public ObservableMcp23017(int[] readPins)
        {
            observers = new List<IObserver<ushort>>();
            this.readPins = readPins;
        }

        private Timer timer;
        const int TIMER_PERIOD = 125;
        const ushort RESET_VALUE = 65535; // all pins on (rarely going to happen, if ever)

        private ushort _pinStates;
        private int[] readPins;

        #region Initialization

        public async new Task InitMCP23017Async(I2CSpeed i2cSpeed = I2CSpeed.I2C_100kHz)
        {
            await base.InitMCP23017Async(i2cSpeed);
            for (int i = 0; i < 16; i++)
            {
                //int ndx = readPins[i];
                //this.pinMode(ndx, Direction.INPUT);
                this.pinMode(i, Direction.INPUT);
            }

            _pinStates = RESET_VALUE;
        }

        #endregion

        #region Timer/Polling

        public void StartTimer(int delay = TIMER_PERIOD)
        {
            if (timer == null)
                timer = new Timer(HandleTimer, null, 0, TIMER_PERIOD);
            else
                timer.Change(0, delay);
        }

        public void StopTimer()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        public void HandleTimer(object state)
        {
            ushort currentStates = this.readGPIOAB();
            // compare our new values with our previous

            if (_pinStates != currentStates)
            {
                // something changed, notify our observers
                foreach (var observer in observers)
                {
                    observer.OnNext(currentStates);
                }

                _pinStates = currentStates;
            }

        }

        #endregion

        #region IObservable

        private List<IObserver<ushort>> observers;

        public IDisposable Subscribe(IObserver<ushort> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);
            return new Unsubscriber(observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<ushort>> _observers;
            private IObserver<ushort> _observer;

            public Unsubscriber(List<IObserver<ushort>> observers, IObserver<ushort> observer)
            {
                _observers = observers;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }

        #endregion

        #region Methods

        public void EndTransmission()
        {
            foreach (var observer in observers.ToArray())
                if (observers.Contains(observer))
                    observer.OnCompleted();

            observers.Clear();
        }

        #endregion
    }
}

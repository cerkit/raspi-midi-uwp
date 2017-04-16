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
    public class ObservableMcp23017 : Mcp23017, IObservable<McpPinState>
    {
        public ObservableMcp23017(int[] readPins)
        {
            observers = new List<IObserver<McpPinState>>();
            this.readPins = readPins;
        }

        private Timer timer;
        const int TIMER_PERIOD = 125;

        private McpPinState[] pinStates;
        private int[] readPins;

        #region Initialization

        public async new Task InitMCP23017Async(I2CSpeed i2cSpeed = I2CSpeed.I2C_100kHz)
        {
            await base.InitMCP23017Async(i2cSpeed);
            for (int i = 0; i < readPins.Length; i++)
            {
                int ndx = readPins[i];
                this.pinMode(ndx, Direction.INPUT);
                this.pullUp(ndx, Level.LOW);
            }

            pinStates = GetPinStates();
        }

        #endregion

        #region Timer/Polling (ugh)

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
            var currentStates = GetPinStates();

            // compare our new values with our previous

            // loop through each pin to see what changed
            for (int i = 0; i < pinStates.Length; i++)
            {
                if (pinStates[i].Level != currentStates[i].Level)
                {
                    // something changed, notify our observers
                    foreach (var observer in observers)
                    {
                        observer.OnNext(currentStates[i]);
                    }
                }
            }
        }

        #endregion

        #region IObservable

        private List<IObserver<McpPinState>> observers;

        public IDisposable Subscribe(IObserver<McpPinState> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);
            return new Unsubscriber(observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<McpPinState>> _observers;
            private IObserver<McpPinState> _observer;

            public Unsubscriber(List<IObserver<McpPinState>> observers, IObserver<McpPinState> observer)
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

        private McpPinState[] GetPinStates()
        {
            McpPinState[] states = { };

            try
            {
                if (timer != null)
                {

                    for (int i = 0; i < readPins.Length; i++)
                    {
                        // only read in the pins that are set to be read
                        int ndx = readPins[i];
                        states[ndx].Pin = readPins[ndx];
                        states[ndx].Level = this.digitalRead(ndx);
                    }

                }
            }
            catch (Exception)
            {
                //throw;
            }

            return states;
        }

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

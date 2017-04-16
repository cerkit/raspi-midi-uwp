using AdafruitClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RaspiMidiUwp.Utilities
{
    public class Mcp23017Notifier : Mcp23017
    {
        #region Constructors

        public Mcp23017Notifier()
        {

        }

        #endregion

        #region Fields

        private ushort _previousState;
        private Timer timer;
        const int TIMER_PERIOD = 125;

        public event EventHandler<PinChangedEventArgs> PinChanged;


        #endregion

        #region Timer

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
            PinChangedEventArgs args = new PinChangedEventArgs { PinState = this.readGPIOAB() };
            PinChanged(this, args);
        }

        #endregion

    }

    public class PinChangedEventArgs : EventArgs
    {
        public ushort PinState { get; set; }
    }
}

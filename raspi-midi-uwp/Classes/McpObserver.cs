using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaspiMidiUwp.Classes
{
    public class McpObserver : IObserver<ushort>
    {
        private IDisposable unsubscriber;
        private string instName;

        public McpObserver(string name)
        {
            instName = name;
        }

        public event EventHandler<PinChangedEventArgs> PinChanged;

        public string Name
        { get { return instName; } }

        public virtual void Subscribe(IObservable<ushort> provider)
        {
            if (provider != null)
                unsubscriber = provider.Subscribe(this);
        }

        public void OnCompleted()
        {
            Unsubscribe();
        }

        public void OnError(Exception error)
        {
            throw error;
        }

        public void OnNext(ushort value)
        {
            var args = new PinChangedEventArgs { PinState = value };
            
            PinChanged(this, args);
        }

        public virtual void Unsubscribe()
        {
            unsubscriber.Dispose();
        }
    }

    public class PinChangedEventArgs : EventArgs
    {
        public ushort PinState { get; set; }
    }
}

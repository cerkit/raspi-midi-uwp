using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaspiMidiUwp.Classes
{
    public class MidiMessage
    {
        public uint MsgChannel { get; set; }
        public uint Velocity { get; set; }
        public uint Note { get; set; }
    }
}

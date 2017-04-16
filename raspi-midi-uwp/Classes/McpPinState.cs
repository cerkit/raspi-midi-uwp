using AdafruitClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaspiMidiUwp.Classes
{
    public class McpPinState
    {
        public int Pin { get; set; }
        public Mcp23017.Level Level { get; set; }
    }
}

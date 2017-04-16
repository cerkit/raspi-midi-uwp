using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaspiMidiUwp.Classes
{
    public class MissingPinStatesException : Exception
    {
        internal MissingPinStatesException()
            : base("There were no read pins provided.")
        {

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongSyllabicator
{
    public class BreakLine
    {
        public int BreakBeat { get; set; }
        public override string ToString()
        {
            return "- " + BreakBeat;
        }

        public BreakLine(int breakBeat)
        {
            BreakBeat = breakBeat;
        }
    }
}

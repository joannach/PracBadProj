using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjTransakcje
{
    public class Wynik
    {
        public bool w_konflikt { get; set; }
        public TimeSpan w_opoznienie { get; set; }
        public DateTime w_start { get; set; }
        public DateTime w_stop { get; set; }

        public Wynik(bool konflikt, TimeSpan opoznienie, DateTime start, DateTime stop)
        {
            w_konflikt = konflikt;
            w_opoznienie = opoznienie;
            w_start = start;
            w_stop = stop;
        }

        public Wynik() { }

    }
}

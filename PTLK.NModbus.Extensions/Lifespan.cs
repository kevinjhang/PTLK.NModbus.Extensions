using System;

namespace PTLK.NModbus.Extensions
{
    class Lifespan
    {
        public Lifespan(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models
{
    public class EventFee
    {
        public Guid EventID { get; set; }

        public bool Child { get; set; }

        public int MaxAge { get; set; }

        public int MaxGrade { get; set; }

        public char Gender { get; set; }


        public string Group { get; set; }

        public decimal Cost { get; set; }

        public string Description { get; set; }
    }
}

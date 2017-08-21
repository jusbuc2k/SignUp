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

        public int MinAge { get; set; }

        public int MaxAge { get; set; }

        public string AgeUnit { get; set; }

        public DateTime? AgeCutoff { get; set; }

        public int MinGrade { get; set; }

        public int MaxGrade { get; set; }

        public string Gender { get; set; }

        public bool ApplyAgeFilter { get; set; }

        public bool ApplyGradeFilter { get; set; }


        public string Group { get; set; }

        public decimal Cost { get; set; }

        public string Description { get; set; }

        public string PcoGroupFieldValue { get; set; }
    }
}

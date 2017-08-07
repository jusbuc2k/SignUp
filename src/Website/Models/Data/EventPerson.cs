using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Data
{
    public class EventPerson : Person
    {
        public Guid EventID { get; set; }

        public string PersonID { get; set; }
        
        public string HouseholdID { get; set; }
    }
}

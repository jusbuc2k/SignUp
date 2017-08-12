using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models
{
    public class SaveChangesModel
    {
        public IEnumerable<Person> People { get; set; }

        public string HouseholdID { get; set; }

        public string HouseholdName { get; set; }

        public Guid EventID { get; set; }


        public IEnumerable<string> Identifiers { get; set; }

        public string Signature { get; set; }
    }
}

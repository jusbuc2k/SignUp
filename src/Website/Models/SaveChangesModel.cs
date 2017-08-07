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

        public bool IsNew { get; set; }
    }
}

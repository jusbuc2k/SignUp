using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Pco
{
    public class PeopleHouseholdDetail : PcoPeopleHousehold
    {
        public IEnumerable<PcoDataRecord<PcoPeoplePersonDetail>> PersonList { get; set; }

    }
}

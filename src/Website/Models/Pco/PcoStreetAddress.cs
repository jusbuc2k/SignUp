using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Pco
{
    public class PcoStreetAddress
    {
        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }

        public string Street { get; set; }

        public string Location { get; set; }

        public bool Primary { get; set; }
    }
}

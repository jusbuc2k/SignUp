using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Pco
{
    public class PcoPhoneNumber
    {
        public string Location { get; set; }

        public string Number { get; set; }
        
        public bool Primary { get; set; }
    }
}

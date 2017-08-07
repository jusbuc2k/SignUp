using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models
{
    public class SendLoginCodeModel
    {
        public string ContactMethod { get; set; }

        public string ContactAddress { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models
{
    public class VerifyLoginTokenModel
    {
        public Guid TokenID { get; set; }

        public string Token { get; set; }
    }
}

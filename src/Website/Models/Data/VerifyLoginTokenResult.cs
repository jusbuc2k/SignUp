using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Data
{
    public class VerifyLoginTokenResult : LoginToken
    {
        public bool Success { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Data
{
    public class LoginToken
    {
        public Guid TokenID { get; set; }

        public string EmailAddress { get; set; }

        public string Token { get; set; }

        public DateTimeOffset ExpiresDateTime { get; set; }

        public int BadAttemptCount { get; set; }

        public string PersonID { get; set; }
    }
}

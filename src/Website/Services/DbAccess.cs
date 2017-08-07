using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Registration.Models.Data;

namespace Registration.Services
{
    public class DbAccess : IDataAccess
    {
        public Task<LoginToken> CreateLoginToken(string emailAddress, string personID)
        {
            return Task.FromResult(new LoginToken()
            {
                TokenID = Guid.NewGuid(),
                Token = "111"
            });
        }

        public Task<VerifyLoginTokenResult> VerifyLoginToken(Guid tokenID, string token)
        {
            return Task.FromResult(new VerifyLoginTokenResult()
            {
                Success = true,
                Token = "111",
                PersonID = "19147425",
                EmailAddress = "justin@buchmail.com"
            });
        }
    }
}

using Registration.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Services
{
    public interface IDataAccess
    {
        Task<LoginToken> CreateLoginToken(string emailAddress, string personID);

        Task<VerifyLoginTokenResult> VerifyLoginToken(Guid tokenID, string token);
    }
}

using Registration.Models;
using Registration.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Services
{
    public interface IDataAccess
    {
        Task<IEnumerable<EventModel>> GetEventList();

        Task<EventModel> GetEvent(Guid eventID);

        Task<LoginToken> CreateLoginToken(string emailAddress, string personID);

        Task<VerifyLoginTokenResult> VerifyLoginToken(Guid tokenID, string token);

        Task CreateEventPerson(EventPerson person);

        Task<EventPerson> GetEventPerson(Guid eventID, string personID);
    }
}

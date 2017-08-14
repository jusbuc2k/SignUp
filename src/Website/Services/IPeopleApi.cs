using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Registration.Models;
using Registration.Models.Pco;

namespace Registration.Services
{
    public interface IPeopleApi
    {
        Task<PcoDataRecord<PcoPeoplePerson>> FindPersonByEmail(string emailAddress);

        Task<PcoListResponse<PcoPeopleHousehold>> GetPersonHouseholds(string personID);

        Task<PcoSingleResponse<PcoPeopleHousehold>> GetHousehold(string id, bool includePeople = false);

        Task<PcoListResponse<PcoEmailAddress>> GetEmailsForPerson(string personID);

        Task<PcoListResponse<PcoPhoneNumber>> GetPhonesForPerson(string personID);

        Task<PcoListResponse<PcoStreetAddress>> GetAddressesForPerson(string personID);

        Task<string> CreatePerson(PcoPeoplePerson person);

        Task<bool> UpdatePerson(string id, PcoPeoplePerson person);

        Task<PcoDataRecord<PcoEmailAddress>> CreateOrUpdateEmail(string personID, PcoDataRecord<PcoEmailAddress> emailAddress);

        Task<PcoDataRecord<PcoPhoneNumber>> CreateOrUpdatePhone(string personID, PcoDataRecord<PcoPhoneNumber> phoneNumber);

        Task<PcoDataRecord<PcoStreetAddress>> CreateOrUpdateAddress(string personID, PcoDataRecord<PcoStreetAddress> streetAddress);

        Task<string> CreateHousehold(string name, string primaryContactID, IEnumerable<string> memberIDs);

        Task UpdateHousehold(string id, string name, string primaryContactID);

        Task AddPersonToHousehold(string householdID, string personID);

        Task<PcoListResponse<PcoStreetAddress>> FindAddressByZipCode(string zip);

        //Task AddFieldDatum(string id, PcoFieldDatum dataum);
    }
}

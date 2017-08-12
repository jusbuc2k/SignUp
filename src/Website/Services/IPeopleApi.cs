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

        Task<PcoListResponse<PcoPeopleHousehold>> FindHouseholds(string personID);

        Task<PcoSingleResponse<PcoPeopleHousehold>> GetHousehold(string id, bool includePeople = false);

        Task<PcoListResponse<PcoEmailAddress>> GetEmailsForPerson(string personID);

        Task<PcoListResponse<PcoPhoneNumber>> GetPhonesForPerson(string personID);

        Task<PcoListResponse<PcoStreetAddress>> GetAddressesForPerson(string personID);

        Task<string> CreatePerson(PcoPeoplePerson person);

        Task<bool> UpdatePerson(string id, PcoPeoplePerson person);

        Task<PcoDataRecord<PcoEmailAddress>> AddOrUpdateEmail(string personID, PcoDataRecord<PcoEmailAddress> emailAddress);

        Task<PcoDataRecord<PcoPhoneNumber>> AddOrUpdatePhone(string personID, PcoDataRecord<PcoPhoneNumber> phoneNumber);

        Task<PcoDataRecord<PcoStreetAddress>> AddOrUpdateAddress(string personID, PcoDataRecord<PcoStreetAddress> streetAddress);

        Task<string> CreateHousehold(string name, string primaryContactID, IEnumerable<string> memberIDs);

        Task UpdateHousehold(string id, string name, string primaryContactID);

        Task AddToHousehold(string householdID, string personID);

        Task<PcoListResponse<PcoStreetAddress>> FindAddressByZipCode(string zip);

        //Task<IEnumerable<PcoDataRecord<PcoPeopleHousehold>>> GetHouseholdByEmail(string emailAddress);

        //Task<PcoDataRecord<PcoPeoplePerson>> CreatePerson(PcoPeoplePerson person, PcoDataRecord<PcoEmailAddress> emailAddress = null, PcoDataRecord<PhoneNumber> phoneNumber = null, PcoDataRecord<StreetAddress> address = null);

        //Task<PcoDataRecord<PcoPeopleHousehold>> CreateHousehold(PcoPeopleHousehold householdAttributes, IEnumerable<PcoDataRecord<PcoPeoplePerson>> members);

        //Task<PcoDataRecord<PcoEmailAddress>> GetEmailAddress(string address);

        //Task<PcoDataRecord<PcoPhoneNumber>> GetPhoneNumber(string phoneNumber);

        //Task<PcoDataRecord<PcoEmailAddress>> CreateEmailAddress(PcoEmailAddress attributes);

        //Task<PcoDataRecord<PcoPhoneNumber>> CreatePhoneNumber(PcoPhoneNumber attributes);

        //Task<PcoListResponse<PcoPeoplePerson>> FindPerson(string firstName = null, string lastName = null, bool? child = null, string gender = null, DateTime? birthDate = null);

        //Task<PcoDataRecord<PcoPeoplePerson>> GetPerson(string id);

        //Task<PcoDataRecord<PeopleHousehold>> GetHousehold(string householdID);

        //Task<IEnumerable<PcoDataRecord<PeoplePerson>>> GetHouseholdPersonList(string householdID);

        //Task UpdatePerson(string personID, PeoplePerson updatedValues);

        //Task<PcoDataRecord<PeoplePerson>> AddPerson(PeoplePerson person);

        //Task AddToHousehold(string householdID, string personID);

        //Task RemoveFromHoushold(string householdID, string personID);

        //Task UpdatePrimaryContact(string householdID, string primaryContactID);

        //Task<IEnumerable<PeoplePersonDetail>> GetHouseholdPersonListDetail(string householdID);

        //Task<IEnumerable<PcoDataRecord<PeopleHousehold>>> GetPersonHouseholdList(string personID);

        //Task<IEnumerable<PcoDataRecord<PhoneNumber>>> GetPersonPhoneNumberList(string personID);

        //Task<IEnumerable<PcoDataRecord<EmailAddress>>> GetPersonEmailList(string personID);

        //Task<IEnumerable<PcoDataRecord<StreetAddress>>> GetPersonAddressList(string personID);
    }
}

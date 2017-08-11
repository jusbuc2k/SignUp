using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Registration.Models;
using WebApplicationBasic;
using Registration.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Registration.Models.Pco;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Registration.Models.Data;

namespace WebApplicationBasic.Controllers
{

    public class HomeController : Controller
    {
        private readonly IMessageService _messageService;
        private readonly IPeopleApi _people;
        private readonly IDataAccess _db;
        private readonly IMemoryCache _cache;
        private readonly SiteOptions _options;

        public HomeController(IMessageService messageService, IPeopleApi people, IDataAccess db, IOptions<SiteOptions> options, IMemoryCache cache)
        {
            _options = options.Value;
            _messageService = messageService;
            _people = people;
            _db = db;
            _cache = cache;
        }

        private async Task<PcoDataRecord<PcoPeopleHousehold>> GetPrimaryHouse(string personID)
        {
            var houses = await _people.FindHouseholds(personID);
            var primaryHouse = houses.Data.FirstOrDefault(x => x.Attributes.PrimaryContactID == personID);

            if (primaryHouse == null && houses.Data.Count == 1)
            {
                primaryHouse = houses.Data.FirstOrDefault();
            }

            if (primaryHouse != null)
            {
                return primaryHouse;
            }
            else
            {
                return null;
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        [Route("api/Event")]
        public Task<IEnumerable<EventModel>> GetEvents()
        {
            return _db.GetEventList();
        }

        [Route("api/Event/{id}")]
        public Task<EventModel> GetEvent(Guid id)
        {
            return _db.GetEvent(id);
        }

        [HttpPost]
        [Route("api/FindEmail")]
        public async Task<IActionResult> FindEmail([FromBody]FindEmailModel model)
        {
            if (!ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            var person = await _people.FindPersonByEmail(model.EmailAddress);
            
            if (person == null)
            {
                return this.NotFound();
            }

            if (this.User.Identity.IsAuthenticated)
            {
                if (this.User.HasClaim(x => x.Type == ClaimTypes.Email && x.Value.Equals(model.EmailAddress, StringComparison.OrdinalIgnoreCase)))
                {
                    var house = await this.GetPrimaryHouse(this.User.Identity.Name);

                    if (house == null)
                    {
                        throw new Exception("There is no household associated with your account.");
                    }

                    return this.Ok(new {
                        Verified = true,
                        HouseholdID = house.ID
                    });
                }
            }

            var token = await _db.CreateLoginToken(model.EmailAddress, person.ID);

            await _messageService.SendMessageAsync(model.EmailAddress, "Your Verification Code", $"Please use the code {token.Token} to verify your identity with {_options.Name}.");

            return this.Ok(new
            {
                TokenID = token.TokenID
            });
        }

        [HttpPost]
        [Route("api/VerifyLoginToken")]
        public async Task<IActionResult> VerifyLoginToken([FromBody]VerifyLoginTokenModel model)
        { 
            var result = await  _db.VerifyLoginToken(model.TokenID, model.Token);

            if (result.Success)
            {
                var identity = new System.Security.Claims.ClaimsIdentity("LoginToken");

                identity.AddClaim(new Claim(ClaimTypes.Name, result.PersonID));
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, result.PersonID));
                identity.AddClaim(new Claim(ClaimTypes.Email, result.EmailAddress));

                var principal = new System.Security.Claims.ClaimsPrincipal(identity);

                await this.HttpContext.Authentication.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, principal);

                var primaryHouse = await this.GetPrimaryHouse(result.PersonID);

                if (primaryHouse != null)
                {
                    return this.Ok(new
                    {
                        PersonID = result.PersonID,
                        EmailAddress = result.EmailAddress,
                        HouseholdID = primaryHouse.ID
                    });
                }
                else
                {
                    throw new Exception("There is no household associated with your account.");
                }              
            }

            return this.BadRequest();
        }

        [Authorize]
        [Route("api/Household/{id}")]
        public async Task<IActionResult> GetHousehold(string id)
        {
            if (!await this.CurrentUserHasAccessToHousehold(id))
            {
                return this.StatusCode(404);
            }

            var house = await _people.GetHousehold(id, includePeople: true);
            var ids = new List<string>();

            ids.Add(house.Data.ID);

            var people = house.GetRelated<PcoPeoplePerson>("people")
                .Select(s => new Person()
                {
                    ID = s.ID,
                    FirstName = s.Attributes.FirstName,
                    LastName = s.Attributes.LastName,
                    BirthDate = s.Attributes.BirthDate,
                    Child = s.Attributes.Child,
                    Gender = s.Attributes.Gender,
                    Grade = s.Attributes.Grade,
                    IsPrimaryContact = (house.Data.Attributes.PrimaryContactID == s.ID),
                    MedicalNotes = s.Attributes.MedicalNotes
                })
                .ToList();

            foreach (var person in people)
            {
                ids.Add(person.ID);

                if (person.Child)
                {
                    continue;
                }

                var emails = await _people.GetEmailsForPerson(person.ID);
                var phones = await _people.GetPhonesForPerson(person.ID);
                var addresses = await _people.GetAddressesForPerson(person.ID);
                var address = addresses.Data.Count == 1 ? addresses.Data.FirstOrDefault() : addresses.Data.FirstOrDefault(x => x.Attributes.Primary);
                var email = emails.Data.Count == 1 ? emails.Data.FirstOrDefault() : emails.Data.FirstOrDefault(x => x.Attributes.Primary);
                var phone = phones.Data.Count == 1 ? phones.Data.FirstOrDefault() : phones.Data.FirstOrDefault(x => x.Attributes.Location == "Mobile");
                
                if (address != null)
                {
                    person.Street = address.Attributes.Street;
                    person.City = address.Attributes.City;
                    person.State = address.Attributes.State;
                    person.Zip = address.Attributes.Zip;
                    person.AddressID = address.ID;
                    ids.Add(address.ID);
                }

                if (phone != null)
                {
                    person.PhoneNumber = phone.Attributes.Number;
                    person.PhoneNumberID = phone.ID;
                    ids.Add(phone.ID);
                }

                if (email != null)
                {
                    person.EmailAddress = email.Attributes.Address;
                    person.EmailAddressID = email.ID;
                    ids.Add(email.ID);
                }
            }

            // compute hash so we don't have to verify with the API every time we need to know
            // if the user has access to the personID/householdID they want to update.
            var hash = this.GenerateIdentifierHash(ids);

            return this.Ok(new
            {
                Signature = hash,
                Identifiers = ids,
                HouseholdID = house.Data.ID,
                PrimaryContactID = house.Data.Attributes.PrimaryContactID,
                People = people
            });
        }

        [HttpPost]
        [Route("api/CompleteRegistration")]
        public async Task<IActionResult> CompleteRegistration([FromBody]SaveChangesModel model)
        {
            string householdID;
            var primary = model.People.Single(x => x.IsPrimaryContact);
            
            if (model.IsNew)
            {
                foreach (var person in model.People)
                {
                    var pcoPerson = new PcoPeoplePerson();

                    person.CopyToPcoPerson(pcoPerson);

                    person.ID = await _people.CreatePerson(pcoPerson, primary.EmailAddress, primary.PhoneNumber);
                }

                householdID = await _people.CreateHousehold(primary.LastName, primary.ID, model.People.Select(s => s.ID));
            }
            else
            {
                if (!this.User.Identity.IsAuthenticated)
                {
                    return this.StatusCode(403, "User Not Authenticated");    
                }

                if (!this.VerifyIdentifierHash(model.Identifiers, model.Signature))
                {
                    return this.StatusCode(403, "Invalid signature");
                }

                //var house = await _people.GetHousehold(model.HouseholdID, includePeople: true);
                //var people = house.GetRelated<PcoPeoplePerson>("people");
                householdID = model.HouseholdID;

                foreach (var updatedPerson in model.People)
                {
                    //var existingPerson = people.SingleOrDefault(x => x.ID == updatedPerson.ID);
                    PcoPeoplePerson pcoPerson;

                    pcoPerson = new PcoPeoplePerson();                   

                    updatedPerson.CopyToPcoPerson(pcoPerson);

                    if (updatedPerson.ID == null)
                    {
                        updatedPerson.ID = await _people.CreatePerson(pcoPerson, updatedPerson.EmailAddress, updatedPerson.PhoneNumber);
                        await _people.AddToHousehold(model.HouseholdID, updatedPerson.ID);
                    }
                    else
                    {
                        if (!model.Identifiers.Contains(updatedPerson.ID))
                        {
                            throw new Exception($"An invalid attempt to update a PersonID {updatedPerson.ID} not in the identifier list was detected.");
                        }

                        await _people.UpdatePerson(updatedPerson.ID, pcoPerson, updatedPerson.EmailAddress, updatedPerson.PhoneNumber);
                    }
                    
                    //TODO: Create/Update Email, Phone, Street
                    //TODO: Check Email, Phone, Street ID's against id list
                }

                //TODO: Update household primary contact?
                //_people.UpdateHouseholdPrimaryContact(model.HouseholdID, primary.ID);
            }

            foreach (var person in model.People)
            {
                await _db.CreateEventPerson(new EventPerson()
                {
                    ID = person.ID,
                    EventID = model.EventID,
                    HouseholdID = model.HouseholdID,
                    FirstName = person.FirstName,
                    LastName = person.LastName,
                    Child = person.Child,
                    PhoneNumber = person.PhoneNumber,
                    EmailAddress = person.EmailAddress,
                    Street = person.Street,
                    City = person.City,
                    State = person.State,
                    Zip = person.Zip,
                    Grade = person.Grade,
                    BirthDate = person.BirthDate,
                    MedicalNotes = person.MedicalNotes,
                    Gender = person.Gender
                });
            }

            return this.NoContent();
        }

        [Route("api/Zip/{zip}")]
        public async Task<IActionResult> GetZipLocation(string zip)
        {
            string cacheKey = $"Zip:${zip}";
            PcoStreetAddress address;

            if (!_cache.TryGetValue(cacheKey, out address))
            {
                var matches = await _people.FindAddressByZipCode(zip);
                if (matches.Data.Count > 0)
                {
                    // Get the city/state most used with this zip code since some are wrong.
                    address = matches.Data.GroupBy(o => o.Attributes.City).OrderByDescending(o => o.Count()).First().First().Attributes;
                    _cache.Set(cacheKey, address, DateTimeOffset.Now.AddHours(24));
                }
            }

            if (address != null)
            {
                return this.Ok(new
                {
                    City = address.City,
                    State = address.State
                });
            }

            return this.NotFound();
        }

        //[HttpPost]
        //public async Task<IActionResult> CreateHousehold(SaveChangesModel model)
        //{
        //    var primaryContact = new PcoPeoplePerson();
        //    var primaryPerson = model.People.Single(x => x.IsPrimaryContact);

        //    primaryPerson.CopyToPcoPerson(primaryContact);

        //    var personID = await _people.CreatePerson(primaryContact, primaryPerson.EmailAddress, primaryPerson.PhoneNumber);

        //    var householdID = await _people.CreateHousehold(personID);

        //    foreach (var person in model.People)
        //    {
        //        var newPerson = new PcoPeoplePerson();
        //        person.CopyToPcoPerson(newPerson);

        //        var newPersonID = await _people.CreatePerson(newPerson, person.EmailAddress, person.PhoneNumber);

        //        await _people.AddToHousehold(householdID, personID);
        //    }

        //    var identity = new System.Security.Claims.ClaimsIdentity("LoginToken");

        //    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, personID));
        //    identity.AddClaim(new Claim(ClaimTypes.Email, primaryPerson.EmailAddress));

        //    var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        //    await this.HttpContext.Authentication.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, principal);

        //    return this.NoContent();
        //}

        //[Authorize]
        //[HttpPost]
        //public async Task<IActionResult> UpdateHousehold(SaveChangesModel model)
        //{
        //    var house = await _people.GetHousehold(model.HouseholdID, includePeople: true);
        //    var people = house.GetRelated<PcoPeoplePerson>("people");

        //    // Ensure I'm in the household in order to save changes
        //    if (!people.Any(x => x.ID == this.User.Identity.Name))
        //    {
        //        return this.StatusCode(403);
        //    }

        //    // Add new people who are not in the existing set
        //    foreach (var updatedPerson in model.People)
        //    {
        //        var existingPerson = people.SingleOrDefault(x => x.ID == updatedPerson.ID);

        //        if (existingPerson == null)
        //        {
        //            existingPerson = new PcoDataRecord<PcoPeoplePerson>()
        //            {
        //                Attributes = new PcoPeoplePerson()
        //            };

        //            updatedPerson.CopyToPcoPerson(existingPerson.Attributes);

        //            var newPersonID = _people.CreatePerson(existingPerson.Attributes, updatedPerson.EmailAddress, updatedPerson.PhoneNumber);
        //        }

        //        updatedPerson.CopyToPcoPerson(existingPerson.Attributes);

        //        await _people.UpdatePerson(existingPerson.ID, existingPerson.Attributes, updatedPerson.EmailAddress, updatedPerson.PhoneNumber);

        //        await _people.AddToHousehold(model.HouseholdID, existingPerson.ID);
        //    }

        //    return this.Ok();
        //}

        private async Task<bool> CurrentUserHasAccessToHousehold(string householdID)
        {
            var personID = this.User.Identity.Name;
            var houses = await _people.FindHouseholds(personID);

            if (!houses.Data.Any(x => x.ID == householdID))
            {
                return false;
            }

            return true;
        }

        private string GenerateIdentifierHash(IEnumerable<string> ids)
        {
            var hasher = System.Security.Cryptography.SHA256.Create();
            var hashMessage = string.Join(",", ids.OrderBy(o => o));

            return Convert.ToBase64String(hasher.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(hashMessage)));
        }

        private bool VerifyIdentifierHash(IEnumerable<string> ids, string providedHash)
        {
            var hasher = System.Security.Cryptography.SHA256.Create();
            var hashMessage = string.Join(",", ids.OrderBy(o => o));
            var hash = Convert.ToBase64String(hasher.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(hashMessage)));
            var providedHashBytes = Convert.FromBase64String(providedHash);

            return hash.Equals(providedHash);
        }
    }
}

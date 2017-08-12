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
using System.Text;

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
        public async Task<EventModel> GetEvent(Guid id)
        {
            await Task.Delay(5000);

            return await _db.GetEvent(id);
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
                    return this.Ok(new {
                        Verified = true
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

                await this.HttpContext.Authentication.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, principal, new Microsoft.AspNetCore.Http.Authentication.AuthenticationProperties()
                {
                    IsPersistent = false,
                    AllowRefresh = true,
                    IssuedUtc = DateTimeOffset.Now,
                    ExpiresUtc = DateTimeOffset.Now.AddMinutes(90)
                });

                return this.Ok(new
                {
                    PersonID = result.PersonID,
                    EmailAddress = result.EmailAddress
                });
            }

            return this.BadRequest();
        }

        [Authorize]
        [HttpPost]
        [Route("api/GetOrCreateHouse")]
        public async Task<IActionResult> GetOrCreateHouse()
        {
            var primaryHouse = await this.GetPrimaryHouse(this.User.Identity.Name);
            
            string houseID;

            if (primaryHouse == null)
            {
                var email = this.User.FindFirst(ClaimTypes.Email).Value;
                houseID = await _people.CreateHousehold(email, this.User.Identity.Name, new string[] { this.User.Identity.Name });
            }
            else
            {
                houseID = primaryHouse.ID;
            }

            var house = await _people.GetHousehold(houseID, includePeople: true);
            var ids = new List<string>();

            ids.Add(house.Data.ID);

            var people = house.GetRelated<PcoPeoplePerson>("people")
                .Select(s => new Person()
                {
                    PersonID = s.ID,
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
                ids.Add(person.PersonID);

                if (person.Child)
                {
                    continue;
                }

                var emails = await _people.GetEmailsForPerson(person.PersonID);
                var phones = await _people.GetPhonesForPerson(person.PersonID);
                var addresses = await _people.GetAddressesForPerson(person.PersonID);
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
                HouseholdName = house.Data.Attributes.Name,
                PrimaryContactID = house.Data.Attributes.PrimaryContactID,
                People = people
            });
        }

        protected async Task<string> CreatePerson(Person person)
        {
            var pcoPerson = new PcoPeoplePerson();

            person.CopyToPcoPerson(pcoPerson);

            var personID = await _people.CreatePerson(pcoPerson);
            
            if (person.EmailAddress != null)
            {
                await _people.AddOrUpdateEmail(personID, new PcoDataRecord<PcoEmailAddress>()
                {
                    Type = "email",
                    Attributes = new PcoEmailAddress()
                    {
                        Address = person.EmailAddress,
                        Location = "Home",
                        Primary = true
                    }
                });
            }

            if (person.PhoneNumber != null)
            {
                await _people.AddOrUpdatePhone(personID, new PcoDataRecord<PcoPhoneNumber>()
                {
                    Type = "phone_number",
                    Attributes = new PcoPhoneNumber(){
                        Number = person.PhoneNumber,
                        Location = "Mobile",
                        Primary = true
                    }
                });
            }

            if (person.Street != null)
            {
                await _people.AddOrUpdateAddress(personID, new PcoDataRecord<PcoStreetAddress>()
                {
                    Type="street_address",
                    Attributes = new PcoStreetAddress()
                    {
                        Street = person.Street,
                        City = person.City,
                        State = person.State,
                        Zip = person.Zip,
                        Location = "Home",
                        Primary = true
                    }
                });
            }

            return personID;
        }

        protected async Task UpdatePerson(Person person)
        {
            var pcoPerson = new PcoPeoplePerson();

            person.CopyToPcoPerson(pcoPerson);

            await _people.UpdatePerson(person.PersonID, pcoPerson);

            if (person.EmailAddress != null)
            {
                await _people.AddOrUpdateEmail(person.PersonID, new PcoDataRecord<PcoEmailAddress>()
                {
                    Type = "email",
                    ID = person.EmailAddressID,
                    Attributes = new PcoEmailAddress()
                    {
                        Address = person.EmailAddress,
                        Location = "Home",
                        Primary = true
                    }
                });
            }

            if (person.PhoneNumber != null)
            {
                await _people.AddOrUpdatePhone(person.PersonID, new PcoDataRecord<PcoPhoneNumber>()
                {
                    Type = "phone_number",
                    ID = person.PhoneNumberID,
                    Attributes = new PcoPhoneNumber()
                    {
                        Number = person.PhoneNumber,
                        Location = "Mobile",
                        Primary = true
                    }
                });
            }

            if (person.Street != null)
            {
                await _people.AddOrUpdateAddress(person.PersonID, new PcoDataRecord<PcoStreetAddress>()
                {
                    Type = "street_address",
                    ID = person.AddressID,
                    Attributes = new PcoStreetAddress()
                    {
                        Street = person.Street,
                        City = person.City,
                        State = person.State,
                        Zip = person.Zip,
                        Location = "Home",
                        Primary = true
                    }
                });
            }
        }

        [HttpPost]
        [Route("api/CompleteRegistration")]
        public async Task<IActionResult> CompleteRegistration([FromBody]SaveChangesModel model)
        {
            var primary = model.People.Single(x => x.IsPrimaryContact);

            if (string.IsNullOrEmpty(model.HouseholdName))
            {
                model.HouseholdName = $"{primary.LastName} Household";
            }            

            if (model.EventID == Guid.Empty)
            {
                return this.BadRequest("An EventID is required.");
            }
            
            if (model.HouseholdID == null)
            {
                foreach (var person in model.People)
                {
                    person.PersonID = await this.CreatePerson(person);
                }

                model.HouseholdID = await _people.CreateHousehold(model.HouseholdName, primary.PersonID, model.People.Select(s => s.PersonID));
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

                foreach (var updatedPerson in model.People)
                {
                    if (updatedPerson.PersonID == null)
                    {
                        updatedPerson.PersonID = await this.CreatePerson(updatedPerson);
                        await _people.AddToHousehold(model.HouseholdID, updatedPerson.PersonID);
                    }
                    else
                    {
                        if (!model.Identifiers.Contains(updatedPerson.PersonID))
                        {
                            throw new Exception($"An invalid attempt to update a PersonID {updatedPerson.PersonID} not in the identifier list was detected.");
                        }

                        if (updatedPerson.EmailAddressID != null && !model.Identifiers.Contains(updatedPerson.EmailAddressID))
                        {
                            throw new Exception($"An invalid attempt to update an Email Address ID {updatedPerson.EmailAddressID} not in the identifier list was detected.");
                        }

                        if (updatedPerson.PhoneNumberID != null && !model.Identifiers.Contains(updatedPerson.PhoneNumberID))
                        {
                            throw new Exception($"An invalid attempt to update an Phone Number ID {updatedPerson.PhoneNumberID} not in the identifier list was detected.");
                        }

                        if (updatedPerson.AddressID != null && !model.Identifiers.Contains(updatedPerson.AddressID))
                        {
                            throw new Exception($"An invalid attempt to update an Address ID {updatedPerson.AddressID} not in the identifier list was detected.");
                        }

                        await this.UpdatePerson(updatedPerson);
                    }
                }

                await _people.UpdateHousehold(model.HouseholdID, model.HouseholdName, primary.PersonID);
            }

            var notifyMessage = new StringBuilder();

            notifyMessage.AppendLine($"A new registration for '{model.HouseholdName}' to event {model.EventID} was submitted.").AppendLine();
            notifyMessage.AppendLine("-- Registered Persons -- ");

            foreach (var person in model.People)
            {
                if (await _db.GetEventPerson(model.EventID, person.PersonID) != null)
                {
                    continue;
                }

                notifyMessage.AppendLine($" - {person.FirstName} {person.LastName}");

                await _db.CreateEventPerson(new EventPerson()
                {
                    PersonID = person.PersonID,
                    EventID = model.EventID,
                    HouseholdID = model.HouseholdID,
                    HouseholdName = model.HouseholdName,
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

            await this.HttpContext.Authentication.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

            if (!string.IsNullOrEmpty(_options.NotifyEmail))
            {
                await _messageService.SendMessageAsync(_options.NotifyEmail, "New Registration", notifyMessage.ToString());
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

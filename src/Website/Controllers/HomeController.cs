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
        private readonly IPeopleApi _pco;
        private readonly IDataAccess _db;
        private readonly IMemoryCache _cache;
        private readonly SiteOptions _options;

        public HomeController(IMessageService messageService, IPeopleApi pco, IDataAccess db, IOptions<SiteOptions> options, IMemoryCache cache)
        {
            _options = options.Value;
            _messageService = messageService;
            _pco = pco;
            _db = db;
            _cache = cache;
        }

        private async Task<PcoDataRecord<PcoPeopleHousehold>> GetPrimaryHousehold(string personID)
        {
            var houses = await _pco.GetPersonHouseholds(personID);
            var primaryHouse = houses.Data.FirstOrDefault(x => x.Attributes.PrimaryContactID == personID);

            //TODO: Evaulate the logic here, if the person isn't the primary contact for a household,
            // but they only have one household, we just take the *only* house they are a member of and assume
            // they should be using that one.
            if (primaryHouse == null && houses.Data.Count == 1)
            {
                primaryHouse = houses.Data.FirstOrDefault();
            }

            return primaryHouse;
        }

        private async Task<string> CreatePerson(Person person)
        {
            var pcoPerson = new PcoPeoplePerson();

            person.CopyToPcoPerson(pcoPerson);

            var personID = await _pco.CreatePerson(pcoPerson);

            if (person.EmailAddress != null)
            {
                await _pco.CreateOrUpdateEmail(personID, new PcoDataRecord<PcoEmailAddress>()
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
                await _pco.CreateOrUpdatePhone(personID, new PcoDataRecord<PcoPhoneNumber>()
                {
                    Type = "phone_number",
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
                await _pco.CreateOrUpdateAddress(personID, new PcoDataRecord<PcoStreetAddress>()
                {
                    Type = "street_address",
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

        private async Task UpdatePerson(Person person)
        {
            var pcoPerson = new PcoPeoplePerson();

            person.CopyToPcoPerson(pcoPerson);

            await _pco.UpdatePerson(person.PersonID, pcoPerson);

            if (person.EmailAddress != null)
            {
                await _pco.CreateOrUpdateEmail(person.PersonID, new PcoDataRecord<PcoEmailAddress>()
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
                await _pco.CreateOrUpdatePhone(person.PersonID, new PcoDataRecord<PcoPhoneNumber>()
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
                await _pco.CreateOrUpdateAddress(person.PersonID, new PcoDataRecord<PcoStreetAddress>()
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

        private string GenerateIdentifierHash(IEnumerable<string> ids)
        {
            var hasher = System.Security.Cryptography.SHA256.Create();
            var hashMessage = string.Join(",", ids.Distinct().OrderBy(o => o));

            return Convert.ToBase64String(hasher.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(hashMessage)));
        }

        private bool VerifyIdentifierHash(IEnumerable<string> ids, string providedHash)
        {
            var hasher = System.Security.Cryptography.SHA256.Create();
            var hashMessage = string.Join(",", ids.Distinct().OrderBy(o => o));
            var hash = Convert.ToBase64String(hasher.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(hashMessage)));
            var providedHashBytes = Convert.FromBase64String(providedHash);

            return hash.Equals(providedHash);
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

            var person = await _pco.FindPersonByEmail(model.EmailAddress);
            
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

        // Gets the user's existing household of which they are the primary contact, or creates a new one.
        [Authorize]
        [HttpPost]
        [Route("api/GetOrCreateHouse")]
        public async Task<IActionResult> GetOrCreateHouse()
        {
            var primaryHouse = await this.GetPrimaryHousehold(this.User.Identity.Name);
            
            string houseID;

            // If the user isn't the primary contact on a household, create a new household
            // for this user and anyone they register under this process.
            if (primaryHouse == null)
            {
                var personID = this.User.Identity.Name;
                var email = this.User.FindFirst(ClaimTypes.Email).Value;

                houseID = await _pco.CreateHousehold(email, personID, new string[] { personID });
            }
            else
            {
                houseID = primaryHouse.ID;
            }

            // Get the people and data for the household that will be used for registration
            var house = await _pco.GetHousehold(houseID, includePeople: true);
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

            // Since email, address, and phones are a different dataset, loads those for each person as well.
            // UGH....lots of http requests.
            foreach (var person in people)
            {
                ids.Add(person.PersonID);

                var emails = await _pco.GetEmailsForPerson(person.PersonID);
                var phones = await _pco.GetPhonesForPerson(person.PersonID);
                var addresses = await _pco.GetAddressesForPerson(person.PersonID);
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

            // compute a hash so we don't have to verify with the API every time we need to know
            // if the user has access to the personID/householdID/email/phone they want to update.
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
        
        // Save the new household or update all the people in the existing one.
        //TODO: Should this be two different methods for creating a new house vs. updating existing?
        [HttpPost]
        [Route("api/CompleteRegistration")]
        public async Task<IActionResult> CompleteRegistration([FromBody]SaveChangesModel model)
        {
            // TODO: There is a scenario where the primary contact is a new person and they were removed
            // and there isn't a new primary contact. Fix that so this never throws an error. 
            var primary = model.People.SingleOrDefault(x => x.IsPrimaryContact);
            var evt = await _db.GetEvent(model.EventID);

            if (evt == null)
            {
                return this.BadRequest("An event must be specified or event not found.");
            }

            if (primary == null)
            {
                return this.BadRequest("A primary contact must be specified.");
            }

            if (model.EventID == Guid.Empty)
            {
                return this.BadRequest("An EventID is required.");
            }

            // We don't let the user set this directly right now, we 
            // just create a household using the name of the primary contact
            if (string.IsNullOrEmpty(model.HouseholdName) || model.HouseholdName == primary.EmailAddress)
            {
                model.HouseholdName = $"{primary.LastName} Household";
            }

            // If we need to create a new household
            if (model.HouseholdID == null)
            {
                // Create each person in the new household
                foreach (var person in model.People)
                {
                    person.PersonID = await this.CreatePerson(person);
                }

                // Create the household and add all the peeps
                model.HouseholdID = await _pco.CreateHousehold(model.HouseholdName, primary.PersonID, model.People.Select(s => s.PersonID));
            }
            else
            {
                // If we are updating an existing household, the user should be signed in 
                if (!this.User.Identity.IsAuthenticated)
                {
                    return this.StatusCode(403, "User Not Authenticated");    
                }

                // Verify the signature on the Identifiers hash
                if (!this.VerifyIdentifierHash(model.Identifiers, model.Signature))
                {
                    return this.StatusCode(403, "Invalid signature");
                }

                // Loop over all the peeps and create/update each one in PCO
                foreach (var updatedPerson in model.People)
                {
                    // Create a new person for ones that don't already exist
                    if (updatedPerson.PersonID == null)
                    {
                        updatedPerson.PersonID = await this.CreatePerson(updatedPerson);
                        await _pco.AddPersonToHousehold(model.HouseholdID, updatedPerson.PersonID);
                    }
                    else
                    {
                        // Verify the person we are updating is in the ident hash
                        if (!model.Identifiers.Contains(updatedPerson.PersonID))
                        {
                            throw new Exception($"An invalid attempt to update a PersonID {updatedPerson.PersonID} not in the identifier list was detected.");
                        }

                        // Verify the e-mail address we are updating is in the ident hash
                        if (updatedPerson.EmailAddressID != null && !model.Identifiers.Contains(updatedPerson.EmailAddressID))
                        {
                            throw new Exception($"An invalid attempt to update an Email Address ID {updatedPerson.EmailAddressID} not in the identifier list was detected.");
                        }

                        // Verify the phone we are updating is in the ident hash
                        if (updatedPerson.PhoneNumberID != null && !model.Identifiers.Contains(updatedPerson.PhoneNumberID))
                        {
                            throw new Exception($"An invalid attempt to update an Phone Number ID {updatedPerson.PhoneNumberID} not in the identifier list was detected.");
                        }

                        // Verify the address we are updating is in the ident hash
                        if (updatedPerson.AddressID != null && !model.Identifiers.Contains(updatedPerson.AddressID))
                        {
                            throw new Exception($"An invalid attempt to update an Address ID {updatedPerson.AddressID} not in the identifier list was detected.");
                        }

                        // Update the person record and all associated records in PCO
                        await this.UpdatePerson(updatedPerson);
                    }
                }

                // Update the household's name
                await _pco.UpdateHousehold(model.HouseholdID, model.HouseholdName, primary.PersonID);
            }

            // Create a notification message to let someone know that a registration was done.
            var notifyMessage = new StringBuilder();

            notifyMessage.AppendLine($"A new registration for '{model.HouseholdName}' to event {model.EventID} was submitted.").AppendLine();
            notifyMessage.AppendLine("*** Registered Persons ***");

            // Loop over each person created/updated in PCO and log a registration record to the Event database
            // and also add them to the notification message.
            foreach (var person in model.People)
            {
                // Silent fail this person if they already were registered
                if (await _db.GetEventPerson(model.EventID, person.PersonID) != null)
                {
                    continue;
                }

                string groupName = null;

                if (person.Selected)
                {
                    notifyMessage.AppendLine($" - {person.FirstName} {person.LastName}");

                    var fee = evt.Fees.FindMatchingFee(person);

                    if (fee == null)
                    {
                        groupName = "N/A";
                    }
                    else
                    {
                        groupName = fee.Group;
                        
                    }
                }

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
                    Gender = person.Gender,
                    //TODO: This is a hack to know which people were selected. Need to refactor the whole Fees/Age Limits thing etc
                    // and add Type field or something to log associated parent contacts or what-not.
                    Group = groupName
                });
            }

            //TODO: We shouldn't leave the user hanging out signed in without them knowing it even though the session will expire
            // but we also don't want them to have to re-verify if they register for something else.
            // OR, maybe that's not an often enough occurance for it to matter.
            // Maybe we should give them a choice on the conf screen "I'm Done" or "Do Another" and the done option
            // will sign them out. Either way, we should add a sign-out button somewhere on the UI even if it's obscure.
            // 
            // await this.HttpContext.Authentication.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Send the notification email.
            //TODO: The NotifyEmail should be configured per-event, not system-wide
            if (!string.IsNullOrEmpty(_options.NotifyEmail))
            {
                await _messageService.SendMessageAsync(_options.NotifyEmail, "New Registration", notifyMessage.ToString());
            }

            return this.NoContent();
        }

        // Get the city/state for a zip code already in our People db (cheap hack to not have to use another API)
        [Route("api/Zip/{zip}")]
        public async Task<IActionResult> GetZipLocation(string zip)
        {
            string cacheKey = $"Zip:${zip}";
            PcoStreetAddress address;

            if (!_cache.TryGetValue(cacheKey, out address))
            {
                var matches = await _pco.FindAddressByZipCode(zip);
                if (matches.Data.Count > 0)
                {
                    // Ugh... Get the city/state most used with this zip code since some are wrong.
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
    }
}

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

namespace WebApplicationBasic.Controllers
{

    public class HomeController : Controller
    {
        private readonly IMessageService _messageService;
        private readonly IPeopleApi _people;
        private readonly IDataAccess _db;

        public HomeController(IMessageService messageService, IPeopleApi people, IDataAccess db)
        {
            _messageService = messageService;
            _people = people;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        [HttpPost]
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

            var token = await _db.CreateLoginToken(model.EmailAddress, person.ID);

            await _messageService.SendMessageAsync(model.EmailAddress, "Your verification code", $"Use the code {token.Token} to verify your account.");

            return this.Ok(new
            {
                TokenID = token.TokenID
            });
        }

        [HttpPost]
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

                var houses = await _people.FindHouseholds(result.PersonID);
                var primaryHouse = houses.Data.FirstOrDefault(x => x.Attributes.PrimaryContactID == result.PersonID);

                if (primaryHouse == null && houses.Data.Count == 1)
                {
                    primaryHouse = houses.Data.FirstOrDefault();
                }

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

        //[Authorize]
        //public async Task<IActionResult> GetHouseholds()
        //{
        //    var personID = this.User.Identity.Name;
        //    var houses = await _people.FindHouseholds(personID);

        //    return this.Ok(houses);
        //}

        [Authorize]
        public async Task<IActionResult> GetHousehold(string id)
        {
            if (!await this.EnsureHouseholdAccess(id))
            {
                return this.StatusCode(403);
            }

            var house = await _people.GetHousehold(id, includePeople: true);

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
                }

                if (phone != null)
                {
                    person.PhoneNumber = phone.Attributes.Number;
                }

                if (email != null)
                {
                    person.EmailAddress = email.Attributes.Address;
                }
            }

            return this.Ok(new
            {
                HouseholdID = house.Data.ID,
                PrimaryContactID = house.Data.Attributes.PrimaryContactID,
                People = people
            });
        }

        [HttpPost]
        public async Task<IActionResult> CompleteRegistration([FromBody]SaveChangesModel model)
        {
            if (model.IsNew)
            {
                var primary = model.People.Single(x => x.IsPrimaryContact);

                foreach (var person in model.People)
                {
                    var pcoPerson = new PcoPeoplePerson();

                    person.CopyToPcoPerson(pcoPerson);

                    person.ID = await _people.CreatePerson(pcoPerson, primary.EmailAddress, primary.PhoneNumber);
                }

                var householdID = await _people.CreateHousehold(primary.LastName, primary.ID, model.People.Select(s => s.ID));
            }
            else
            {
                if (!await this.EnsureHouseholdAccess(model.HouseholdID))
                {
                    return this.StatusCode(403);
                }

                var house = await _people.GetHousehold(model.HouseholdID, includePeople: true);
                var people = house.GetRelated<PcoPeoplePerson>("people");

                foreach (var updatedPerson in model.People)
                {
                    var existingPerson = people.SingleOrDefault(x => x.ID == updatedPerson.ID);

                    if (existingPerson == null)
                    {
                        existingPerson = new PcoDataRecord<PcoPeoplePerson>()
                        {
                            Attributes = new PcoPeoplePerson()
                        };

                        updatedPerson.CopyToPcoPerson(existingPerson.Attributes);

                        var newPersonID = _people.CreatePerson(existingPerson.Attributes, updatedPerson.EmailAddress, updatedPerson.PhoneNumber);
                    }

                    updatedPerson.CopyToPcoPerson(existingPerson.Attributes);

                    await _people.UpdatePerson(existingPerson.ID, existingPerson.Attributes, updatedPerson.EmailAddress, updatedPerson.PhoneNumber);
                    await _people.AddToHousehold(model.HouseholdID, existingPerson.ID);
                }
            }

            return this.NoContent();
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

        private async Task<bool> EnsureHouseholdAccess(string householdID)
        {
            var personID = this.User.Identity.Name;
            var houses = await _people.FindHouseholds(personID);

            if (!houses.Data.Any(x => x.ID == householdID))
            {
                return false;
            }

            return true;
        }
    }
}

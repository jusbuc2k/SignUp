using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Registration.Models.Pco;
using Microsoft.Extensions.Options;

namespace Registration.Services
{
    public class HttpPeopleApiOptions
    {
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class HttpPeopleApi : IPeopleApi
    {
        public HttpPeopleApi(IOptions<HttpPeopleApiOptions> options)
        {
            this.Url = options.Value.Url;
            this.Username = options.Value.Username;
            this.Password = options.Value.Password;
        }

        protected string Url { get; set; }

        protected string Username { get; set; }

        protected string Password { get; set; }

        protected System.Net.Http.HttpClient Http { get; set; }

        protected void EnsureClient()
        {
            if (this.Http == null)
            {
                this.Http = new System.Net.Http.HttpClient();
                var byteArray = System.Text.UTF8Encoding.UTF8.GetBytes(string.Concat(this.Username,":", this.Password));
                this.Http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }
        }

        protected async Task<T> GetAsync<T>(string path)
        {
            this.EnsureClient();

            string url;

            if (path.StartsWith("https://"))
            {
                url = path;
            }
            else
            {
                url = string.Concat(this.Url, path);
            }
                        
            var result = await this.Http.GetAsync(url);

            result.EnsureSuccessStatusCode();

            var content = await result.Content.ReadAsStringAsync();

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
        }

        protected async Task<PcoSingleResponse<T>> GetRecordAsync<T>(string path) where T: class
        {
            var response = await this.GetAsync<PcoSingleResponse<T>>(path);

            if (response == null)
            {
                return null;
            }

            return response;
        }

        protected async Task<PcoListResponse<T>> GetListAsync<T>(string path) where T : class
        {
            var response = await this.GetAsync<PcoListResponse<T>>(path);

            if (response == null)
            {
                return null;
            }

            return response;
        }

        protected async Task<PcoDataRecord<T>> PostAsync<T>(string path, PcoSingleResponse<T> data)
        {
            this.EnsureClient();
            string url;

            if (path.StartsWith("https://"))
            {
                url = path;
            }
            else
            {
                url = string.Concat(this.Url, "/", path);
            }

            var dataJSON = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            var result = await this.Http.PostAsync(url, new StringContent(dataJSON));

            result.EnsureSuccessStatusCode();

            var content = await result.Content.ReadAsStringAsync();

            return Newtonsoft.Json.JsonConvert.DeserializeObject<PcoDataRecord<T>>(content);
        }

        protected async Task PatchAsync<T>(string path, T data)
        {
            this.EnsureClient();
            string url = string.Concat(this.Url, "/", path); ;

            var dataJSON = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            var method = new HttpMethod("PATCH");
            var message = new HttpRequestMessage(method, url);

            message.Content = new StringContent(dataJSON);

            var result = await this.Http.SendAsync(message);

            result.EnsureSuccessStatusCode();
        }

        //protected PeoplePerson ConvertPerson(DataRecord<PeoplePerson> person, PeopleArrayResponse<PeoplePerson> response)
        //{
        //    var attr = person.Attributes;

        //    attr.AddressList = person.Relationships["addresses"].Data
        //                .SelectMany(d =>
        //                    response.Included.Where(x => x.Type == d.Type && x.ID == d.ID)
        //                    .Select(i => i.Attributes.ToObject<StreetAddress>())
        //                );

        //    attr.PhoneNumberList = person.Relationships["phone_numbers"].Data
        //                .SelectMany(d =>
        //                    response.Included.Where(x => x.Type == d.Type && x.ID == d.ID)
        //                    .Select(i => i.Attributes.ToObject<PhoneNumber>())
        //                );

        //    attr.EmailAddressList = person.Relationships["emails"].Data
        //                .SelectMany(d =>
        //                    response.Included.Where(x => x.Type == d.Type && x.ID == d.ID)
        //                    .Select(i => i.Attributes.ToObject<EmailAddress>())
        //                );

        //    return attr;
        //}


        public async Task<PcoDataRecord<PcoPeoplePerson>> FindPersonByEmail(string emailAddress)
        {
            var results = await this.GetListAsync<PcoEmailAddress>($"emails?where[address]={emailAddress}");

            var firstMatch = results.Data.FirstOrDefault();

            if (firstMatch != null)
            {
                var person = await this.GetRecordAsync<PcoPeoplePerson>($"emails/{firstMatch.ID}/person");

                return person.Data;
            }
            else
            {
                return null;
            }
        }

        public Task<PcoListResponse<PcoPeopleHousehold>> FindHouseholds(string personID)
        {
           return this.GetListAsync<PcoPeopleHousehold>($"people/{personID}/households");
        }

        public Task<PcoSingleResponse<PcoPeopleHousehold>> GetHousehold(string id, bool includePeople = false)
        {
            var query = new StringBuilder();

            query.Append($"households/{id}");

            if (includePeople)
            {
                query.Append("?include=people");
            }

            return this.GetRecordAsync<PcoPeopleHousehold>(query.ToString());
        }

        public Task<PcoListResponse<PcoEmailAddress>> GetEmailsForPerson(string personID)
        {
            return this.GetListAsync<PcoEmailAddress>($"people/{personID}/emails");
        }

        public Task<PcoListResponse<PcoPhoneNumber>> GetPhonesForPerson(string personID)
        {
            return this.GetListAsync<PcoPhoneNumber>($"people/{personID}/phone_numbers");
        }

        public Task<PcoListResponse<PcoStreetAddress>> GetAddressesForPerson(string personID)
        {
            return this.GetListAsync<PcoStreetAddress>($"people/{personID}/addresses");
        }

        public async Task<bool> AddOrUpdateEmail(string personID, string emailAddress)
        {
            var emailRecord = await this.GetEmailAddress(emailAddress);


            if (emailRecord == null)
            {
                emailRecord = await this.PostAsync<PcoEmailAddress>($"people/{personID}/emails", new PcoSingleResponse<PcoEmailAddress>()
                {
                    Data = new PcoDataRecord<PcoEmailAddress>()
                    {
                        Attributes = new PcoEmailAddress()
                        {
                            Address = emailAddress,
                            Location = "Home",
                            Primary = true
                        },
                        Type = "Email"
                    }
                });
            }
            else
            {
                emailRecord.Attributes.Address = emailAddress;

                await this.PatchAsync($"people/{personID}/emails/{emailRecord.ID}", new PcoSingleResponse<PcoEmailAddress>()
                {
                    Data = emailRecord
                });
            }


            return true;
        }

        public async Task<bool> AddOrUpdatePhone(string personID, string phoneNumber)
        {
            var phoneRecord = await this.GetPhoneNumber(phoneNumber);

            if (phoneRecord == null)
            {
                phoneRecord = await this.PostAsync<PcoPhoneNumber>($"people/{personID}/phone_numbers", new PcoSingleResponse<PcoPhoneNumber>()
                {
                    Data = new PcoDataRecord<PcoPhoneNumber>()
                    {
                        Attributes = new PcoPhoneNumber()
                        {
                            Number = phoneNumber,
                            Location = "Home",
                            Primary = true
                        },
                        Type = "Email"
                    }
                });
            }
            else
            {
                phoneRecord.Attributes.Number = phoneNumber;

                await this.PatchAsync($"people/{personID}/phone_numbers/{phoneRecord.ID}", new PcoSingleResponse<PcoPhoneNumber>()
                {
                    Data = phoneRecord
                });
            }

            return true;
        }

        public async Task<string> CreatePerson(PcoPeoplePerson person, string emailAddress, string phoneNumber)
        {
            var personRecord = await this.PostAsync<PcoPeoplePerson>("people", new PcoSingleResponse<PcoPeoplePerson>()
            {
                Data = new PcoDataRecord<PcoPeoplePerson>()
                {
                    Attributes = person,
                    Type = "Person"
                }
            });

            if (!person.Child)
            {
                await AddOrUpdateEmail(personRecord.ID, emailAddress);
                await AddOrUpdatePhone(personRecord.ID, phoneNumber);
            }            

            return personRecord.ID;
        }
        
        public async Task<bool> UpdatePerson(string id, PcoPeoplePerson person, string emailAddress, string phoneNumber)
        {
            await this.PatchAsync<PcoSingleResponse<PcoPeoplePerson>>($"people/{id}", new PcoSingleResponse<PcoPeoplePerson>()
            {
                Data = new PcoDataRecord<PcoPeoplePerson>()
                {
                    Attributes = person,
                    Type = "Person",
                    ID = id
                }
            });

            if (!person.Child)
            {
                await AddOrUpdateEmail(id, emailAddress);
                await AddOrUpdatePhone(id, phoneNumber);
            }

            return true;
        }

        public async Task<string> CreateHousehold(string name, string primaryContactID, IEnumerable<string> memberIDs)
        {
            var house = await this.PostAsync<PcoPeopleHousehold>($"household", new PcoSingleResponse<PcoPeopleHousehold>()
            {
                Data = new PcoDataRecord<PcoPeopleHousehold>()
                {
                    Attributes = new PcoPeopleHousehold()
                    {
                        Name = name,
                        PrimaryContactID = primaryContactID
                    },
                    Relationships = new Dictionary<string, PcoPeopleRelationship>()
                    {
                        {
                            "people",
                            new PcoPeopleRelationship(memberIDs.Select(peep => new PcoPeopleRelationshipData() { Type = "person", ID = peep }))
                        }
                    }
                }
            });

            return house.ID;
        }

        public Task AddToHousehold(string householdID, string personID)
        {
            throw new NotImplementedException();
        }

        //public async Task<IEnumerable<DataRecord<PcoPeopleHousehold>>> GetHouseholdByEmail(string emailAddress)
        //{
        //    var emails = await this.GetListAsync<EmailAddress>($"emails?where[address]={emailAddress}");

        //    if (emails.Data.Count() <= 0)
        //    {
        //        return Enumerable.Empty<DataRecord<PcoPeopleHousehold>>();
        //    }

        //    if (emails.Data.Count() == 1)
        //    {
        //        return (await this.GetListAsync<PcoPeopleHousehold>($"emails/{emails.Data.First().ID}/person/households")).Data;
        //    }
        //    else if (emails.Data.Count() > 1)
        //    {
        //        //TODO: handle this.
        //        throw new NotImplementedException();
        //    }
        //    else if (emails.Data.Count() == 0)
        //    {
        //        return Enumerable.Empty<DataRecord<PcoPeopleHousehold>>();
        //    }
        //    else
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        ////public async Task<DataRecord<PeoplePerson>> GetOrAddPerson(PeoplePerson person)
        ////{
        ////    int isChild = person.Child ? 1 : 0;

        ////    var response = await this.GetListAsync<PeoplePerson>($"people?where[first_name]={person.FirstName}&where[last_name]={person.LastName}&child={isChild}&include=emails,phone_numbers");
        ////    var matches = response.Data;

        ////    if (person.Child)
        ////    {
        ////        matches = matches.Where(x => x.Attributes.BirthDate == person.BirthDate);
        ////    }
        ////    else
        ////    {
        ////        if (person.MobilePhone?.Number != null)
        ////        {
        ////            matches = matches.Where(m =>
        ////                m.Relationships["phone_numbers"].Data.SelectMany(r => response.Included.Where(i => i.Type == "PhoneNumber" && i.ID == r.ID).Select(s => s.Attributes.ToObject<PhoneNumber>()))
        ////                .Any(x => x.Number == person.MobilePhone.Number)
        ////            );
        ////        }

        ////        if (person.EmailAddress?.Address != null)
        ////        {
        ////            matches = matches.Where(m =>
        ////                m.Relationships["emails"].Data.SelectMany(r => response.Included.Where(i => i.Type == "EmailAddress" && i.ID == r.ID).Select(s => s.Attributes.ToObject<EmailAddress>()))
        ////                .Any(x => x.Address == person.EmailAddress.Address)
        ////            );
        ////        }
        ////    }

        ////    if (matches.Count() == 1)
        ////    {
        ////        return matches.Single();
        ////    }
        ////    else
        ////    {
        ////        return await this.PostAsync<PeoplePerson>("people", person);
        ////    }
        ////}


        //public async Task<DataRecord<PcoPeoplePerson>> CreatePerson(PcoPeoplePerson person, DataRecord<EmailAddress> emailAddress = null, DataRecord<PhoneNumber> phoneNumber = null, DataRecord<StreetAddress> address = null)
        //{
        //    return await this.PostAsync<PcoPeoplePerson>("people", new PcoSingleResponse<PcoPeoplePerson>()
        //    {
        //        Data = new DataRecord<PcoPeoplePerson>()
        //        {
        //            Attributes = person,
        //            Relationships = new Dictionary<string, PeopleRelationship>()
        //            {
        //                {
        //                    "phone_numbers",
        //                    new PeopleRelationship(new PeopleRelationshipData(){ Type = "PhoneNumber", ID = phoneNumber.ID })
        //                },
        //                {
        //                    "emails",
        //                    new PeopleRelationship(new PeopleRelationshipData(){ Type = "Email", ID = emailAddress.ID })
        //                },
        //                {
        //                    "addresses",
        //                    new PeopleRelationship(new PeopleRelationshipData(){ Type = "Address", ID = address.ID })
        //                }
        //            }
        //        }
        //    });
        //}

        //public async Task<DataRecord<PcoPeopleHousehold>> CreateHousehold(PcoPeopleHousehold householdAttributes, IEnumerable<DataRecord<PcoPeoplePerson>> members)
        //{
        //    return await this.PostAsync<PcoPeopleHousehold>($"household", new PcoSingleResponse<PcoPeopleHousehold>()
        //    {
        //        Data = new DataRecord<PcoPeopleHousehold>()
        //        {
        //            Attributes = householdAttributes,
        //            Relationships = new Dictionary<string, PeopleRelationship>()
        //            {
        //                {
        //                    "people",
        //                    new PeopleRelationship(members.Select(peep => new PeopleRelationshipData() { Type = "person", ID = peep.ID }))
        //                }
        //            }
        //        }                
        //    });
        //}

        //public async Task<IEnumerable<DataRecord<PcoPeoplePerson>>> GetHouseholdPersonList(string householdID)
        //{
        //    var result = await this.GetListAsync<PcoPeoplePerson>($"household/{householdID}/people");

        //    return result.Data;
        //}

        public async Task<PcoDataRecord<PcoEmailAddress>> GetEmailAddress(string address)
        {
            var results = await this.GetListAsync<PcoEmailAddress>($"emails?where[address]={address}");

            if (results.Data.Count() <= 0)
            {
                return null;
            }
            else
            {
                return results.Data.First();
            }
        }

        public async Task<PcoDataRecord<PcoPhoneNumber>> GetPhoneNumber(string phoneNumber)
        {
            var results = await this.GetListAsync<PcoPhoneNumber>($"phone_numbers?where[number]={phoneNumber}");

            if (results.Data.Count() <= 0)
            {
                return null;
            }
            else
            {
                return results.Data.First();
            }
        }

        //public async Task<DataRecord<EmailAddress>> CreateEmailAddress(EmailAddress attributes)
        //{
        //    return await this.PostAsync("emails", new PcoSingleResponse<EmailAddress>()
        //    {
        //        Data = new DataRecord<EmailAddress>()
        //        {
        //            Attributes = attributes
        //        }
        //    });
        //}

        //public async Task<DataRecord<PhoneNumber>> CreatePhoneNumber(PhoneNumber attributes)
        //{
        //    return await this.PostAsync("phone_numbers", new PcoSingleResponse<PhoneNumber>()
        //    {
        //        Data = new DataRecord<PhoneNumber>()
        //        {
        //            Attributes = attributes
        //        }
        //    });
        //}

        //public async Task UpdatePerson(string personID, PcoPeoplePerson updatedValues)
        //{
        //    await this.PatchAsync<PcoPeoplePerson>($"people/{personID}", updatedValues);

        //    var primaryPhones = await this.GetListAsync<PhoneNumber>($"people/{personID}/phone_numbers?where[primary]=1");
        //    var primaryPhone = primaryPhones.Data.FirstOrDefault();

        //    if (primaryPhone == null)
        //    {
        //        await this.PostAsync($"people/{personID}/phone_numbers", updatedValues.MobilePhone);
        //    }
        //    else
        //    {
        //        primaryPhone.Attributes.Location = updatedValues.MobilePhone.Location;
        //        primaryPhone.Attributes.Number = updatedValues.MobilePhone.Number;

        //        await this.PatchAsync($"people/{personID}/phone_numbers/{primaryPhone.ID}", primaryPhone.Attributes);
        //    }          
        //}

        //public async Task<DataRecord<PcoPeoplePerson>> AddPerson(PcoPeoplePerson person)
        //{
        //    var newPerson = await this.PostAsync<PcoPeoplePerson>($"people", person);

        //    if (person.MobilePhone != null)
        //    {
        //        person.MobilePhone.Primary = true;
        //        await this.PostAsync($"people/{newPerson.ID}/phone_numbers", person.MobilePhone);
        //    }

        //    if (person.EmailAddress != null)
        //    {
        //        person.EmailAddress.Primary = true;
        //        await this.PostAsync($"people/{newPerson.ID}/emails", person.EmailAddress);
        //    }

        //    if (person.StreetAddress != null)
        //    {
        //        person.StreetAddress.Primary = true;
        //        await this.PostAsync($"people/{newPerson.ID}/addresses", person.StreetAddress);
        //    }

        //    return newPerson;
        //}

        //public async Task<PcoListResponse<PcoPeoplePerson>> FindPerson(string firstName = null, string lastName = null, bool? child = null, string gender = null, DateTime? birthDate = null)
        //{
        //    var query = new StringBuilder();

        //    query = query.Append("people?include=addresses,phone_numbers,emails");

        //    if (firstName != null)
        //    {
        //        query = query.Append("&where[first_name]=").Append(firstName);
        //    }

        //    if (lastName != null)
        //    {
        //        query = query.Append("&where[last_name]=").Append(lastName);
        //    }

        //    if (gender != null)
        //    {
        //        query = query.Append("&where[gender]=").Append(gender);
        //    }

        //    if (child.HasValue)
        //    {
        //        query = query.Append("&where[child]=").Append(child.Value ? "1" : "0");
        //    }

        //    if (birthDate.HasValue)
        //    {
        //        query = query.Append("&where[birthdate]=").Append(birthDate.Value.ToString("yyyy-MM-dd"));
        //    }

        //    return await this.GetListAsync<PcoPeoplePerson>(query.ToString());
        //}

        //public Task AddToHousehold(string householdID, string personID)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task RemoveFromHoushold(string householdID, string personID)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task UpdatePrimaryContact(string householdID, string primaryContactID)
        //{
        //    throw new NotImplementedException();
        //}

        //public async Task<DataRecord<PeoplePerson>> GetPersonByEmail(string emailAddress)
        //{
        //    this.EnsureClient();

        //    var response = await this.GetAsync<PeopleArrayResponse<dynamic>>(string.Concat("emails?where[address]=", emailAddress));

        //    var emailData = response.Data.FirstOrDefault();

        //    if (emailData == null)
        //    {
        //        return null;
        //    }

        //    var person = await this.GetAsync<PeopleSingleRecordResponse<PeoplePerson>>(string.Concat("emails/", emailData.ID, "/person"));

        //    return person != null ? person.Data : null;
        //}

        //public async Task<DataRecord<PeopleHousehold>> GetHousehold(string householdID)
        //{
        //    return await this.GetRecordAsync<PeopleHousehold>($"household/{householdID}");
        //}

        //public async Task<IEnumerable<DataRecord<PeoplePerson>>> GetHouseholdPersonList(string householdID)
        //{
        //    return await this.GetListAsync<PeoplePerson>($"household/{householdID}/people");
        //}

        //public async Task<IEnumerable<PeoplePersonDetail>> GetHouseholdPersonListDetail(string householdID)
        //{
        //    var response = await this.GetAsync<PeopleArrayResponse<PeoplePerson>>($"household/{householdID}/people?include=addresses,phone_numbers,emails");

        //    if (response == null)
        //    {
        //        return null;
        //    }

        //    var list = new List<PeoplePersonDetail>();

        //    foreach (var person in response.Data)
        //    {
        //        list.Add(new PeoplePersonDetail()
        //        {
        //            FirstName = person.Attributes.FirstName,
        //            LastName = person.Attributes.LastName,
        //            BirthDate = person.Attributes.BirthDate,
        //            Gender = person.Attributes.Gender,
        //            MedicalNotes = person.Attributes.MedicalNotes,
        //            AddressList = person.Relationships["addresses"].Data
        //                .SelectMany(d =>
        //                    response.Included.Where(x => x.Type == d.Type && x.ID == d.ID)
        //                    .Select(i => i.Attributes.ToObject<StreetAddress>())
        //                ),
        //            PhoneNumberList = person.Relationships["phone_numbers"].Data
        //                .SelectMany(d =>
        //                    response.Included.Where(x => x.Type == d.Type && x.ID == d.ID)
        //                    .Select(i => i.Attributes.ToObject<PhoneNumber>())
        //                ),
        //            EmailAddressList = person.Relationships["emails"].Data
        //                .SelectMany(d =>
        //                    response.Included.Where(x => x.Type == d.Type && x.ID == d.ID)
        //                    .Select(i => i.Attributes.ToObject<EmailAddress>())
        //                )
        //        });
        //    }

        //    return list;
        //}

        //public async Task<IEnumerable<DataRecord<PeopleHousehold>>> GetPersonHouseholdList(string personID)
        //{
        //    return await this.GetListAsync<PeopleHousehold>($"people/{personID}/households");
        //}

        //public async Task<IEnumerable<DataRecord<PhoneNumber>>> GetPersonPhoneNumberList(string personID)
        //{
        //    return await this.GetListAsync<PhoneNumber>($"people/{personID}/phone_numbers");
        //}

        //public async Task<IEnumerable<DataRecord<EmailAddress>>> GetPersonEmailList(string personID)
        //{
        //    return await this.GetListAsync<EmailAddress>($"people/{personID}/emails");
        //}

        //public async Task<IEnumerable<DataRecord<StreetAddress>>> GetPersonAddressList(string personID)
        //{
        //    return await this.GetListAsync<StreetAddress>($"people/{personID}/addresses");
        //}

        //public async Task<IEnumerable<PeopleHousehold>> GetHouseholdsForPerson(string personID)
        //{
        //    this.EnsureClient();

        //    var response = await this.GetAsync<PeopleArrayResponse<PeopleHousehold>>(string.Concat("person/", personID, "/households"));

        //    return response.Data.Select(s => new Household()
        //    {
        //        HouseholdID = s.ID,
        //        MemberCount = s.Attributes.MemberCount,
        //        Name = s.Attributes.Name,
        //        Members = 
        //    });
        //}

        //public async Task<PeoplePerson> GetPersonByEmail(string emailAddress)
        //{
        //    this.EnsureClient();

        //    var response = await this.GetAsync<PeopleArrayResponse<dynamic>>(string.Concat("emails?where[address]=", emailAddress));

        //    var emailData = response.Data.FirstOrDefault();

        //    if (emailData == null)
        //    {
        //        return null;
        //    }

        //    var person = await this.GetAsync<PeopleSingleRecordResponse<PeoplePerson>>(string.Concat("emails/", emailData.ID, "/person"));

        //    return new Person()
        //    {
        //        PersonID = person.Data.ID,
        //        FirstName = person.Data.Attributes.FirstName,
        //        LastName = person.Data.Attributes.LastName,
        //        Gender = person.Data.Attributes.Gender
        //    };
        //}

        //public Task<IEnumerable<PeopleHousehold>> GetPersonHouseholdList(string personID)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<PeopleHousehold> GetHousehold(string householdID)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<IEnumerable<PeoplePerson>> GetHouseholdPersonList(string householdID)
        //{
        //    throw new NotImplementedException();
        //}
    }
}

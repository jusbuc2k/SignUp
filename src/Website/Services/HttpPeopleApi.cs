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
    public static class PcoEndPointNames
    {
        public static readonly string People = "people";
        public static readonly string Households = "households";
        public static readonly string Emails = "emails";
        public static readonly string PhoneNumbers = "phone_numbers";
        public static readonly string Addresses = "addresses";
        public static readonly string FieldData = "field_data";
    }

    public static class PcoTypeNames
    {
        public static readonly string Person = "Person";
        public static readonly string Household = "Household";
        public static readonly string EmailAddress = "Email";
        public static readonly string Phone = "PhoneNumber";
        public static readonly string Address = "Address";
        public static readonly string HouseholdMembership = "HouseholdMembership";
        public static readonly string FieldDatum = "FieldDatum";
        public static readonly string FieldDefinition = "FieldDefinition";
    }

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
            this.Options = options.Value;
        }

        protected HttpPeopleApiOptions Options { get; set; }

        protected System.Net.Http.HttpClient Http { get; set; }

        protected void EnsureClient()
        {
            if (this.Http == null)
            {
                this.Http = new System.Net.Http.HttpClient();
                var byteArray = System.Text.UTF8Encoding.UTF8.GetBytes(string.Concat(this.Options.Username,":", this.Options.Password));
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
                url = string.Concat(this.Options.Url, "/", path);
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

        protected async Task<PcoSingleResponse<T>> PostAsync<T>(string path, PcoSingleResponse<T> data)
        {
            this.EnsureClient();
            string url;

            if (path.StartsWith("https://"))
            {
                url = path;
            }
            else
            {
                url = string.Concat(this.Options.Url, "/", path);
            }

            var dataJSON = Newtonsoft.Json.JsonConvert.SerializeObject(data, new Newtonsoft.Json.JsonSerializerSettings()
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            });

            var result = await this.Http.PostAsync(url, new StringContent(dataJSON));

            result.EnsureSuccessStatusCode();

            var content = await result.Content.ReadAsStringAsync();

            return Newtonsoft.Json.JsonConvert.DeserializeObject<PcoSingleResponse<T>>(content);
        }

        protected async Task PatchAsync<T>(string path, T data)
        {
            this.EnsureClient();
            string url = string.Concat(this.Options.Url, "/", path); ;

            var dataJSON = Newtonsoft.Json.JsonConvert.SerializeObject(data, new Newtonsoft.Json.JsonSerializerSettings()
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            });

            var method = new HttpMethod("PATCH");
            var message = new HttpRequestMessage(method, url);

            message.Content = new StringContent(dataJSON);

            var result = await this.Http.SendAsync(message);

            result.EnsureSuccessStatusCode();
        }

        public async Task<PcoDataRecord<PcoPeoplePerson>> FindPersonByEmail(string emailAddress)
        {
            emailAddress = System.Net.WebUtility.UrlEncode(emailAddress);

            var results = await this.GetListAsync<PcoEmailAddress>($"{PcoEndPointNames.Emails}?where[address]={emailAddress}");
            var firstMatch = results.Data.FirstOrDefault();

            if (firstMatch != null)
            {
                var person = await this.GetRecordAsync<PcoPeoplePerson>($"{PcoEndPointNames.Emails}/{firstMatch.ID}/person");

                return person.Data;
            }
            else
            {
                return null;
            }
        }

        public Task<PcoListResponse<PcoPeopleHousehold>> GetPersonHouseholds(string personID)
        {
            personID = System.Net.WebUtility.UrlEncode(personID);
            return this.GetListAsync<PcoPeopleHousehold>($"{PcoEndPointNames.People}/{personID}/{PcoEndPointNames.Households}");
        }

        public Task<PcoSingleResponse<PcoPeopleHousehold>> GetHousehold(string id, bool includePeople = false)
        {
            id = System.Net.WebUtility.UrlEncode(id);

            var query = new StringBuilder();

            query.Append($"{PcoEndPointNames.Households}/{id}");

            if (includePeople)
            {
                query.Append("?include=people");
            }

            return this.GetRecordAsync<PcoPeopleHousehold>(query.ToString());
        }

        public Task<PcoListResponse<PcoEmailAddress>> GetEmailsForPerson(string personID)
        {
            personID = System.Net.WebUtility.UrlEncode(personID);
            return this.GetListAsync<PcoEmailAddress>($"{PcoEndPointNames.People}/{personID}/{PcoEndPointNames.Emails}");
        }

        public Task<PcoListResponse<PcoPhoneNumber>> GetPhonesForPerson(string personID)
        {
            personID = System.Net.WebUtility.UrlEncode(personID);
            return this.GetListAsync<PcoPhoneNumber>($"{PcoEndPointNames.People}/{personID}/{PcoEndPointNames.PhoneNumbers}");
        }

        public Task<PcoListResponse<PcoStreetAddress>> GetAddressesForPerson(string personID)
        {
            personID = System.Net.WebUtility.UrlEncode(personID);
            return this.GetListAsync<PcoStreetAddress>($"{PcoEndPointNames.People}/{personID}/{PcoEndPointNames.Addresses}");
        }

        public async Task<PcoDataRecord<PcoEmailAddress>> CreateOrUpdateEmail(string personID, PcoDataRecord<PcoEmailAddress> emailAddress)
        {
            personID = System.Net.WebUtility.UrlEncode(personID);

            if (string.IsNullOrEmpty(emailAddress.ID))
            {
                var emailAddressResponse = await this.PostAsync<PcoEmailAddress>($"{PcoEndPointNames.People}/{personID}/{PcoEndPointNames.Emails}", new PcoSingleResponse<PcoEmailAddress>()
                {
                    Data = emailAddress
                });

                return emailAddressResponse.Data;
            }
            else
            {
                await this.PatchAsync($"people/{personID}/emails/{System.Net.WebUtility.UrlEncode(emailAddress.ID)}", new PcoSingleResponse<PcoEmailAddress>()
                {
                    Data = emailAddress
                });

                return emailAddress;
            }
        }

        public async Task<PcoDataRecord<PcoPhoneNumber>> CreateOrUpdatePhone(string personID, PcoDataRecord<PcoPhoneNumber> phoneNumber)
        {
            //var phoneRecord = await this.GetPhoneNumber(phoneNumber);

            personID = System.Net.WebUtility.UrlEncode(personID);

            if (string.IsNullOrEmpty(phoneNumber.ID))
            {
                var newPhoneNumber = await this.PostAsync<PcoPhoneNumber>($"people/{personID}/phone_numbers", new PcoSingleResponse<PcoPhoneNumber>()
                {
                    Data = phoneNumber
                });

                return newPhoneNumber.Data;
            }
            else
            {
                await this.PatchAsync($"people/{personID}/phone_numbers/{System.Net.WebUtility.UrlEncode(phoneNumber.ID)}", new PcoSingleResponse<PcoPhoneNumber>()
                {
                    Data = phoneNumber
                });

                return phoneNumber;
            }
        }

        public async Task<PcoDataRecord<PcoStreetAddress>> CreateOrUpdateAddress(string personID, PcoDataRecord<PcoStreetAddress> address)
        {
            personID = System.Net.WebUtility.UrlEncode(personID);

            if (string.IsNullOrEmpty(address.ID))
            {
                var newAddress = await this.PostAsync<PcoStreetAddress>($"people/{personID}/addresses", new PcoSingleResponse<PcoStreetAddress>()
                {
                    Data = address
                });

                return newAddress.Data;
            }
            else
            {
                await this.PatchAsync($"people/{personID}/addresses/{System.Net.WebUtility.UrlEncode(address.ID)}", new PcoSingleResponse<PcoStreetAddress>()
                {
                    Data = address
                });

                return address;
            }
        }

        public async Task<string> CreatePerson(PcoPeoplePerson person)
        {
            var newPerson = await this.PostAsync<PcoPeoplePerson>("people", new PcoSingleResponse<PcoPeoplePerson>()
            {
                Data = new PcoDataRecord<PcoPeoplePerson>()
                {
                    Attributes = person,
                    Type = "person"
                }
            });

            return newPerson.Data.ID;
        }
        
        public async Task<bool> UpdatePerson(string id, PcoPeoplePerson person)
        {
            id = System.Net.WebUtility.UrlEncode(id);

            await this.PatchAsync<PcoSingleResponse<PcoPeoplePerson>>($"people/{id}", new PcoSingleResponse<PcoPeoplePerson>()
            {
                Data = new PcoDataRecord<PcoPeoplePerson>()
                {
                    Attributes = person,
                    Type = "person",
                    ID = id
                }
            });

            return true;
        }

        public async Task<string> CreateHousehold(string name, string primaryContactID, IEnumerable<string> memberIDs)
        {
            var house = await this.PostAsync<PcoPeopleHousehold>($"{PcoEndPointNames.Households}", new PcoSingleResponse<PcoPeopleHousehold>()
            {
                Data = new PcoDataRecord<PcoPeopleHousehold>()
                {
                    Type = PcoTypeNames.Household,
                    Attributes = new PcoPeopleHousehold()
                    {
                        Name = name,
                        PrimaryContactID = primaryContactID
                    },
                    Relationships = new Dictionary<string, PcoPeopleRelationship>()
                    {
                        {
                            "people",
                            new PcoPeopleRelationship(memberIDs.Select(peep => new PcoPeopleRelationshipData() {
                                Type = PcoTypeNames.Person,
                                ID = peep })
                            )
                        },
                        {
                            "primary_contact",
                            new PcoPeopleRelationship(new PcoPeopleRelationshipData()
                            {
                                ID  = primaryContactID,
                                Type = PcoTypeNames.Person
                            })
                        }
                    }
                }
            });

            return house.Data.ID;
        }

        public async Task UpdateHousehold(string id, string name, string primaryContactID)
        {
            id = System.Net.WebUtility.UrlEncode(id);

            await this.PatchAsync<PcoSingleResponse<PcoPeopleHousehold>>($"{PcoEndPointNames.Households}/{id}", new PcoSingleResponse<PcoPeopleHousehold>()
            {
                Data = new PcoDataRecord<PcoPeopleHousehold>()
                {
                    ID = id,
                    Type = PcoTypeNames.Household,
                    Attributes = new PcoPeopleHousehold()
                    {
                        Name = name,
                        PrimaryContactID = primaryContactID
                    }
                }
            });
        }

        public async Task AddPersonToHousehold(string householdID, string personID)
        {
            householdID = System.Net.WebUtility.UrlEncode(householdID);

            await this.PostAsync<dynamic>($"households/{householdID}/household_memberships", new PcoSingleResponse<dynamic>()
            {
                Data = new PcoDataRecord<dynamic>
                {
                    Type = PcoTypeNames.HouseholdMembership,
                    Attributes = new { Pending = false },
                    Relationships = new Dictionary<string, PcoPeopleRelationship>()
                    {
                        {
                            "person",
                            new PcoPeopleRelationship(new PcoPeopleRelationshipData()
                            {
                                ID = personID,
                                Type = PcoTypeNames.Person
                            })
                        }
                    }
                }               
            });
        }

        public Task<PcoListResponse<PcoStreetAddress>> FindAddressByZipCode(string zip)
        {
            zip = System.Net.WebUtility.UrlEncode(zip);

            return this.GetListAsync<PcoStreetAddress>($"addresses?where[zip]={zip}");
        }

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
       
    }
}

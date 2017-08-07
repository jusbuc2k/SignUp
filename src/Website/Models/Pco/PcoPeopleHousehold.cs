using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Pco
{
    public class PcoPeopleHousehold
    {
        public string Name { get; set; }

        [JsonProperty("member_count")]
        public int MemberCount { get; set; }

        [JsonProperty("primary_contact_id")]
        public string PrimaryContactID { get; set; }
    }
}

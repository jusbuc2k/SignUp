using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Pco
{
    public class PcoDataRecord<T>
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("attributes")]
        public T Attributes { get; set; }

        [JsonProperty("relationships")]
        public Dictionary<string, PcoPeopleRelationship> Relationships { get; set; }
    }
}

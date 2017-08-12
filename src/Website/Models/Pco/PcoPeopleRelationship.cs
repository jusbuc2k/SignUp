using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Pco
{
    public class PcoPeopleRelationship
    {
        public PcoPeopleRelationship()
        {
        }

        public PcoPeopleRelationship(IEnumerable<PcoPeopleRelationshipData> data)
        {
            this.Data = JArray.FromObject(data);
        }

        public PcoPeopleRelationship(PcoPeopleRelationshipData data)
        {
            this.Data = JObject.FromObject(data);
        }

        public IDictionary<string, string> Links { get; set; }

        [JsonProperty("data")]
        public JContainer Data { get; set; }

        public IEnumerable<PcoPeopleRelationshipData> GetDataAsList()
        {
            if (this.Data is IEnumerable)
            {
                return this.Data.ToObject<IEnumerable<PcoPeopleRelationshipData>>();
            }
            else if (this.Data != null)
            {
                return new PcoPeopleRelationshipData[] { this.Data.ToObject<PcoPeopleRelationshipData>() };
            }

            return Enumerable.Empty<PcoPeopleRelationshipData>();
        }

    }
}

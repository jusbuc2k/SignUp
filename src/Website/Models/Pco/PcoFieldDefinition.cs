using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Pco
{
    public class PcoFieldDefinition
    {
        [JsonProperty("sequence")]
        public int Sequence { get; set; }

        [JsonProperty("data_type")]
        public string DataType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }
    }
}

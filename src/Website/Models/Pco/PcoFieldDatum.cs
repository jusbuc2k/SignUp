using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Pco
{
    public class PcoFieldDatum
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}

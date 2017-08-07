using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Pco
{
    public class PcoResponse
    {
        public IDictionary<string, string> Links { get; set; }

        public IEnumerable<PcoDataRecord<JObject>> Included { get; set; }

        
    }
}

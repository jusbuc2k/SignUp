using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Pco
{
    public class PcoDataRecord<T>
    {
        public string Type { get; set; }

        public string ID { get; set; }

        public T Attributes { get; set; }

        public Dictionary<string, PcoPeopleRelationship> Relationships { get; set; }
    }
}

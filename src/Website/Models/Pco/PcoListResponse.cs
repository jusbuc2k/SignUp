using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models.Pco
{
    public class PcoListResponse<T> : PcoResponse
    {
        public ICollection<PcoDataRecord<T>> Data { get; set; }

        public IEnumerable<PcoDataRecord<R>> GetRelated<R>(PcoDataRecord<T> record, string relationshipName)
        {
            var relationships = this.Data
                .Where(x => x.ID == record.ID)
                .SelectMany(s => s.Relationships[relationshipName].GetDataAsList());

            return relationships.SelectMany(
                rel => this.Included.Where(x => x.Type == rel.Type && x.ID == rel.ID).Select(s => new PcoDataRecord<R>()
                {
                    ID = s.ID,
                    Attributes = s.Attributes.ToObject<R>(),
                    Relationships = s.Relationships,
                    Type = s.Type
                })
            );
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models
{
    public static class Extensions
    {
        public static EventFee FindMatchingFee(this IEnumerable<EventFee> fees, Person person)
        {
            var match = fees.Where(fee =>
            {
                if (fee.Child != person.Child)
                {
                    return false;
                }

                if (fee.Gender != "*" && person.Gender != fee.Gender)
                {
                    return false;
                }

                // check grade rules
                if (fee.ApplyGradeFilter)
                {
                    if (person.Grade == null)
                    {
                        return false;
                    }
                    else if (person.Grade > fee.MaxGrade || person.Grade < fee.MinGrade)
                    {
                        return false;
                    }
                }

                // check age rules.
                if (fee.ApplyAgeFilter)
                {
                    if (!person.BirthDate.HasValue)
                    {
                        return false;
                    }
                    else
                    {
                        var age = (fee.AgeCutoff.HasValue) ? fee.AgeCutoff.Value.Subtract(person.BirthDate.Value) : DateTime.Now.Subtract(person.BirthDate.Value);
                        var cutoffDate = fee.AgeCutoff.HasValue ? fee.AgeCutoff.Value : DateTime.Now;
                        var minBirthDate = cutoffDate.AddYears(-fee.MaxAge);
                        var maxBirthDate = cutoffDate.AddYears(-fee.MinAge);

                        if (person.BirthDate.Value < minBirthDate)
                        {
                            return false;
                        }

                        if (person.BirthDate.Value > maxBirthDate)
                        {
                            return false;
                        }
                    }
                }

                return true;
            });

            return match.FirstOrDefault();
        }
    }
}

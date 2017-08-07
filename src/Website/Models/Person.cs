using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models
{
    public class Person
    {
        public string ID { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        /// <summary>
        /// Gets or set the gender (M or F)
        /// </summary>
        public string Gender { get; set; }

        public DateTime? BirthDate { get; set; }

        public bool Child { get; set; }

        public string EmailAddress { get; set; }

        public string PhoneNumber { get; set; }

        public string Grade { get; set; }

        public string MedicalNotes { get; set; }

        public bool IsPrimaryContact { get; set; }

        public string Street { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }

        public void CopyToPcoPerson(Pco.PcoPeoplePerson person)
        {
            person.FirstName = this.FirstName;
            person.LastName = this.LastName;
            person.Child = this.Child;
            person.Gender = this.Gender;
            person.BirthDate = this.BirthDate;
            person.Grade = this.Grade;
            person.MedicalNotes = this.MedicalNotes;
        }
    }
}

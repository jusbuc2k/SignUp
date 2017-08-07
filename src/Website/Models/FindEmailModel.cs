using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models
{
    public class FindEmailModel
    {
        [Required]
        public string EmailAddress { get; set; }
    }
}

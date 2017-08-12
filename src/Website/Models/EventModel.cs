using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Models
{
    public class EventModel
    {
        public Guid EventID { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string LongDescription { get; set; }

        public string PaymentInstructions { get; set; }

        public string ConfirmationMessage { get; set; }

        public string LogoUrl { get; set; }

        public DateTimeOffset BeginDateTime { get; set; }

        public DateTimeOffset EndDateTime { get; set; }

        public IEnumerable<EventFee> Fees { get; set; }

        public string SupportInfo { get; set; }
    }
}

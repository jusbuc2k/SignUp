using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Services
{
    public class SmtpMessageService : IMessageService
    {
        public Task<bool> SendMessageAsync(string recipientAddress, string messageSubject, string messageBody)
        {
            return Task.FromResult(true);
        }
    }
}

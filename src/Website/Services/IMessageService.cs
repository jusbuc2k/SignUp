using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Registration.Services
{
    public interface IMessageService
    {
        Task<bool> SendMessageAsync(string recipientAddress, string messageSubject, string messageBody);
    }
}

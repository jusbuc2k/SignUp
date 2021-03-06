﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Options;

namespace Registration.Services
{
    public class SendGridOptions
    {
        public string ReplyAddress { get; set; }

        public string ApiKey { get; set; }

        public string FromAddress { get; set; }

        public string FromName { get; set; }
    }


    public class SendGridMessageService : IMessageService
    {
        private SendGridOptions _options;

        public SendGridMessageService(IOptions<SendGridOptions> options)
        {
            _options = options.Value;
        }
        
        public async Task<bool> SendMessageAsync(string recipientAddress, string messageSubject, string messageBody)
        {
            var msg = new SendGridMessage();

            msg.SetFrom(new EmailAddress(_options.FromAddress, _options.FromName));

            if (!string.IsNullOrEmpty(_options.ReplyAddress))
            {
                msg.SetReplyTo(new EmailAddress(_options.ReplyAddress));
            }

            var recipients = new List<EmailAddress>
            {
                new EmailAddress(recipientAddress),
            };
            msg.AddTos(recipients);

            msg.SetSubject(messageSubject);

            msg.AddContent(MimeType.Text, messageBody);
            //msg.AddContent(MimeType.Html, "<p>Hello World!</p>");

            msg.SetOpenTracking(false, null);

            var client = new SendGridClient(_options.ApiKey);

            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                return true;
            }

            return false;
        }
    }
}

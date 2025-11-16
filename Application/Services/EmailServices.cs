using Microsoft.Extensions.Options;
using TutorLinkBe.Domain.Models;
using System.Net;
using System.Net.Mail;
namespace TutorLinkBe.Application.Services
{
    public class EmailServices :IEmailService
    {
        private readonly EmailSettings _emailSettings;
    public EmailServices(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

        public async Task SendEmailAsync(string toEmail, string subject, string content)
        {
            var mailMessage = new MailMessage()
            {
                From = new MailAddress(_emailSettings.Sender, _emailSettings.SenderName),
                Subject = subject,
                Body = content,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            var smtpClient = new SmtpClient(_emailSettings.MailServer, _emailSettings.MailPort)
            {
                Credentials = new NetworkCredential(_emailSettings.Sender, _emailSettings.Password),
                EnableSsl = true,
            };

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RocketLaunchNotifier.Services
{
    public class EmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public EmailService(ILogger<EmailService> logger)
        {
            // Load configuration from appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _smtpHost = config["SmtpSettings:Host"];
            _smtpPort = int.Parse(config["SmtpSettings:Port"]);
            _smtpUser = config["SmtpSettings:User"];
            _smtpPass = config["SmtpSettings:Pass"];
            _logger = logger;
        }

        public void SendEmail(string recipient, string subject, string body)
        {
            try
            {
                using var smtpClient = new SmtpClient(_smtpHost)
                {
                    Port = _smtpPort,
                    Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUser),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };
                mailMessage.To.Add(recipient);

                smtpClient.Send(mailMessage);
                _logger.LogInformation($"Email sent to {recipient}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email to {recipient}: {ex.Message}");
            }
        }

        public void SendEmails(List<string> recipients, string subject, string body)
        {
            foreach (var recipient in recipients)
            {
                SendEmail(recipient, subject, body);
            }
        }
    }
}

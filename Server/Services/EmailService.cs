using DynamicFormsApp.Shared.Services;
using DynamicFormsApp.Shared.Models;
using System.Net.Mail;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace DynamicFormsApp.Server.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _env;

        public EmailService(IConfiguration configuration, IHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        public async Task SendBugReportEmail(EmailModel email)
        {
            var mail = new MailMessage
            {
                From = new MailAddress("NDABugReport@ntnanderson.com"),
                Subject = $"Bug Report for Application Device Tracking",
                IsBodyHtml = true,
                Body = $"{email.UserName} has reported a bug:<br/><br/>{email.Body}" +
                       $"<br/><br/><a href='{email.Link}'>View the page</a>."
            };

            // Primary recipient
            mail.To.Add(_configuration["Email:BugReportEmailTo"]);

            // Attach any files
            if (email.Attachments != null)
            {
                foreach (var att in email.Attachments)
                {
                    var stream = new MemoryStream(att.Content);
                    mail.Attachments.Add(new Attachment(stream, att.Name));
                }
            }

            using var client = new SmtpClient(_configuration["Email:IP"])
            {
                Port = int.Parse(_configuration["Email:Port"]!)
            };
            await client.SendMailAsync(mail);
        }

        public async Task SendFormResponseNotification(string toEmail, string formName, int formId)
        {
            var baseUrl = _configuration["AppBaseUrl"]?.TrimEnd('/') ?? string.Empty;
            var responsesLink = $"{baseUrl}/forms/{formId}/responses";

            var mail = new MailMessage
            {
                From = new MailAddress(_configuration["Email:From"] ?? "noreply@example.com"),
                Subject = $"Your form '{formName}' received a new response",
                Body = $@"<p style='font-family:sans-serif;font-size:14px'>A new response has been submitted for your form <strong>{formName}</strong>.</p>
                        <p><a href='{responsesLink}' style='display:inline-block;padding:8px 12px;background-color:#007bff;color:#fff;text-decoration:none;border-radius:4px'>View Responses</a></p>",
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            using var client = new SmtpClient(_configuration["Email:IP"])
            {
                Port = int.Parse(_configuration["Email:Port"]!)
            };
            await client.SendMailAsync(mail);
        }

        public async Task SendFormShareNotification(string toEmail, string firstName, string formName, string? description, int formId, string sharedBy, string ownerEmail)
        {
            var baseUrl = _configuration["AppBaseUrl"]?.TrimEnd('/') ?? string.Empty;
            var formLink = $"{baseUrl}/forms/{formId}";


            var greeting = string.IsNullOrWhiteSpace(firstName) ? "Hello" : $"Hi {firstName}";

            var descBlock = string.IsNullOrWhiteSpace(description) ? string.Empty : $"<p>{description}</p>";

            var mail = new MailMessage
            {
                From = new MailAddress(string.IsNullOrWhiteSpace(ownerEmail) ? (_configuration["Email:From"] ?? "noreply@example.com") : ownerEmail),
                Subject = $"{sharedBy} shared a form with you",
                Body = $@"<div style='font-family:sans-serif;font-size:14px;line-height:1.5'>
                            <p>{greeting},</p>
                            <p><strong>{sharedBy}</strong> has shared the form <strong>{formName}</strong> with you.</p>
                            {descBlock}
                            <p style='text-align:center;margin:20px 0'>
                                <a href='{formLink}' style='display:inline-block;padding:10px 20px;background-color:#007bff;color:#fff;text-decoration:none;border-radius:4px'>👉 View the Form</a>
                            </p>
                            <p>If you have any questions, contact me at <a href='mailto:{ownerEmail}'>{ownerEmail}</a>.</p>
                            <p style='font-size:12px;color:#555'>If you weren\u2019t expecting this, ignore this email or contact us.</p>
                        </div>",
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            using var client = new SmtpClient(_configuration["Email:IP"])
            {
                Port = int.Parse(_configuration["Email:Port"]!)
            };
            await client.SendMailAsync(mail);
        }

        public async Task SendFormTransferNotification(string toEmail, string formName, string? description, int formId, string transferredBy)
        {
            var baseUrl = _configuration["AppBaseUrl"]?.TrimEnd('/') ?? string.Empty;
            var formLink = $"{baseUrl}/forms/{formId}";

            var descBlock = string.IsNullOrWhiteSpace(description) ? string.Empty : $"<p>{description}</p>";

            var mail = new MailMessage
            {
                From = new MailAddress(_configuration["Email:From"] ?? "noreply@example.com"),
                Subject = $"{transferredBy} transferred a form to you",
                Body = $@"<div style='font-family:sans-serif;font-size:14px;line-height:1.5'>
                            <p><strong>{transferredBy}</strong> has transferred the form <strong>{formName}</strong> to you.</p>
                            {descBlock}
                            <p style='text-align:center;margin:20px 0'>
                                <a href='{formLink}' style='display:inline-block;padding:10px 20px;background-color:#007bff;color:#fff;text-decoration:none;border-radius:4px'>👉 View the Form</a>
                            </p>
                        </div>",
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            using var client = new SmtpClient(_configuration["Email:IP"])
            {
                Port = int.Parse(_configuration["Email:Port"]!)
            };
            await client.SendMailAsync(mail);
        }
    }
}

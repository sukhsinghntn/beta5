using DynamicFormsApp.Shared.Services;
using DynamicFormsApp.Shared.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DynamicFormsApp.Client.Services
{
    public class EmailServiceProxy : IEmailService
    {
        private readonly HttpClient _httpClient;

        public EmailServiceProxy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task SendBugReportEmail(EmailModel email)
        {
            await _httpClient.PostAsJsonAsync("api/email/bug/", email);
        }

        public async Task SendFormResponseNotification(string toEmail, string formName, int formId)
        {
            var payload = new { toEmail, formName, formId };
            await _httpClient.PostAsJsonAsync("api/email/formresponse", payload);
        }

        public async Task SendFormShareNotification(string toEmail, string firstName, string formName, string? description, int formId, string sharedBy, string ownerEmail)
        {
            var payload = new { toEmail, firstName, formName, description, formId, sharedBy, ownerEmail };
            await _httpClient.PostAsJsonAsync("api/email/formshare", payload);
        }

        public async Task SendFormTransferNotification(string toEmail, string formName, string? description, int formId, string transferredBy)
        {
            var payload = new { toEmail, formName, description, formId, transferredBy };
            await _httpClient.PostAsJsonAsync("api/email/formtransfer", payload);
        }
    }
}

using DynamicFormsApp.Shared.Models;
using System.Threading.Tasks;

namespace DynamicFormsApp.Shared.Services
{
    public interface IEmailService
    {
        Task SendBugReportEmail(EmailModel email);
        Task SendFormResponseNotification(string toEmail, string formName, int formId);
        Task SendFormShareNotification(string toEmail, string firstName, string formName, string? description, int formId, string sharedBy, string ownerEmail);
        Task SendFormTransferNotification(string toEmail, string formName, string? description, int formId, string transferredBy);
    }
}

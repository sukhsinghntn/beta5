using Microsoft.AspNetCore.Mvc;
using DynamicFormsApp.Shared.Services;
using DynamicFormsApp.Shared.Models;
using System.Threading.Tasks;

namespace DynamicFormsApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("bug")]
        public async Task<IActionResult> SendBugReportEmail([FromBody] EmailModel email)
        {
            await _emailService.SendBugReportEmail(email);
            return Ok();
        }

        [HttpPost("formresponse")]
        public async Task<IActionResult> SendFormResponseEmail([FromBody] FormResponseNotification model)
        {
            await _emailService.SendFormResponseNotification(model.toEmail, model.formName, model.formId);
            return Ok();
        }

        [HttpPost("formshare")]
        public async Task<IActionResult> SendFormShareEmail([FromBody] FormShareNotification model)
        {
            await _emailService.SendFormShareNotification(model.toEmail, model.firstName, model.formName, model.description, model.formId, model.sharedBy, model.ownerEmail);
            return Ok();
        }

        public class FormResponseNotification
        {
            public string toEmail { get; set; }
            public string formName { get; set; }
            public int formId { get; set; }
        }

        public class FormShareNotification
        {
            public string toEmail { get; set; }
            public string firstName { get; set; }
            public string formName { get; set; }
            public string? description { get; set; }
            public int formId { get; set; }
            public string sharedBy { get; set; }
            public string ownerEmail { get; set; }
        }
    }
}

using DynamicFormsApp.Server.Services;
using DynamicFormsApp.Shared;
using DynamicFormsApp.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace DynamicFormsApp.Server.Controllers
{
    [ApiController]
    [Route("api/components")]
    public class ComponentsController : ControllerBase
    {
        private readonly ComponentService _service;
        public ComponentsController(ComponentService service)
        {
            _service = service;
        }

        [HttpGet("{department}")]
        public async Task<IEnumerable<CustomComponent>> GetForUser(string department, [FromQuery] string user)
        {
            return await _service.GetForUserAsync(department, user);
        }

        public class SaveComponentRequest
        {
            public string Name { get; set; } = string.Empty;
            public DesignerField Field { get; set; } = new();
            public string Department { get; set; } = string.Empty;
            public string CreatedBy { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<ActionResult<CustomComponent>> Save([FromBody] SaveComponentRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest("Name is required");
            if (RequiresOptions(req.Field.FieldType) && !HasOptions(req.Field))
                return BadRequest("At least one option is required");
            var comp = await _service.SaveAsync(req.Name, req.Field, req.Department, req.CreatedBy);
            return Ok(comp);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string user)
        {
            await _service.DeleteAsync(id, user);
            return NoContent();
        }
        
        private static bool RequiresOptions(string fieldType) =>
            fieldType is "radio" or "checkbox" or "dropdown" or
            "grid_radio" or "grid_checkbox" or "grid_text";

        private static bool HasOptions(DesignerField field) => field.FieldType switch
        {
            "radio" or "checkbox" or "dropdown" => field.OptionItems.Any(o => !string.IsNullOrWhiteSpace(o)),
            "grid_radio" or "grid_checkbox" => field.GridRows.Any(r => !string.IsNullOrWhiteSpace(r)) && field.GridColumns.Any(c => !string.IsNullOrWhiteSpace(c)),
            "grid_text" => field.GridColumns.Any(c => !string.IsNullOrWhiteSpace(c)),
            _ => true
        };
    }
}

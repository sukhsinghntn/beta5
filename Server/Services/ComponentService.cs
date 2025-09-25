using DynamicFormsApp.Server.Data;
using DynamicFormsApp.Shared;
using DynamicFormsApp.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq;

namespace DynamicFormsApp.Server.Services
{
    public class ComponentService
    {
        private readonly AppDbContext _db;
        public ComponentService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<CustomComponent> SaveAsync(string name, DesignerField field, string department, string user)
        {
            var comp = new CustomComponent
            {
                Name = name,
                FieldJson = JsonSerializer.Serialize(field),
                Department = department,
                CreatedBy = user
            };
            _db.CustomComponents.Add(comp);
            await _db.SaveChangesAsync();
            return comp;
        }

        public async Task<List<CustomComponent>> GetForUserAsync(string department, string user)
        {
            return await _db.CustomComponents
                .Where(c => c.Department == department || c.CreatedBy == user)
                .ToListAsync();
        }

        public async Task DeleteAsync(int id, string user)
        {
            var comp = await _db.CustomComponents.FirstOrDefaultAsync(c => c.Id == id && c.CreatedBy == user);
            if (comp != null)
            {
                _db.CustomComponents.Remove(comp);
                await _db.SaveChangesAsync();
            }
        }
    }
}

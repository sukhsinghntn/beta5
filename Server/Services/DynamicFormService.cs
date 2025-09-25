using DynamicFormsApp.Server.Data;
using DynamicFormsApp.Shared.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DynamicFormsApp.Server.Services
{
    public class DynamicFormService
    {
        private readonly AppDbContext _db;

        public DynamicFormService(AppDbContext db)
        {
            _db = db;
        }

        private string SanitizeKey(string raw) =>
            Regex.Replace(raw, @"[^\w]", "_");

        private string GetUniqueKey(string rawKey, HashSet<string> existing)
        {
            var baseKey = SanitizeKey(rawKey);
            if (string.IsNullOrEmpty(baseKey))
            {
                baseKey = "Field";
            }

            var unique = baseKey;
            int suffix = 1;
            while (existing.Contains(unique))
            {
                unique = $"{baseKey}_{suffix}";
                suffix++;
            }
            existing.Add(unique);
            return unique;
        }

        public async Task<int> UpdateFormAsync(int formId, CreateFormDto dto, string user)
        {
            var existing = await _db.Forms.Include(f => f.Fields).FirstOrDefaultAsync(f => f.Id == formId && f.CreatedBy == user);
            if (existing == null)
            {
                throw new InvalidOperationException("Form not found");
            }

            var deletions = existing.Fields
                .Where(f => !dto.Fields.Any(n => string.Equals(n.Key, f.Key, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (deletions.Count > 0 && !existing.IsDraft)
            {
                // Archive current form with a new ID and table
                var archive = new Form
                {
                    Name = existing.Name,
                    Description = existing.Description,
                    CreatedBy = existing.CreatedBy,
                    RequireLogin = existing.RequireLogin,
                    NotifyOnResponse = existing.NotifyOnResponse,
                    NotificationEmail = existing.NotificationEmail,
                    IsActive = false,
                    IsDraft = existing.IsDraft,
                    Version = existing.Version,
                    PreviousVersionId = existing.PreviousVersionId,
                    Fields = existing.Fields.Select(f => new FormField
                    {
                        Key = f.Key,
                        Label = f.Label,
                        FieldType = f.FieldType,
                        Placeholder = f.Placeholder,
                        CharLimit = f.CharLimit,
                        MinCharLimit = f.MinCharLimit,
                        IsRequired = f.IsRequired,
                        OptionsJson = f.OptionsJson,
                        ImageUrl = f.ImageUrl,
                        ImageWidth = f.ImageWidth,
                        ImageHeight = f.ImageHeight,
                        Row = f.Row,
                        Column = f.Column
                    }).ToList()
                };

                _db.Forms.Add(archive);
                await _db.SaveChangesAsync();

                var rawName = SanitizeKey(existing.Name);
                var oldTable = $"Form_{existing.Id}_{rawName}";
                var archiveTable = $"Form_{archive.Id}_{rawName}";
                await _db.Database.ExecuteSqlRawAsync($"EXEC sp_rename '{oldTable}', '{archiveTable}'");

                // Prepare new table for the edited form
                _db.FormFields.RemoveRange(existing.Fields);
                existing.Fields.Clear();

                var newTable = $"Form_{existing.Id}_{rawName}";
                var sb = new StringBuilder($"CREATE TABLE [{newTable}] (ResponseId INT IDENTITY(1,1) PRIMARY KEY, CreatedAt DATETIME2 NOT NULL, Status NVARCHAR(50) NULL");
                if (dto.RequireLogin)
                {
                    sb.Append(", [ResponderName] NVARCHAR(255) NULL");
                }

                var keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var fld in dto.Fields)
                {
                    var keySource = string.IsNullOrWhiteSpace(fld.Key) ? fld.Label : fld.Key;
                    var key = GetUniqueKey(keySource, keySet);
                    var newField = new FormField
                    {
                        Key = key,
                        Label = fld.Label,
                        FieldType = fld.FieldType,
                        Placeholder = fld.Placeholder,
                        CharLimit = fld.CharLimit,
                        MinCharLimit = fld.MinCharLimit,
                        IsRequired = fld.IsRequired,
                        OptionsJson = fld.OptionsJson,
                        ImageUrl = fld.ImageUrl,
                        ImageWidth = fld.ImageWidth,
                        ImageHeight = fld.ImageHeight,
                        Row = fld.Row,
                        Column = fld.Column
                    };
                    existing.Fields.Add(newField);

                    if (newField.FieldType != "section" && newField.FieldType != "title" && newField.FieldType != "image" && newField.FieldType != "statictext")
                    {
                        sb.Append($", [{newField.Key}] {MapToSqlType(newField.FieldType)} {(newField.IsRequired ? "NOT NULL" : "NULL")}");
                    }
                }
                sb.Append(");");
                await _db.Database.ExecuteSqlRawAsync(sb.ToString());

                existing.Version += 1;
                existing.PreviousVersionId = archive.Id;
                existing.Name = dto.Name;
                existing.Description = dto.Description;
                existing.RequireLogin = dto.RequireLogin;
                existing.NotifyOnResponse = dto.NotifyOnResponse;
                existing.NotificationEmail = dto.NotificationEmail;
                existing.IsActive = dto.IsActive;
                existing.IsDraft = dto.IsDraft;

                await _db.SaveChangesAsync();
                return existing.Id;
            }

            if (deletions.Count > 0)
            {
                var rawName = SanitizeKey(existing.Name);
                var tableName = $"Form_{existing.Id}_{rawName}";
                foreach (var del in deletions)
                {
                    var sql = $"ALTER TABLE [{tableName}] DROP COLUMN [{del.Key}];";
                    await _db.Database.ExecuteSqlRawAsync(sql);
                    _db.FormFields.Remove(del);
                }
            }

            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.RequireLogin = dto.RequireLogin;
            existing.NotifyOnResponse = dto.NotifyOnResponse;
            existing.NotificationEmail = dto.NotificationEmail;
            existing.IsActive = dto.IsActive;
            existing.IsDraft = dto.IsDraft;

            var existingKeys = new HashSet<string>(existing.Fields.Select(f => f.Key), StringComparer.OrdinalIgnoreCase);

            foreach (var fld in dto.Fields)
            {
                var match = existing.Fields.FirstOrDefault(f => f.Key.Equals(fld.Key, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    match.Label = fld.Label;
                    match.FieldType = fld.FieldType;
                    match.Placeholder = fld.Placeholder;
                    match.CharLimit = fld.CharLimit;
                    match.MinCharLimit = fld.MinCharLimit;
                    match.IsRequired = fld.IsRequired;
                    match.OptionsJson = fld.OptionsJson;
                    match.ImageUrl = fld.ImageUrl;
                    match.ImageWidth = fld.ImageWidth;
                    match.ImageHeight = fld.ImageHeight;
                    match.Row = fld.Row;
                    match.Column = fld.Column;
                }
                else
                {
                    var keySource = string.IsNullOrWhiteSpace(fld.Key) ? fld.Label : fld.Key;
                    var key = GetUniqueKey(keySource, existingKeys);
                    var newField = new FormField
                    {
                        Key = key,
                        Label = fld.Label,
                        FieldType = fld.FieldType,
                        Placeholder = fld.Placeholder,
                        CharLimit = fld.CharLimit,
                        MinCharLimit = fld.MinCharLimit,
                        IsRequired = fld.IsRequired,
                        OptionsJson = fld.OptionsJson,
                        ImageUrl = fld.ImageUrl,
                        ImageWidth = fld.ImageWidth,
                        ImageHeight = fld.ImageHeight,
                        Row = fld.Row,
                        Column = fld.Column
                    };
                    existing.Fields.Add(newField);

                    var rawName = SanitizeKey(existing.Name);
                    var tableName = $"Form_{existing.Id}_{rawName}";
                    if (newField.FieldType != "section" && newField.FieldType != "title" && newField.FieldType != "image" && newField.FieldType != "statictext")
                    {
                        var sqlType = MapToSqlType(newField.FieldType);
                        var sql = $"ALTER TABLE [{tableName}] ADD [{newField.Key}] {sqlType} NULL;";
                        await _db.Database.ExecuteSqlRawAsync(sql);
                    }
                }
            }

            await _db.SaveChangesAsync();
            return existing.Id;
        }


        public async Task<int> CreateFormAsync(string formName, string? description, List<CreateFieldDto> fields, string createdBy, bool requireLogin, bool notifyOnResponse, string? notificationEmail, bool isActive, bool isDraft = false, int version = 1, int? previousVersionId = null)
        {
            var form = new Form
            {
                Name = formName,
                Description = description,
                CreatedBy = createdBy,
                RequireLogin = requireLogin,
                NotifyOnResponse = notifyOnResponse,
                NotificationEmail = notificationEmail,
                IsActive = isActive,
                IsDraft = isDraft,
                Version = version,
                PreviousVersionId = previousVersionId,
                Fields = new List<FormField>()
            };
            var keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var fld in fields)
            {
                var keySource = string.IsNullOrWhiteSpace(fld.Key) ? fld.Label : fld.Key;
                form.Fields.Add(new FormField
                {
                    Key = GetUniqueKey(keySource, keySet),
                    Label = fld.Label,
                    FieldType = fld.FieldType,
                    Placeholder = fld.Placeholder,
                    CharLimit = fld.CharLimit,
                    MinCharLimit = fld.MinCharLimit,
                    IsRequired = fld.IsRequired,
                    OptionsJson = fld.OptionsJson,
                    ImageUrl = fld.ImageUrl,
                    ImageWidth = fld.ImageWidth,
                    ImageHeight = fld.ImageHeight,
                    Row = fld.Row,
                    Column = fld.Column
                });
            }
            _db.Forms.Add(form);
            await _db.SaveChangesAsync();

            var rawName = SanitizeKey(form.Name);
            var tableName = $"Form_{form.Id}_{rawName}";
            var sb = new StringBuilder(
                $"CREATE TABLE [{tableName}] (" +
                "ResponseId INT IDENTITY(1,1) PRIMARY KEY, " +
                "CreatedAt DATETIME2 NOT NULL, " +
                "Status NVARCHAR(50) NULL");
            if (requireLogin)
            {
                sb.Append(", [ResponderName] NVARCHAR(255) NULL");
            }

            foreach (var fld in form.Fields)
            {
                if (fld.FieldType == "section" || fld.FieldType == "title" || fld.FieldType == "image" || fld.FieldType == "statictext")
                    continue;
                sb.Append($", [{fld.Key}] {MapToSqlType(fld.FieldType)} {(fld.IsRequired ? "NOT NULL" : "NULL")}");
            }
            sb.Append(");");

            await _db.Database.ExecuteSqlRawAsync(sb.ToString());
            return form.Id;
        }

        public async Task<Form> GetFormAsync(int formId)
        {
            var form = await _db.Forms
                .Include(f => f.Fields)
                .FirstOrDefaultAsync(f => f.Id == formId)
                ?? throw new InvalidOperationException("Form not found");
            return form;
        }

        public async Task<Form> StoreResponseAsync(int formId, Dictionary<string, object> values, string? responderName = null)
        {
            var form = await _db.Forms.FindAsync(formId)
                       ?? throw new InvalidOperationException("Form not found");
            if (!form.IsActive)
            {
                throw new InvalidOperationException("Form inactive");
            }
            var rawName = SanitizeKey(form.Name);
            var tableName = $"Form_{formId}_{rawName}";

            var validFields = form.Fields
                .Where(f => f.FieldType != "section" && f.FieldType != "title" && f.FieldType != "image" && f.FieldType != "statictext")
                .Select(f => f.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var filtered = values
                .Where(kv => validFields.Contains(kv.Key))
                .ToDictionary(k => k.Key, v => v.Value);

            var cols = string.Join(", ", filtered.Keys.Select(k => $"[{k}]"));
            var paramNames = string.Join(", ",
                filtered.Keys.Select((k, i) => $"@p{i}"));

            var sqlParams = new List<SqlParameter>();
            int idx = 0;
            foreach (var kv in filtered)
            {
                object raw = kv.Value;
                if (raw is JsonElement je)
                {
                    raw = je.ValueKind switch
                    {
                        JsonValueKind.String => je.GetString(),
                        JsonValueKind.Number when je.TryGetInt64(out var l) => l,
                        JsonValueKind.Number when je.TryGetDouble(out var d) => d,
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Array => je.GetRawText(), // Store JSON string for arrays
                        JsonValueKind.Object => je.GetRawText(),
                        JsonValueKind.Null => null,
                        _ => je.GetRawText(),
                    };
                }

                if (raw is List<string> stringList)
                {
                    raw = JsonSerializer.Serialize(stringList);
                }

                sqlParams.Add(new SqlParameter($"@p{idx}", raw ?? DBNull.Value));
                idx++;
            }

            cols = string.IsNullOrEmpty(cols) ? "CreatedAt, Status" : cols + ", CreatedAt, Status";
            paramNames = string.IsNullOrEmpty(paramNames) ? "@p_created, @p_status" : paramNames + ", @p_created, @p_status";
            sqlParams.Add(new SqlParameter("@p_created", DateTime.UtcNow));
            sqlParams.Add(new SqlParameter("@p_status", DBNull.Value));

            if (form.RequireLogin)
            {
                cols += ", ResponderName";
                paramNames += ", @p_responder";
                sqlParams.Add(new SqlParameter("@p_responder", (object?)responderName ?? DBNull.Value));
            }

            var sql = $"INSERT INTO [{tableName}] ({cols}) VALUES ({paramNames});";
            await _db.Database.ExecuteSqlRawAsync(sql, sqlParams.ToArray());

            return form;
        }

        public async Task DeactivateFormAsync(int formId, string user)
        {
            var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == formId && f.CreatedBy == user);
            if (form == null)
            {
                throw new InvalidOperationException("Form not found");
            }

            form.IsActive = false;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteFormAsync(int formId, string user)
        {
            var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == formId && f.CreatedBy == user);
            if (form == null)
            {
                throw new InvalidOperationException("Form not found");
            }

            form.IsDeleted = true;
            form.IsActive = false;
            await _db.SaveChangesAsync();
        }

        public async Task RestoreFormAsync(int formId)
        {
            var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == formId);
            if (form == null)
            {
                throw new InvalidOperationException("Form not found");
            }

            form.IsDeleted = false;
            await _db.SaveChangesAsync();
        }

        public async Task ActivateFormAsync(int formId, string user)
        {
            var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == formId && f.CreatedBy == user);
            if (form == null)
            {
                throw new InvalidOperationException("Form not found");
            }

            form.IsActive = true;
            await _db.SaveChangesAsync();
        }

        public async Task<List<Form>> GetAllFormsAsync()
        {
            return await _db.Forms
                .Include(f => f.Fields)
                .Where(f => f.IsActive && !f.IsDraft && !f.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<Form>> GetDeletedFormsAsync()
        {
            return await _db.Forms
                .Include(f => f.Fields)
                .Where(f => f.IsDeleted && !f.IsDraft)
                .ToListAsync();
        }

        public async Task<int> GetFormCountAsync()
        {
            return await _db.Forms.CountAsync(f => f.IsActive && !f.IsDraft && !f.IsDeleted);
        }

        public async Task<List<Form>> SearchFormsAsync(bool includePrivate)
        {
            var query = _db.Forms
                .Include(f => f.Fields)
                .Where(f => f.IsActive && !f.IsDraft && !f.IsDeleted);

            if (!includePrivate)
            {
                query = query.Where(f => !f.RequireLogin);
            }

            return await query.ToListAsync();
        }

        public async Task<List<Form>> GetFormsByUserAsync(string user, bool includeDrafts = false)
        {
            var query = _db.Forms
                .Include(f => f.Fields)
                .Where(f => f.CreatedBy == user && !f.IsDeleted)
                .Where(f => !_db.Forms.Any(f2 => f2.PreviousVersionId == f.Id));

            if (!includeDrafts)
            {
                query = query.Where(f => !f.IsDraft);
            }

            return await query.ToListAsync();
        }

        public async Task<List<Form>> GetDraftFormsByUserAsync(string user)
        {
            return await _db.Forms
                .Include(f => f.Fields)
                .Where(f => f.CreatedBy == user && f.IsDraft && f.IsActive && !f.IsDeleted)
                .ToListAsync();
        }

        public async Task ShareFormAsync(int formId, string owner, string targetUser)
        {
            var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == formId && f.CreatedBy == owner);
            if (form == null)
            {
                throw new InvalidOperationException("Form not found");
            }

            var exists = await _db.FormShares.AnyAsync(s => s.FormId == formId && s.UserName == targetUser);
            if (!exists)
            {
                _db.FormShares.Add(new FormShare { FormId = formId, UserName = targetUser });
                await _db.SaveChangesAsync();
            }
        }

        public async Task ChangeOwnerAsync(int formId, string newOwner)
        {
            var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == formId);
            if (form == null)
            {
                throw new InvalidOperationException("Form not found");
            }

            form.CreatedBy = newOwner;
            await _db.SaveChangesAsync();
        }

        public async Task<List<FormShare>> GetFormSharesAsync(int formId, string requester)
        {
            var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == formId);
            if (form == null)
            {
                throw new InvalidOperationException("Form not found");
            }
            if (form.CreatedBy != requester)
            {
                throw new UnauthorizedAccessException();
            }
            return await _db.FormShares.Where(s => s.FormId == formId).ToListAsync();
        }

        public async Task RemoveShareAsync(int formId, string requester, string targetUser)
        {
            var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == formId);
            if (form == null)
            {
                throw new InvalidOperationException("Form not found");
            }
            if (form.CreatedBy != requester && requester != targetUser)
            {
                throw new UnauthorizedAccessException();
            }

            var share = await _db.FormShares.FirstOrDefaultAsync(s => s.FormId == formId && s.UserName == targetUser);
            if (share != null)
            {
                _db.FormShares.Remove(share);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<Form>> GetFormsSharedWithUserAsync(string user)
        {
            var formIds = await _db.FormShares.Where(s => s.UserName == user).Select(s => s.FormId).ToListAsync();
            return await _db.Forms
                .Include(f => f.Fields)
                .Where(f => formIds.Contains(f.Id) && f.IsActive && !f.IsDeleted)
                .ToListAsync();
        }

        private async Task<bool> HasResponseAccessAsync(int formId, string user)
        {
            var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == formId);
            if (form == null) return false;
            if (form.CreatedBy == user) return true;
            return await _db.FormShares.AnyAsync(s => s.FormId == formId && s.UserName == user);
        }

        public async Task<List<Form>> GetFormHistoryAsync(int formId)
        {
            var result = new List<Form>();
            var current = await _db.Forms
                .Include(f => f.Fields)
                .FirstOrDefaultAsync(f => f.Id == formId);

            while (current != null)
            {
                result.Add(current);
                if (current.PreviousVersionId.HasValue)
                {
                    current = await _db.Forms
                        .Include(f => f.Fields)
                        .FirstOrDefaultAsync(f => f.Id == current.PreviousVersionId.Value);
                }
                else
                {
                    current = null;
                }
            }

            return result
                .OrderByDescending(f => f.Version)
                .ToList();
        }

        public async Task<List<Dictionary<string, object>>> GetResponsesAsync(int formId, string user)
        {
            if (!await HasResponseAccessAsync(formId, user))
            {
                throw new UnauthorizedAccessException();
            }

            var form = await _db.Forms.FindAsync(formId)
                       ?? throw new InvalidOperationException("Form not found");
            if (!form.IsActive && form.CreatedBy != user)
            {
                throw new InvalidOperationException("Form inactive");
            }
            var rawName = SanitizeKey(form.Name);
            var tableName = $"Form_{formId}_{rawName}";

            using var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM [{tableName}];";
            using var reader = await cmd.ExecuteReaderAsync();

            var results = new List<Dictionary<string, object>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var val = await reader.IsDBNullAsync(i)
                               ? null
                               : reader.GetValue(i);
                    row[name] = val!;
                }
                results.Add(row);
            }
            return results;
        }

        public async Task<Dictionary<string, object>> GetResponseAsync(int formId, int responseId, string user)
        {
            if (!await HasResponseAccessAsync(formId, user))
            {
                throw new UnauthorizedAccessException();
            }

            var form = await _db.Forms.FindAsync(formId)
                       ?? throw new InvalidOperationException("Form not found");
            if (!form.IsActive && form.CreatedBy != user)
            {
                throw new InvalidOperationException("Form inactive");
            }
            var rawName = SanitizeKey(form.Name);
            var tableName = $"Form_{formId}_{rawName}";

            using var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM [{tableName}] WHERE ResponseId=@id;";
            var param = cmd.CreateParameter();
            param.ParameterName = "@id";
            param.Value = responseId;
            cmd.Parameters.Add(param);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException("Response not found");
            }

            var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                var val = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                row[name] = val!;
            }
            return row;
        }

        public async Task<List<int>> GetResponseIdsAsync(int formId, string user)
        {
            if (!await HasResponseAccessAsync(formId, user))
            {
                throw new UnauthorizedAccessException();
            }

            var form = await _db.Forms.FindAsync(formId)
                       ?? throw new InvalidOperationException("Form not found");
            if (!form.IsActive && form.CreatedBy != user)
            {
                throw new InvalidOperationException("Form inactive");
            }

            var rawName = SanitizeKey(form.Name);
            var tableName = $"Form_{formId}_{rawName}";

            using var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT ResponseId FROM [{tableName}] ORDER BY ResponseId;";

            var ids = new List<int>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetInt32(0));
            }

            return ids;
        }

        public async Task UpdateResponseStatusAsync(int formId, int responseId, string status, string user)
        {
            if (!await HasResponseAccessAsync(formId, user))
            {
                throw new UnauthorizedAccessException();
            }

            var form = await _db.Forms.FindAsync(formId)
                       ?? throw new InvalidOperationException("Form not found");
            var rawName = SanitizeKey(form.Name);
            var tableName = $"Form_{formId}_{rawName}";

            using var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"UPDATE [{tableName}] SET Status=@status WHERE ResponseId=@id;";
            var p1 = cmd.CreateParameter();
            p1.ParameterName = "@status";
            p1.Value = status ?? string.Empty;
            cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter();
            p2.ParameterName = "@id";
            p2.Value = responseId;
            cmd.Parameters.Add(p2);
            await cmd.ExecuteNonQueryAsync();
        }

        private string MapToSqlType(string fieldType) => fieldType switch
        {
            "number" => "FLOAT",
            "date" => "DATE",
            "time" => "TIME",
            "datetime" => "DATETIME2",
            "file" => "NVARCHAR(MAX)",
            "checkbox" => "NVARCHAR(MAX)",      // Store as JSON array
            "dropdown" => "NVARCHAR(255)",
            "user" => "NVARCHAR(255)",
            "department" => "NVARCHAR(255)",
            "location" => "NVARCHAR(255)",
            "radio" => "NVARCHAR(255)",
            "textarea" => "NVARCHAR(MAX)",
            "grid_radio" => "NVARCHAR(MAX)",    // JSON object
            "grid_checkbox" => "NVARCHAR(MAX)", // JSON object
            "grid_text" => "NVARCHAR(MAX)",     // JSON array of rows
            "scale" => "INT",
            _ => "NVARCHAR(MAX)"
        };
    }
}

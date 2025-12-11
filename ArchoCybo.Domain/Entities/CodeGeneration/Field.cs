using ArchoCybo.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchoCybo.Domain.Entities.CodeGeneration
{
    public class Field
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        // Enum for type
        public FieldDataType DataType { get; set; } = FieldDataType.String;

        // Validation constraints
        public bool IsNullable { get; set; } = true;
        public bool IsPrimaryKey { get; set; } = false;
        public int? MaxLength { get; set; }
        public int? MinLength { get; set; }
        public bool IsEmail => DataType == FieldDataType.Email;
        public bool IsDate => DataType == FieldDataType.Date || DataType == FieldDataType.DateTime;

        // Relation to Entity
        public Guid EntityId { get; set; }
        public Entity Entity { get; set; } = null!;

        // Helper for DataAnnotations
        public string GetValidationAttributes()
        {
            var attrs = new List<string>();

            if (!IsNullable)
                attrs.Add("[Required]");

            if (MinLength.HasValue)
                attrs.Add($"[MinLength({MinLength.Value})]");

            if (MaxLength.HasValue)
                attrs.Add($"[MaxLength({MaxLength.Value})]");

            if (IsEmail)
                attrs.Add("[EmailAddress]");

            if (IsDate)
                attrs.Add("[DataType(DataType.Date)]");

            return string.Join(" ", attrs);
        }
    }

}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Prosjekt.Models.Entities
{
    public class GeoChange
    {
        [Key]
        public int Id { get; set; }

        public Coordinates Coords { get; set; }

        [Required]
        [MaxLength(4000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ChangeTypes { get; set; } = string.Empty;

        public double Radius { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BindNever]
        public string UserId { get; set; } = string.Empty;
    }
}
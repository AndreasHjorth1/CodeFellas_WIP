using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    public record VehicleViewModel : ResourceViewModel
    {
        public enum VehicleType
        {
            [Display(Name = "Annet")]
            Other = 0,
            [Display(Name = "Bil")]
            Car = 1,
            [Display(Name = "Buss")]
            Bus = 2,
            [Display(Name = "Sykkel")]
            Bicycle = 3,
            [Display(Name = "Motorsykkel")]
            Motorcycle = 4,
            [Display(Name = "Traktor")]
            Tractor = 5,
            [Display(Name = "Lastebil")]
            Truck = 6,
            [Display(Name = "Doser")]
            Dozer = 7,
            [Display(Name = "Gravemaskin")]
            Excavator = 8
        }

        [Display(Name = "Kjøretøytype")]
        public VehicleType Type { get; init; } = VehicleType.Other;

        [Display(Name = "Skiltnummer")]
        public string LicensePlate { get; init; } = string.Empty;

        public VehicleViewModel()
        {
            Category = ResourceType.Vehicle;
        }
    }
}
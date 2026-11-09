using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    public record VehicleViewModel : ResourceViewModel
    {
        public enum VehicleType
        {
            [Display(Name = "Bil")]
            Car,
            [Display(Name = "Buss")]
            Bus,
            [Display(Name = "Sykkel")]
            Bicycle,
            [Display(Name = "Motorsykkel")]
            Motorcycle,
            [Display(Name = "Traktor")]
            Tractor,
            [Display(Name = "Lastebil")]
            Truck, [Display(Name = "Doser")]
            Dozer,
            [Display(Name = "Gravemaskin")]
            Excavator,
            [Display(Name = "Annet")]
            Other
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
namespace CarServices.Domain.Entities
{
    public class VehicleModel
    {
        public int Id { get; set; }
        public int BrandId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = "";
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public bool IsActive { get; set; } = true;
        public VehicleBrand Brand { get; set; } = null!;
        public VehicleCategory Category { get; set; } = null!;
    }
}

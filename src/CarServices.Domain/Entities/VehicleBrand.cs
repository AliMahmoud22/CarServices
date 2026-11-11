namespace CarServices.Domain.Entities
{
    public class VehicleBrand
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public List<VehicleModel> Models { get; set; } = new();
    }
}

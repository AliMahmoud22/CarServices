namespace CarServices.Domain.Entities
{
    public class VehicleCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int SortOrder { get; set; }
        public List<VehicleModel> Models { get; set; } = new();
        public List<ServicePrice> Prices { get; set; } = new();
    }
}

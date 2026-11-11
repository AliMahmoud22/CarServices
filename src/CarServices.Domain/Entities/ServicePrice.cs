namespace CarServices.Domain.Entities
{
    public class ServicePrice
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public Service Service { get; set; } = null!;
        public VehicleCategory Category { get; set; } = null!;
    }
}

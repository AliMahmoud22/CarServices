namespace CarServices.Domain.Entities
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public List<ServicePrice> Prices { get; set; } = new();
    }
}

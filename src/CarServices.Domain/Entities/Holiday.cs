namespace CarServices.Domain.Entities
{
    public class Holiday
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public string? Reason { get; set; }
    }
}

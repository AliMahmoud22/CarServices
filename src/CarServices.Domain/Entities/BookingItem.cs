namespace CarServices.Domain.Entities
{
    // نسخة مجمّدة: لو تغيّر السعر لاحقًا لا يتأثر الحجز القديم
    public class BookingItem
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = "";
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public Booking Booking { get; set; } = null!;
    }
}

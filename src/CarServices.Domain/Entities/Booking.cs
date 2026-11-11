using CarServices.Domain.Enums;

namespace CarServices.Domain.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public string Reference { get; set; } = "";      // رقم الحجز الذي يراه العميل، مثل CS-20261008-0042
        public int CustomerId { get; set; }
        public int VehicleModelId { get; set; }
        public int? ModelYear { get; set; }
        public string? PlateNumber { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }              // = StartAt + مجموع مدد الخدمات
        public decimal TotalPrice { get; set; }          // السعر مجمّد وقت الحجز
        public int TotalDurationMinutes { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public string? CustomerNotes { get; set; }
        public string? InternalNotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Customer Customer { get; set; } = null!;
        public VehicleModel VehicleModel { get; set; } = null!;
        public List<BookingItem> Items { get; set; } = new();
    }
}

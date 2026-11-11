namespace CarServices.Domain.Entities
{
    public class ShopSetting
    {
        public int Id { get; set; }
        public int MaxConcurrentBookings { get; set; } = 2;  // عدد الرافعات/الأماكن
        public int SlotIntervalMinutes { get; set; } = 30;
        public int MinHoursBeforeBooking { get; set; } = 2;
        public int MaxDaysAhead { get; set; } = 30;
    }
}

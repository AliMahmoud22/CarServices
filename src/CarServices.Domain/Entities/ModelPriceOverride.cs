namespace CarServices.Domain.Entities
{
    // استثناء لموديل معين (سعر أو مدة مختلفة عن فئته). اختياري.
    public class ModelPriceOverride
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public int VehicleModelId { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsAvailable { get; set; } = true; // false = الخدمة غير متاحة لهذا الموديل
    }
}

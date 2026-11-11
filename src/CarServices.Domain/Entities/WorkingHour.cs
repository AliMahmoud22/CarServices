namespace CarServices.Domain.Entities
{
    public class WorkingHour
    {
        public int Id { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeOnly OpenTime { get; set; }
        public TimeOnly CloseTime { get; set; }
        public bool IsClosed { get; set; }
    }
}

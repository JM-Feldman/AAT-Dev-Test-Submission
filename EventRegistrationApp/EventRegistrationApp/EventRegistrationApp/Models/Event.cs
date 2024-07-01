namespace EventRegistrationApp.Models
{
    public class Event
    {
        public int EventId { get; set; }
        public required string EventName { get; set; }
        public int NumSeatsAvailable { get; set; }
    }
}

using System;

namespace HotelBot
{
    public class BookingDetails
    {
        public int NumberOfGuests { get; set; }
        public bool? Breakfast { get; set; }
        public string? Arrival { get; set; } = null!;
        public string? Departure { get; set; } = null!;
    }
}
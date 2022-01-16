namespace HotelBot;

public class BookingDetails
{
    public int NumberOfGuests { get; set; }
    public bool? Breakfast { get; set; }
    public string? Arrival { get; set; } = null!;
    public string? Departure { get; set; } = null!;

    /// <summary>
    ///     Number of Children under 16 that are part of the NumberOfGuests value.
    ///     The Default Value is set to -1 to account for 0 children.
    /// </summary>
    public int NumberOfChildren { get; set; } = -1;

    public int NumberOfNights { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public bool? ParkingLot { get; set; }
    public string PillowType { get; set; } = null!;
    public string Allergies { get; set; } = null!;
    public bool? AgeVerified { get; set; }
}
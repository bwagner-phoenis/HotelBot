using System.Text;

namespace HotelBot;

public class BookingDetails
{
    /// <summary>
    /// Initialize a new BookingDetails class and if a valid result is provided, fill the finds from the result.
    /// </summary>
    /// <param name="persons"></param>
    public BookingDetails(int persons)
    {
        // Initialize BookingDetails with any entities we may have found in the response.
        NumberOfGuests = persons;
    }

    /// <summary>
    /// Name of the guest making the reservation
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Total number of guests including children
    /// </summary>
    public int NumberOfGuests { get; set; }
    
    /// <summary>
    /// Breakfast Details like the drink choice are stored here
    /// </summary>
    public BreakfastDetails? Breakfast { get; set; }
    
    /// <summary>
    /// Arrival Date and Time 
    /// </summary>
    public string? Arrival { get; set; } = null!;

    /// <summary>
    /// Number of Children under 16 that are part of the NumberOfGuests value.
    /// The Default Value is set to -1 to account for 0 children.
    /// </summary>
    public int NumberOfChildren { get; set; } = -1;

    /// <summary>
    /// Number of nights the guests will be staying, counting from arrival
    /// </summary>
    public int NumberOfNights { get; set; }
    
    /// <summary>
    /// Payment method chosen by the user making the request 
    /// </summary>
    public string PaymentMethod { get; set; } = null!;
    
    /// <summary>
    /// Is a reserved parking lot required?
    /// </summary>
    public bool? ParkingLot { get; set; }
    
    /// <summary>
    /// Chosen pillow type the guests like
    /// </summary>
    public string PillowType { get; set; } = null!;
    
    /// <summary>
    /// Any known food allergies of the guests
    /// </summary>
    public string Allergies { get; set; } = null!;
    
    /// <summary>
    /// Is the age already verified?
    /// </summary>
    public bool? AgeVerified { get; set; }

    /// <summary>
    /// Overload default method to print the booking information in a human friendly way.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.AppendLine($"Details of your booking.");
        builder.AppendLine($"Name: {Name}");
        builder.AppendLine($"{NumberOfGuests} people from {Arrival} for {NumberOfNights} nights.");
        builder.AppendLine($"{(NumberOfChildren <= 0 ? "No" : NumberOfChildren.ToString())} children");
        builder.AppendLine($"{(Breakfast is not null ? Breakfast.ToString() : "Without Breakfast")}");
        builder.AppendLine(
            $"{(string.IsNullOrWhiteSpace(Allergies) ? "No known food allergies" : $"Known allergies: {Allergies}")}");
        builder.AppendLine(
            $"{(ParkingLot.GetValueOrDefault() ? "With reserved Parking Lot" : "Without reserved Parking Lot")}");
        builder.AppendLine($"Your preferred pillow type: {PillowType}");
        builder.AppendLine(
            $"{(AgeVerified.GetValueOrDefault() ? "Your age has been verified" : "Your age has not yet been verified")}");
        builder.AppendLine($"You will be paying with {PaymentMethod}");

        return builder.ToString();
    }
}
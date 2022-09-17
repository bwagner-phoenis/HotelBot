using System;
using System.Linq;
using System.Text;
using HotelBot.NLPModel;

namespace HotelBot;

public class BookingDetails
{
    /// <summary>
    /// Initialize a new BookingDetails class and if a valid result is provided, fill the finds from the result.
    /// </summary>
    /// <param name="request"></param>
    public BookingDetails(HotelBotResult._Entities.BookingRequestClass? request)
    {
        if (request is null) return;

        // Initialize BookingDetails with any entities we may have found in the response.
        NumberOfGuests = int.Parse(request.Adults?.FirstOrDefault() ?? "0");
        NumberOfChildren = int.Parse(request.Children?.FirstOrDefault() ?? "-1");
        Arrival = request.Arrival?.FirstOrDefault();
        NumberOfNights = int.Parse(request.Nights?.FirstOrDefault() ?? "0");
    }

    public string Name { get; set; } = "";
    public int NumberOfGuests { get; set; }
    public BreakfastDetails? Breakfast { get; set; }
    public string? Arrival { get; set; } = null!;

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
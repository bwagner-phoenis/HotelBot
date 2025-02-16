using System.Text;

namespace HotelBot;

/// <summary>
/// This object stores all relevant information regarding the breakfast choices
/// </summary>
public class BreakfastDetails
{
    /// <summary>
    /// The type of breakfast buffet choice selected by the user.
    /// None will indicate that no breakfast is booked
    /// </summary>
    public BreakfastTypes BreakfastType { get; set; }
    
    /// <summary>
    /// The preferred drink for the breakfast. If no breakfast is booked this will be None. 
    /// </summary>
    public MorningDrinks MorningDrink { get; set; }

    /// <summary>
    /// Custom ToString Method to print the chosen combination of buffet and drink.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var builder = new StringBuilder();

        if (BreakfastType == BreakfastTypes.None)
            return BreakfastType.ToDisplayName();

        builder.AppendLine($"Your breakfast choices:");
        builder.AppendLine(
            $"{BreakfastType.ToDisplayName()} with {MorningDrink.ToDisplayName()}");

        return builder.ToString();
    }
}

/// <summary>
/// Available breakfast options
/// </summary>
public enum BreakfastTypes
{
    None,
    Continental,
    FullEnglish,
    Traditional,
    Vegan,
    Buffet,
}

/// <summary>
/// Extenstion class with a method to print a user friendly name
/// </summary>
public static class BreakfastTypesExtension
{
    public static string ToDisplayName(this BreakfastTypes type) =>
        type switch
        {
            BreakfastTypes.Continental => "Continental breakfast",
            BreakfastTypes.FullEnglish => "Full english breakfast",
            BreakfastTypes.Traditional => "traditional breakfast",
            BreakfastTypes.Vegan => "Vegan breakfast",
            BreakfastTypes.Buffet => "Breakfast buffet",
            _ => "No breakfast booked"
        };
}

/// <summary>
/// Available drink options
/// </summary>
public enum MorningDrinks
{
    None,
    NoPreference,
    Coffee,
    BlackTea,
    GreenTea,
    HotChocolate,
}

/// <summary>
/// Extenstion class with a method to print a user friendly name
/// </summary>
public static class MorningDrinksExtension
{
    public static string ToDisplayName(this MorningDrinks drink) =>
        drink switch
        {
            MorningDrinks.Coffee => "Coffee",
            MorningDrinks.BlackTea => "Black tea",
            MorningDrinks.GreenTea => "Green tea",
            MorningDrinks.HotChocolate => "Hot chocolate",
            _ => "No preference"
        };
}
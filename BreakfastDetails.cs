using System;
using System.Text;

namespace HotelBot;

public class BreakfastDetails
{
    public BreakfastTypes BreakfastType { get; set; }
    public MorningDrinks MorningDrink { get; set; }

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

public enum BreakfastTypes
{
    None,
    Continental,
    FullEnglish,
    Traditional,
    Vegan,
    Buffet,
}

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

public enum MorningDrinks
{
    None,
    NoPreference,
    Coffee,
    BlackTea,
    GreenTea,
    HotChocolate,
}

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
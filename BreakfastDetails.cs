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

        builder.AppendLine($"Your breakfast choices:");
        builder.AppendLine($"{Enum.GetName(BreakfastType)} with {Enum.GetName(MorningDrink)}");
        
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

public enum MorningDrinks
{
    NoPreference,
    Coffee,
    BlackTea,
    GreenTea,
    HotChocolate,
}
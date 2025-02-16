using System.Collections.Generic;
using System.Linq;
using HotelBot.Clu;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace HotelBot.NLPModel;

/// <summary>
/// An <see cref="IRecognizerConvert"/> implementation that provides helper methods and properties to interact with
/// the CLU recognizer results.
/// </summary>
public class HotelBooking : IRecognizerConvert
{
    public enum Intent
    {
        None,
        Booking,
        HotelQuestion,
        Help,
        Confirm,
        Reject,
        Cancel,
    }

    public string Text { get; set; }

    public string AlteredText { get; set; }

    public Dictionary<Intent, IntentScore> Intents { get; set; }

    public CluEntities Entities { get; set; }

    public IDictionary<string, object> Properties { get; set; }

    public void Convert(dynamic result)
    {
        var jsonResult = JsonConvert.SerializeObject(result,
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Error = OnError
            });
        var app = JsonConvert.DeserializeObject<HotelBooking>(jsonResult);

        Text = app.Text;
        AlteredText = app.AlteredText;
        Intents = app.Intents;
        Entities = app.Entities;
        Properties = app.Properties;
    }

    public (Intent intent, double score) GetTopIntent()
    {
        var maxIntent = Intent.None;
        var max = 0.0;
        foreach (var entry in Intents)
        {
            if (entry.Value.Score > max)
            {
                maxIntent = entry.Key;
                max = entry.Value.Score.Value;
            }
        }

        return (maxIntent, max);
    }

    public class CluEntities
    {
        public CluEntity[] Entities;

        private CluEntity[] GetAdultsEntitiesList() =>
            Entities.Where(e => e.Category == "BookingRequest.Adults").ToArray();

        public string GetAdults => GetAdultsEntitiesList()[0]?.Text ?? "";
    }

    private static void OnError(object? sender, ErrorEventArgs args)
    {
        // If needed, put your custom error logic here
        Console.WriteLine(args.ErrorContext.Error.Message);
        args.ErrorContext.Handled = true;
    }
}
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;

namespace HotelBot.NLPModel;

public class LuisResult : IRecognizerConvert
{
    public enum Intent
    {
        Book_A_Room,
        Help,
        None,
        Trip_Date,
        With_Breakfast,
        Without_Breakfast
    }

    public _Entities Entities { get; set; } = new();

    public Dictionary<Intent, IntentScore> Intents { get; set; } = new();
    public string Text { get; set; } = "";
    public string AlteredText { get; set; } = "";

    [JsonExtensionData(ReadData = true, WriteData = true)]
    public Dictionary<string, object> Properties { get; set; } = new();

    public void Convert(dynamic result)
    {
        var app = JsonConvert.DeserializeObject<LuisResult>(JsonConvert.SerializeObject(result,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        Text = app.Text;
        AlteredText = app.AlteredText;
        Intents = app.Intents;
        Entities = app.Entities;
        Properties = app.Properties;
    }

    public (Intent intent, double score) TopIntent()
    {
        var maxIntent = Intent.None;
        var max = 0.0;
        foreach (var entry in Intents)
            if (entry.Value.Score > max)
            {
                maxIntent = entry.Key;
                max = entry.Value.Score.Value;
            }

        return (maxIntent, max);
    }

    public class _Entities
    {
        // Built-in entities
        public string? StartDate { get; set; } = null!;
        public string? EndDate { get; set; } = null!;
        public string? NumberOfGuests { get; set; } = null!;

        // Lists

        // Composites
        // public class _InstanceFrom
        // {
        //     public InstanceData[] Airport;
        // }
        // public class FromClass
        // {
        //     public string[][] Airport;
        //     [JsonProperty("$instance")]
        //     public _InstanceFrom _instance;
        // }
        // public FromClass[] From;
        //
        // public class _InstanceTo
        // {
        //     public InstanceData[] Airport;
        // }
        // public class ToClass
        // {
        //     public string[][] Airport;
        //     [JsonProperty("$instance")]
        //     public _InstanceTo _instance;
        // }
        // public ToClass[] To;
        //
        // // Instance
        // public class _Instance
        // {
        //     public InstanceData[] datetime;
        //     public InstanceData[] Airport;
        //     public InstanceData[] From;
        //     public InstanceData[] To;
        // }
        // [JsonProperty("$instance")]
        // public _Instance _instance;
    }
}
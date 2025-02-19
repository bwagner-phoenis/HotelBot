// <auto-generated>
// Code generated by luis:generate:cs
// Tool github: https://github.com/microsoft/botframework-cli
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Newtonsoft.Json.Converters;

namespace HotelBot.NLPModel
{
    public partial class HotelBotResult7: IRecognizerConvert
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("alteredText")]
        public string AlteredText;

        [JsonConverter(typeof(StringEnumConverter))]  
        public enum Intent {
            Booking,
            None,
            [JsonProperty("Utilities.Cancel")]
            Utilities_Cancel,
            [JsonProperty("Utilities.Confirm")]
            Utilities_Confirm,
            [JsonProperty("Utilities.Escalate")]
            Utilities_Escalate,
            [JsonProperty("Utilities.FinishTask")]
            Utilities_FinishTask,
            [JsonProperty("Utilities.Goback")]
            Utilities_GoBack,
            [JsonProperty("Utilities.Help")]
            Utilities_Help,
            [JsonProperty("Utilities.Reject")]
            Utilities_Reject,
            [JsonProperty("Utilities.Repeat")]
            Utilities_Repeat,
            [JsonProperty("Utilities.SelectAny")]
            Utilities_SelectAny,
            [JsonProperty("Utilities.SelectItem")]
            Utilities_SelectItem,
            [JsonProperty("Utilities.SelectNone")]
            Utilities_SelectNone,
            [JsonProperty("Utilities.ShowNext")]
            Utilities_ShowNext,
            [JsonProperty("Utilities.ShowPrevious")]
            Utilities_ShowPrevious,
            [JsonProperty("Utilities.StartOver")]
            Utilities_StartOver,
            [JsonProperty("Utilities.Stop")]
            Utilities_Stop
        };
        [JsonProperty("intents")]
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {
            // Simple entities
            public string[] Utilities_DirectionalReference;

            // Built-in entities
            public DateTimeSpec[] datetime;
            public double[] number;
            public double[] ordinal;


            // Composites
            public class _InstanceBookingRequest
            {
                public InstanceData[] Adults;
                public InstanceData[] Children;
                public InstanceData[] Nights;
                public InstanceData[] Arrival;
            }
            public class BookingRequestClass
            {
                public string[] Adults;
                public string[] Children;
                public string[] Nights;
                public string[] Arrival;
                [JsonProperty("$instance")]
                public _InstanceBookingRequest _instance;
            }
            public BookingRequestClass[] BookingRequest;

            // Instance
            public class _Instance
            {
                public InstanceData[] Adults;
                public InstanceData[] Arrival;
                public InstanceData[] BookingRequest;
                public InstanceData[] Children;
                public InstanceData[] Nights;
                public InstanceData[] Utilities_DirectionalReference;
                public InstanceData[] datetime;
                public InstanceData[] number;
                public InstanceData[] ordinal;
            }
            [JsonProperty("$instance")]
            public _Instance _instance;
        }
        [JsonProperty("entities")]
        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties {get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<HotelBotResult>(
                JsonConvert.SerializeObject(
                    result,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Error = OnError }
                )
            );
            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        private static void OnError(object sender, ErrorEventArgs args)
        {
            // If needed, put your custom error logic here
            Console.WriteLine(args.ErrorContext.Error.Message);
            args.ErrorContext.Handled = true;
        }

        public (Intent intent, double score) TopIntent()
        {
            Intent maxIntent = Intent.None;
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
    }
}

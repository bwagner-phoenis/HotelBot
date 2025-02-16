using Newtonsoft.Json;

namespace HotelBot.Clu;

/// <summary>
/// DTO class to hold the returned entity information from CLU recognition.
/// </summary>
public class CluEntity
{
    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("offset")]
    public int Offset { get; set; }

    [JsonProperty("length")]
    public int Length { get; set; }

    [JsonProperty("confidenceScore")]
    public float ConfidenceScore { get; set; }
}
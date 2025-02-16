namespace HotelBot;

/// <summary>
/// Mapping class from the setting values in the appsettings.json file
/// </summary>
public class CluSettings
{
    public string MicrosoftAppId { get; set; } = "";
    public string MicrosoftAppPassword { get; set; } = "";
    public string CluProjectName { get; set; } = "";
    public string CluDeploymentName { get; set; } = "";
    public string CluAPIKey { get; set; } = "";
    public string CluAPIHostName { get; set; } = "";
}
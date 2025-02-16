using System.Threading;
using System.Threading.Tasks;
using HotelBot.Clu;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Options;

namespace HotelBot;

/// <summary>
/// Implementation of the Recognizer Interface using LUIS from Microsoft
/// </summary>
public class HotelRecognizer : IRecognizer
{
    private readonly CluRecognizer? _recognizer = null;

    public HotelRecognizer(IOptions<CluSettings> configuration)
    {
        var cluIsConfigured = !string.IsNullOrEmpty(configuration.Value.CluProjectName)
                              && !string.IsNullOrEmpty(configuration.Value.CluDeploymentName)
                              && !string.IsNullOrEmpty(configuration.Value.CluAPIKey)
                              && !string.IsNullOrEmpty(configuration.Value.CluAPIHostName);

        if (!cluIsConfigured) return;

        var cluApplication = new CluApplication(
            configuration.Value.CluProjectName,
            configuration.Value.CluDeploymentName,
            configuration.Value.CluAPIKey,
            configuration.Value.CluAPIHostName);
        // Set the recognizer options depending on which endpoint version you want to use.
        var recognizerOptions = new CluOptions(cluApplication)
        {
            Language = "en",
            Verbose = true,
        };

        _recognizer = new CluRecognizer(recognizerOptions);
    }

    /// <summary>
    /// This property indicates that the constructor ran successfully and the recognizer can be used. 
    /// </summary>
    public virtual bool IsConfigured => _recognizer != null;

    /// <summary>
    /// Runs an utterance through a recognizer and returns a generic recognizer result.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis of utterance.</returns>
    public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext,
        CancellationToken cancellationToken)
        => await _recognizer?.RecognizeAsync(turnContext, cancellationToken)!;

    /// <summary>
    /// Runs an utterance through a recognizer and returns a strongly-typed recognizer result.
    /// </summary>
    /// <typeparam name="T">The recognition result type.</typeparam>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis of utterance.</returns>
    public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
        where T : IRecognizerConvert, new()
        => await _recognizer?.RecognizeAsync<T>(turnContext, cancellationToken)!;
}
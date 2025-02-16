using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.Language.Conversations;
using Azure.Core;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TraceExtensions;
using Newtonsoft.Json.Linq;

namespace HotelBot.Clu;

/// <summary>
/// Class for a recognizer that utilizes the CLU service.
/// </summary>
public class CluRecognizer : IRecognizer
{
    /// <summary>
    /// The context label for a CLU trace activity.
    /// </summary>
    private const string CluTraceLabel = "CLU Trace";

    /// <summary>
    /// Key used when adding Question Answering into to  <see cref="RecognizerResult"/> intents collection.
    /// </summary>
    public const string QuestionAnsweringMatchIntent = "QuestionAnsweringMatch";

    /// <summary>
    /// The Conversation Analysis Client instance that handles calls to the service.
    /// </summary>
    private readonly ConversationAnalysisClient _conversationsClient;

    /// <summary>
    /// CLU Recognizer Options
    /// </summary>
    private readonly CluOptions _options;

    /// <summary>
    /// The CluRecognizer constructor.
    /// </summary>
    public CluRecognizer(CluOptions options, ConversationAnalysisClient? conversationAnalysisClient = null)
    {
        // for mocking purposes
        _conversationsClient = conversationAnalysisClient ?? new ConversationAnalysisClient(
            new Uri(options.CluApplication.Endpoint),
            new AzureKeyCredential(options.CluApplication.EndpointKey));
        _options = options;
    }

    /// <summary>
    /// The RecognizeAsync function used to recognize the intents and entities in the utterance present in the turn context. 
    /// The function uses the options provided in the constructor of the CluRecognizer object.
    /// </summary>
    public async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        var utterance = turnContext.Activity?.AsMessageActivity()?.Text ?? "";
        var conversationId = turnContext.Activity?.Conversation.Id ?? "1";
        var participantId = turnContext.Activity?.From?.Id ?? "1";
        
        return await RecognizeInternalAsync(utterance, turnContext, cancellationToken);
    }

    /// <summary>
    /// The RecognizeAsync overload of template type T that allows the user to define their own implementation of the IRecognizerConvert class.
    /// </summary>
    public async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
        where T : IRecognizerConvert, new()
    {
        var result = new T();
        
        var utterance = turnContext.Activity?.AsMessageActivity()?.Text ?? "";
        var conversationId = turnContext.Activity?.Conversation.Id ?? "1";
        var participantId = turnContext.Activity?.From?.Id ?? "1";
        
        result.Convert(await RecognizeInternalAsync(utterance, turnContext, cancellationToken));
        return result;
    }

    private async Task<RecognizerResult> RecognizeInternalAsync(string utterance, ITurnContext turnContext, CancellationToken cancellationToken)
    {

        var request = new
        {
            analysisInput = new
            {
                conversationItem = new
                {
                    text = utterance,
                    id = "1",
                    participantId = "1",
                }
            },
            parameters = new
            {
                projectName = _options.CluApplication.ProjectName,
                deploymentName = _options.CluApplication.DeploymentName,

                // Use Utf16CodeUnit for strings in .NET.
                stringIndexType = "Utf16CodeUnit",
            },
            kind = "Conversation",
        };


        var cluResponse = await _conversationsClient.AnalyzeConversationAsync(RequestContent.Create(request));
        using JsonDocument result = JsonDocument.Parse(cluResponse.ContentStream);
        var recognizerResult = RecognizerResultBuilder.BuildRecognizerResultFromCluResponse(result, utterance);

        var traceInfo = JObject.FromObject(
            new
            {
                response = result,
                recognizerResult,
            });

        await turnContext.TraceActivityAsync("CLU Recognizer", traceInfo, nameof(CluRecognizer), CluTraceLabel, cancellationToken);

        return recognizerResult;
    }
}
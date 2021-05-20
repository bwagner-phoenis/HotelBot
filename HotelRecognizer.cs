using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Options;

namespace HotelBot
{
    public class HotelRecognizer : IRecognizer
    {
        private readonly LuisRecognizer? _recognizer = null;
        
        public HotelRecognizer(IOptions<LuisSettings> settings)
        {
            var configuration = settings.Value;
            
            var luisIsConfigured = !string.IsNullOrEmpty(configuration.LuisAppId) && !string.IsNullOrEmpty(configuration.LuisAPIKey) && !string.IsNullOrEmpty(configuration.LuisAPIHostName);

            if (!luisIsConfigured) return;
            
            var luisApplication = new LuisApplication(
                configuration.LuisAppId,
                configuration.LuisAPIKey,
                configuration.LuisAPIHostName);

            var recognizerOptions = new LuisRecognizerOptionsV3(luisApplication)
            {
                PredictionOptions = new Microsoft.Bot.Builder.AI.LuisV3.LuisPredictionOptions()
                {
                    IncludeInstanceData = true,
                }
            };

            _recognizer = new LuisRecognizer(recognizerOptions);
        }

        public virtual bool IsConfigured => _recognizer != null;

        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
            => await _recognizer?.RecognizeAsync(turnContext, cancellationToken)!;

        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
            => await _recognizer?.RecognizeAsync<T>(turnContext, cancellationToken)!;
    }
}
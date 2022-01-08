using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System.Threading;
using System.Threading.Tasks;
using HotelBot.NLPModel;
using Microsoft.Extensions.Logging;

namespace HotelBot.Dialogs
{
    public class RoomBookingDialog : BaseDialog
    {
        private const string DestinationStepMsgText = "How many guests are comming?";
        private const string OriginStepMsgText = "Do you like to book with breakfast?";
        private readonly ILogger<RoomBookingDialog> _logger;

        private HotelRecognizer _recognizer;
        
        public RoomBookingDialog(HotelRecognizer recognizer, ILogger<RoomBookingDialog> logger)
            : base(nameof(RoomBookingDialog))
        {
            _recognizer = recognizer;
            _logger = logger;
            
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new NumberPrompt<int>("IntPrompt"));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new BreakfastDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                NumberOfGuestStepAsync,
                BreakfastStepAsync,
                ArrivalDateStepAsync,
                DepartureDateStepAsync,
                // ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> NumberOfGuestStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingDetails)stepContext.Options;

            var luisResult = await _recognizer.RecognizeAsync<LuisResult>(stepContext.Context, cancellationToken);
            
            _logger.LogDebug($"LUIS Result: {luisResult.ToString()}");

            if (bookingDetails.NumberOfGuests == 0)
            {
                var promptMessage = MessageFactory.Text(DestinationStepMsgText, DestinationStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync("IntPrompt", new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(bookingDetails.NumberOfGuests, cancellationToken);
        }

        private async Task<DialogTurnResult> BreakfastStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingDetails)stepContext.Options;

            bookingDetails.NumberOfGuests = (int)stepContext.Result;

             if (bookingDetails.Breakfast == null)
             {
                 var promptMessage = MessageFactory.Text(OriginStepMsgText, OriginStepMsgText, InputHints.ExpectingInput);
                 return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
             }
        
             return await stepContext.NextAsync(bookingDetails.Breakfast, cancellationToken);
        }
        
        private async Task<DialogTurnResult> ArrivalDateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingDetails)stepContext.Options;

            var luisResult = await _recognizer.RecognizeAsync<LuisResult>(stepContext.Context, cancellationToken);

            if (luisResult?.TopIntent().intent == LuisResult.Intent.With_Breakfast)
            {
                bookingDetails.Breakfast = true;
                return await stepContext.BeginDialogAsync(nameof(BreakfastDialog), bookingDetails.Arrival, cancellationToken);
            }
            else if(luisResult?.TopIntent().intent == LuisResult.Intent.Without_Breakfast)
                bookingDetails.Breakfast = false;
            
            if (bookingDetails.Arrival == null || IsAmbiguous(bookingDetails.Arrival))
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), bookingDetails.Arrival, cancellationToken);
            }
        
            return await stepContext.NextAsync(bookingDetails.Arrival, cancellationToken);
        }
        
        private async Task<DialogTurnResult> DepartureDateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingDetails)stepContext.Options;
        
            bookingDetails.Arrival = (string)stepContext.Result;
        
            if (bookingDetails.Departure == null || IsAmbiguous(bookingDetails.Departure))
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), bookingDetails.Departure, cancellationToken);
            }
        
            return await stepContext.NextAsync(bookingDetails.Departure, cancellationToken);
        }
        // private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        // {
        //     var bookingDetails = (BookingDetails)stepContext.Options;
        //
        //     bookingDetails.TravelDate = (string)stepContext.Result;
        //
        //     var messageText = $"Please confirm, I have you traveling to: {bookingDetails.Destination} from: {bookingDetails.Origin} on: {bookingDetails.TravelDate}. Is this correct?";
        //     var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
        //
        //     return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        // }
        //
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!(bool)stepContext.Result) return await stepContext.EndDialogAsync(null, cancellationToken);
            
            var bookingDetails = (BookingDetails)stepContext.Options;
        
            return await stepContext.EndDialogAsync(bookingDetails, cancellationToken);

        }
        
        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        }
    }
}

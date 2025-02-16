using System.Threading;
using System.Threading.Tasks;
using HotelBot.NLPModel;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.Text.RegularExpressions;

namespace HotelBot.Dialogs;

/// <summary>
/// Main Dialog of the bot, from here the booking dialog will be started
/// </summary>
public partial class MainDialog : ComponentDialog
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();

    private readonly HotelRecognizer _recognizer;
    private readonly ILogger<MainDialog> _logger;

    private const string HelpMessage =
        "This Bot is designed to guide you through the process of booking a room in our hotel.\n\nTo start the booking process enter a prompt like **\"I need a room for 2 people\"**.\nIn the following dialog the bot will ask you further questions to gather the required information for the booking and to make your stay as pleasant as possible.\n\nIf you need help during the booking process you can type in **\"help\"**, or a related sentence, an you get assistance.";

    private const string QuestionMessage =
        "Here are some further information about our hotel:\n\n**Refunds**\nWe have a customer friendly refund policy. Ask and you shall recieve. What, when and how depends on our mood.\n\n**Room Availablity**\nAs long as this bot responds there are rooms available\n\n**Animals**\nPets are allowed as long as you clean up after them!\n\n**Sustainability**\nWe are a sustainable hotel and no matter how ofter you keep the towels on the floor we wash them only after you leave. To save water and stuff.";

    private const string PromptMessage = "What else can I do for you?";

    public MainDialog(HotelRecognizer recognizer, RoomBookingDialog bookingDialog, ILogger<MainDialog> logger) : base(
        nameof(MainDialog))
    {
        _recognizer = recognizer;
        _logger = logger;

        AddDialog(new TextPrompt(nameof(TextPrompt)));
        AddDialog(bookingDialog);
        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), [
            IntroStepAsync,
            ActStepAsync,
            FinalStepAsync
        ]));

        // The initial child Dialog to run.
        InitialDialogId = nameof(WaterfallDialog);
    }

    private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        if (!_recognizer.IsConfigured)
        {
            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text(
                    "NOTE: CLU is not configured. To enable all capabilities add the required information to the appsettings.json file.",
                    inputHint: InputHints.IgnoringInput), cancellationToken);

            return await stepContext.NextAsync(null, cancellationToken);
        }

        // Use the text provided in FinalStepAsync or the default if it is the first time.
        var messageText = stepContext.Options?.ToString() ??
                          "What can I help you with today?\n\nSay something like **\"I need a room for 4 people.\"** or **\"help\"** to get further hints.";
        var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
        return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage },
            cancellationToken);
    }

    private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        if (!_recognizer.IsConfigured)
            // CLU is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
            //return await stepContext.BeginDialogAsync(nameof(BookingDialog), new BookingDetails(), cancellationToken);
            _logger.LogError("CLU Configuration is either not working or not properly set!");

        // Call CLU and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
        var cluResult = await _recognizer.RecognizeAsync<HotelBooking>(stepContext.Context, cancellationToken);

        switch (cluResult.GetTopIntent().intent)
        {
            case HotelBooking.Intent.Booking:

                var personString = cluResult.Entities.GetAdults;

                var regEx = NumberRegex();
                var match = regEx.Match(personString);

                var personCount = int.Parse(match.Groups[0].Value);

                var bookingDetails = new BookingDetails(personCount);

                // Run the BookingDialog giving it whatever details we have from the CLU call, it will fill out the remainder.
                var result =
                    await stepContext.BeginDialogAsync(nameof(RoomBookingDialog), bookingDetails, cancellationToken);

                return result;

            case HotelBooking.Intent.Help:
                // Show the general help message to give the user some hints
                var helpMessage = MessageFactory.Text(HelpMessage, HelpMessage, InputHints.ExpectingInput);
                await stepContext.Context.SendActivityAsync(helpMessage, cancellationToken);
                break;

            case HotelBooking.Intent.HotelQuestion:
                // Show the general help message to give the user some hints
                var answerMessage = MessageFactory.Text(QuestionMessage, QuestionMessage, InputHints.ExpectingInput);
                await stepContext.Context.SendActivityAsync(answerMessage, cancellationToken);
                break;

            default:
                // Catch all for unhandled intents
                var didntUnderstandMessageText =
                    $"Sorry, I didn't get that. Please try asking in a different way (intent was {cluResult.GetTopIntent().intent})";
                var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText,
                    InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                break;
        }

        return await stepContext.NextAsync(null, cancellationToken);
    }

    private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
        // the Result here will be null.
        if (stepContext.Result is BookingDetails result)
        {
            // Now we have all the booking details call the booking service.
            // If the call to the booking service was successful tell the user.
            var messageText = $"Thank you for your booking!{ControlChars.NewLine}" +
                              $"A request is placed in our system and after a short check from an service desk employee a quote will be send to you for confirmation.{ControlChars.NewLine}" +
                              $"Your reservation is valid for 7 days and you can always call us if something is wrong or needs to be changed.{ControlChars.NewLine}" +
                              $"Thank you for using our chatbot service and we are looking forward to welcome you in our hotel!";

            var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
            await stepContext.Context.SendActivityAsync(message, cancellationToken);
        }

        // Restart the main dialog with a different message the second time around
        return await stepContext.ReplaceDialogAsync(InitialDialogId, PromptMessage, cancellationToken);
    }
}
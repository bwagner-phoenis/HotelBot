﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotelBot.NLPModel;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace HotelBot.Dialogs;

public class MainDialog : ComponentDialog
{
    private readonly HotelRecognizer _recognizer;
    private readonly ILogger<MainDialog> Logger;

    public MainDialog(HotelRecognizer recognizer, RoomBookingDialog bookingDialog, ILogger<MainDialog> logger) : base(
        nameof(MainDialog))
    {
        _recognizer = recognizer;
        Logger = logger;

        AddDialog(new TextPrompt(nameof(TextPrompt)));
        AddDialog(bookingDialog);
        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
        {
            IntroStepAsync,
            ActStepAsync
            //FinalStepAsync,
        }));

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
                    "NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.",
                    inputHint: InputHints.IgnoringInput), cancellationToken);

            return await stepContext.NextAsync(null, cancellationToken);
        }

        // Use the text provided in FinalStepAsync or the default if it is the first time.
        var messageText = stepContext.Options?.ToString() ??
                          "What can I help you with today?\nSay something like \"I need a room for 4 people.\"";
        var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
        return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage },
            cancellationToken);
    }

    private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        if (!_recognizer.IsConfigured)
            // LUIS is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
            //return await stepContext.BeginDialogAsync(nameof(BookingDialog), new BookingDetails(), cancellationToken);
            Logger.LogError("LUIS Configuration is either not working or not properly set!");

        // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
        //var luisResult = await _recognizer.RecognizeAsync<FlightBooking>(stepContext.Context, cancellationToken);
        var luisResult = await _recognizer.RecognizeAsync<HotelBotResult>(stepContext.Context, cancellationToken);

        switch (luisResult.TopIntent().intent)
        {
            case HotelBotResult.Intent.Booking:

                HotelBotResult._Entities.BookingRequestClass? request  = null;
                
                if(luisResult?.Entities?.BookingRequest is not null)
                    request = luisResult.Entities.BookingRequest.FirstOrDefault();

                var bookingDetails = new BookingDetails(request);
                
                // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                var result =  await stepContext.BeginDialogAsync(nameof(RoomBookingDialog), bookingDetails, cancellationToken);
                
                return result;

            case HotelBotResult.Intent.Utilities_Help:
                // We haven't implemented the GetWeatherDialog so we just display a TO-DO message.
                var getWeatherMessageText = "TODO: output some helpfull messages";
                var getWeatherMessage = MessageFactory.Text(getWeatherMessageText, getWeatherMessageText,
                    InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(getWeatherMessage, cancellationToken);
                break;

            default:
                // Catch all for unhandled intents
                var didntUnderstandMessageText =
                    $"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResult.TopIntent().intent})";
                var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText,
                    InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                break;
        }

        return await stepContext.NextAsync(null, cancellationToken);
    }

    // Shows a warning if the requested From or To cities are recognized as entities but they are not in the Airport entity list.
    // In some cases LUIS will recognize the From and To composite entities as a valid cities but the From and To Airport values
    // will be empty if those entity values can't be mapped to a canonical item in the Airport.
    /*private static async Task ShowWarningForUnsupportedCities(ITurnContext context, FlightBooking luisResult, CancellationToken cancellationToken)
    {
        var unsupportedCities = new List<string>();

        var fromEntities = luisResult.FromEntities;
        if (!string.IsNullOrEmpty(fromEntities.From) && string.IsNullOrEmpty(fromEntities.Airport))
        {
            unsupportedCities.Add(fromEntities.From);
        }

        var toEntities = luisResult.ToEntities;
        if (!string.IsNullOrEmpty(toEntities.To) && string.IsNullOrEmpty(toEntities.Airport))
        {
            unsupportedCities.Add(toEntities.To);
        }

        if (unsupportedCities.Any())
        {
            var messageText = $"Sorry but the following airports are not supported: {string.Join(',', unsupportedCities)}";
            var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
            await context.SendActivityAsync(message, cancellationToken);
        }
    }

    private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
        // the Result here will be null.
        if (stepContext.Result is BookingDetails result)
        {
            // Now we have all the booking details call the booking service.

            // If the call to the booking service was successful tell the user.

            var timeProperty = new TimexProperty(result.TravelDate);
            var travelDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
            var messageText = $"I have you booked to {result.Destination} from {result.Origin} on {travelDateMsg}";
            var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
            await stepContext.Context.SendActivityAsync(message, cancellationToken);
        }

        // Restart the main dialog with a different message the second time around
        var promptMessage = "What else can I do for you?";
        return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
    }*/
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HotelBot.NLPModel;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.VisualBasic;
using Constants = Microsoft.Recognizers.Text.DataTypes.TimexExpression.Constants;

namespace HotelBot.Dialogs;

public class RoomBookingDialog : BaseDialog
{
    private const string NumberOfGuestsStepMsgText = "How many guests are comming?";
    private const string NumberOfChildrenStepMsgText = "How many children under 16 are part of the guests?";
    private const string BreakfastStepMsgText = "Do you like to book with breakfast?";
    private const string NumberOfNightsMsgText = "How many nights do you plan to stay?";
    private const string PaymentMethodMsgText = "Please select your desired payment method.";
    private const string ParkingLotRequiredMsgText = "Will you need a reserved parking lot in front of the hotel?";

    private const string PillowTypeMsgText =
        $"We have variaty of pillows available for you.{ControlChars.NewLine}Please choose the type you like.";

    private const string AllergiesMsgText = "Please enter all relevant allergies we need to be aware of.";
    private const string AgeVerificationMsgText = "Please verify that you are 18 or older.";

    private const string NameMsgText = "Please gives us your name for the reservation.";
    
    private const string ConfirmMsgText =
        "Here we have a summary of your booking. Please confirm that everything is correct or start again.";

    private const string FinalMsgText = "Thank you for your booking!";

    private readonly ILogger<RoomBookingDialog> _logger;

    private readonly IList<Choice> _paymentChoices = ChoiceFactory.ToChoices(new List<string>
    {
        "Invoice",
        "Credit Card",
        "Debit Card",
        "Cash on arrival",
        "Cash when leaving"
    });

    private readonly IList<Choice> _pillowChoices = ChoiceFactory.ToChoices(new List<string>
    {
        "Swiss stone pine",
        "Down Feathers",
        "Memory Foam",
        "Gel foam"
    });

    private readonly HotelRecognizer _recognizer;

    public RoomBookingDialog(HotelRecognizer recognizer, ILogger<RoomBookingDialog> logger)
        : base(nameof(RoomBookingDialog))
    {
        _recognizer = recognizer;
        _logger = logger;

        //Add the needed prompts and other dialog steps
        AddDialog(new TextPrompt(nameof(TextPrompt)));
        AddDialog(new NumberPrompt<int>("IntPrompt"));
        AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
        AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
        //Add custom Dialogs for special handlings
        AddDialog(new DateResolverDialog());
        AddDialog(new BreakfastDialog());
        //Create the main Dialog
        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
        {
            NumberOfGuestStepAsync,
            NumberOfChildrenStepAsync,
            BreakfastStepAsync,
            ArrivalDateStepAsync,
            NumberOfNightsStepAsync,
            PaymentMethodStepAsync,
            ParkingLotRequiredStepAsync,
            PillowTypeStepAsync,
            AllergiesStepAsync,
            NameQuestionStepAsync,
            AgeVerificationStepAsync,
            ConfirmStepAsync,
            FinalStepAsync
        }));

        // The initial child Dialog to run.
        InitialDialogId = nameof(WaterfallDialog);
    }

    /// <summary>
    ///     Step 1: Get the number of guest
    /// </summary>
    /// <param name="stepContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<DialogTurnResult> NumberOfGuestStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        var luisResult = await _recognizer.RecognizeAsync<HotelBotResult>(stepContext.Context, cancellationToken);

        _logger.LogDebug($"LUIS Result: {luisResult}");

        if (bookingDetails.NumberOfGuests != 0)
            return await stepContext.NextAsync(bookingDetails.NumberOfGuests, cancellationToken);

        var promptMessage = MessageFactory.Text(NumberOfGuestsStepMsgText, NumberOfGuestsStepMsgText,
            InputHints.ExpectingInput);
        return await stepContext.PromptAsync("IntPrompt", new PromptOptions { Prompt = promptMessage },
            cancellationToken);
    }

    /// <summary>
    ///     Step 2: Get the Number of children
    /// </summary>
    /// <param name="stepContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<DialogTurnResult> NumberOfChildrenStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        bookingDetails.NumberOfGuests = (int)stepContext.Result;

        if (bookingDetails.NumberOfChildren != -1)
            return await stepContext.NextAsync(bookingDetails.NumberOfChildren, cancellationToken);

        var promptMessage = MessageFactory.Text(NumberOfChildrenStepMsgText, NumberOfChildrenStepMsgText,
            InputHints.ExpectingInput);
        return await stepContext.PromptAsync("IntPrompt", new PromptOptions { Prompt = promptMessage },
            cancellationToken);
    }

    /// <summary>
    ///     Step 3: Handle Breakfast booking
    /// </summary>
    /// <param name="stepContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<DialogTurnResult> BreakfastStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        bookingDetails.NumberOfChildren = (int)stepContext.Result;

        if (bookingDetails.Breakfast != null)
            return await stepContext.NextAsync(bookingDetails.Breakfast, cancellationToken);

        var promptMessage =
            MessageFactory.Text(BreakfastStepMsgText, BreakfastStepMsgText, InputHints.ExpectingInput);
        return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage },
            cancellationToken);
    }

    /// <summary>
    ///     Step 4: Get the Arrival date
    /// </summary>
    /// <param name="stepContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<DialogTurnResult> ArrivalDateStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        // var luisResult = await _recognizer.RecognizeAsync<HotelBotResult>(stepContext.Context, cancellationToken);
        //
        // if (luisResult.TopIntent().intent == HotelBotResult.Intent.With_Breakfast)
        // {
        //     bookingDetails.Breakfast = true;
        //     return await stepContext.BeginDialogAsync(nameof(BreakfastDialog), bookingDetails.Arrival,
        //         cancellationToken);
        // }
        //
        // if (luisResult.TopIntent().intent == HotelBotResult.Intent.Without_Breakfast) bookingDetails.Breakfast = false;

        bookingDetails.Breakfast = (bool)stepContext.Result;
        if (bookingDetails.Breakfast.GetValueOrDefault())
        {
            return await stepContext.BeginDialogAsync(nameof(BreakfastDialog), bookingDetails.Arrival,
                cancellationToken);
        }

        if (bookingDetails.Arrival == null || IsAmbiguous(bookingDetails.Arrival))
            return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), bookingDetails.Arrival,
                cancellationToken);

        return await stepContext.NextAsync(bookingDetails.Arrival, cancellationToken);
    }

    /// <summary>
    ///     Step 5: Get the number of nights the guests are staying
    /// </summary>
    /// <param name="stepContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<DialogTurnResult> NumberOfNightsStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        bookingDetails.Arrival = (string)stepContext.Result;

        if (bookingDetails.NumberOfNights != 0)
            return await stepContext.NextAsync(bookingDetails.NumberOfNights, cancellationToken);

        var promptMessage = MessageFactory.Text(NumberOfNightsMsgText, NumberOfNightsMsgText,
            InputHints.ExpectingInput);
        return await stepContext.PromptAsync("IntPrompt", new PromptOptions { Prompt = promptMessage },
            cancellationToken);
    }

    private async Task<DialogTurnResult> PaymentMethodStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        bookingDetails.NumberOfNights = (int)stepContext.Result;

        if (!string.IsNullOrWhiteSpace(bookingDetails.PaymentMethod))
            return await stepContext.NextAsync(bookingDetails.PaymentMethod, cancellationToken);

        var promptMessage =
            MessageFactory.Text(PaymentMethodMsgText, PaymentMethodMsgText, InputHints.ExpectingInput);

        return await stepContext.PromptAsync("ChoicePrompt", new PromptOptions
        {
            Prompt = promptMessage,
            Choices = _paymentChoices,
            Style = ListStyle.HeroCard
        }, cancellationToken);
    }

    private async Task<DialogTurnResult> ParkingLotRequiredStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        bookingDetails.PaymentMethod = ((FoundChoice)stepContext.Result).Value;

        if (bookingDetails.ParkingLot.HasValue)
            return await stepContext.NextAsync(bookingDetails.ParkingLot, cancellationToken);

        var promptMessage = MessageFactory.Text(ParkingLotRequiredMsgText, ParkingLotRequiredMsgText,
            InputHints.ExpectingInput);

        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage },
            cancellationToken);
    }

    private async Task<DialogTurnResult> PillowTypeStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        bookingDetails.ParkingLot = (bool)stepContext.Result;

        if (string.IsNullOrWhiteSpace(bookingDetails.PillowType))
        {
            var promptMessage =
                MessageFactory.Text(PillowTypeMsgText, PillowTypeMsgText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync("ChoicePrompt", new PromptOptions
            {
                Prompt = promptMessage,
                Choices = _pillowChoices,
                Style = ListStyle.Auto
            }, cancellationToken);
        }

        return await stepContext.NextAsync(bookingDetails.PillowType, cancellationToken);
    }

    private async Task<DialogTurnResult> AllergiesStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        bookingDetails.PillowType = ((FoundChoice)stepContext.Result).Value;

        if (!string.IsNullOrWhiteSpace(bookingDetails.Allergies))
            return await stepContext.NextAsync(bookingDetails.Allergies, cancellationToken);

        var promptMessage = MessageFactory.Text(AllergiesMsgText, AllergiesMsgText,
            InputHints.ExpectingInput);
        return await stepContext.PromptAsync("TextPrompt", new PromptOptions { Prompt = promptMessage },
            cancellationToken);
    }

    private async Task<DialogTurnResult> NameQuestionStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        bookingDetails.Allergies = (string)stepContext.Result;

        if (!string.IsNullOrWhiteSpace(bookingDetails.Name))
            return await stepContext.NextAsync(bookingDetails.Name, cancellationToken);

        var promptMessage = MessageFactory.Text(NameMsgText, NameMsgText,
            InputHints.ExpectingInput);
        return await stepContext.PromptAsync("TextPrompt", new PromptOptions { Prompt = promptMessage },
            cancellationToken);
    }
    
    private async Task<DialogTurnResult> AgeVerificationStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        bookingDetails.Name = (string)stepContext.Result;

        if (bookingDetails.AgeVerified.HasValue)
            return await stepContext.NextAsync(bookingDetails.AgeVerified, cancellationToken);

        var promptMessage = MessageFactory.Text(AgeVerificationMsgText, AgeVerificationMsgText,
            InputHints.ExpectingInput);
        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage },
            cancellationToken);
    }

    private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        bookingDetails.AgeVerified = (bool)stepContext.Result;

        var summaryMsg = ConfirmMsgText + ControlChars.NewLine + bookingDetails;
        var promptMessage = MessageFactory.Text(summaryMsg, summaryMsg,
            InputHints.ExpectingInput);
        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage },
            cancellationToken);
    }

    private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        if (!(bool)stepContext.Result) return await stepContext.EndDialogAsync(null, cancellationToken);

        var bookingDetails = (BookingDetails)stepContext.Options;

        return await stepContext.EndDialogAsync(bookingDetails, cancellationToken);
    }

    private static bool IsAmbiguous(string timex)
    {
        CultureInfo[] cultures =
        {
            CultureInfo.CreateSpecificCulture("en-US"),
            CultureInfo.CreateSpecificCulture("de-DE")
        };

        var dateValue = DateTime.MinValue;

        foreach (var culture in cultures)
        {
            try
            {
                dateValue = DateTime.Parse(timex, culture);
            }
            catch (FormatException)
            {
                //ignore the format exception and continue with the next culture
            }
        }

        var timexProperty = TimexProperty.FromDate(dateValue);
        return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
    }
}
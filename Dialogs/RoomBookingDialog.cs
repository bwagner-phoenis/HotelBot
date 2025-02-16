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
    // Defining string constants for the use in the dialog later.

    private const string NumberOfGuestsStepMsgText = "How many guests are comming?";
    private const string NumberOfChildrenStepMsgText = "How many children under 16 are part of the guests?";
    private const string BreakfastStepMsgText = "Do you like to book with breakfast?";
    private const string NumberOfNightsMsgText = "How many nights do you plan to stay?";
    private const string PaymentMethodMsgText = "Please select your desired payment method.";
    private const string ParkingLotRequiredMsgText = "Will you need a reserved parking lot in front of the hotel?";

    private const string PillowTypeMsgText =
        $"We have a variaty of pillows available for you to choose.{ControlChars.NewLine}Please select the type you like.";

    private const string AllergiesMsgText =
        $"Please enter all relevant food or other allergies we need to be aware of.{ControlChars.NewLine}If you have no allergies enter \"none\"";

    private const string AgeVerificationMsgText = "Please verify that you are atleast 18 years old.";

    private const string NameMsgText = "Please gives us your full name for the reservation.";

    private const string ConfirmMsgText =
        "Here we have a summary of your booking. Please confirm that everything is correct or say \"start again\".";

    private const string BreakfastTypeMsgText = "What kind of breakfast do you like?";

    private const string HotDrinkMsgText = "What kind of hot drink do you like?" + ControlChars.NewLine +
                                           "If you don't know or like to try different options please choose\"I will decide then\".";

    // Logging Component for the use in this dialog, the instance to use is injected in the constructor
    private readonly ILogger<RoomBookingDialog> _logger;

    // In some steps the user can choose between given choices. These are defined here as the objects required
    // from the step components. Some are string lists others are created from enums
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

    private readonly IList<Choice> _breakfastChoices = ChoiceFactory.ToChoices(Enum.GetNames(typeof(BreakfastTypes)));
    private readonly IList<Choice> _hotDrinkChoices = ChoiceFactory.ToChoices(Enum.GetNames(typeof(MorningDrinks)));

    /// <summary>
    /// Constructor of the Dialog used to setup the prompts used in the dialog and initializes the
    /// used objects through dependency injection
    /// </summary>
    /// <param name="recognizer">Recognizer component for the use in this dialog.</param>
    /// <param name="logger">The concrete logger instance for the use in this dialog.</param>
    public RoomBookingDialog(HotelRecognizer recognizer, ILogger<RoomBookingDialog> logger)
        : base(nameof(RoomBookingDialog), recognizer)
    {
        _logger = logger;

        //Add the needed prompts and other dialog steps
        AddDialog(new TextPrompt(nameof(TextPrompt)));
        AddDialog(new NumberPrompt<int>("IntPrompt"));
        AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
        AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

        //Add custom Dialogs for special handling of dates
        AddDialog(new DateResolverDialog(recognizer));

        //Create the main Dialog with the different steps, the order is important since this is the order of the dialog flow
        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), [
            NumberOfGuestStepAsync,
            NumberOfChildrenStepAsync,
            BreakfastStepAsync,
            BreakfastChoiceStepAsync,
            CoffeeOrTeaStepAsync,
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
        ]));

        // The initial or first child Dialog to run.
        InitialDialogId = nameof(WaterfallDialog);
    }

    /// <summary>
    ///     Step 1: Get the number of guest 
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
    /// <returns></returns>
    private async Task<DialogTurnResult> NumberOfGuestStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        var cluResult = await Recognizer.RecognizeAsync<HotelBooking>(stepContext.Context, cancellationToken);
        _logger.LogDebug($"CLU Result: {cluResult}");

        if (bookingDetails.NumberOfGuests != 0)
            return await stepContext.NextAsync(bookingDetails.NumberOfGuests, cancellationToken);

        var promptMessage = MessageFactory.Text(NumberOfGuestsStepMsgText, NumberOfGuestsStepMsgText,
            InputHints.ExpectingInput);
        return await stepContext.PromptAsync("IntPrompt", new PromptOptions { Prompt = promptMessage },
            cancellationToken);
    }

    /// <summary>
    ///     Step 2: Store the number of guests from the preceding step and ask for the number of children, if not already supplied
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
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
    ///     Step 3: Stores the number of children and asks if breakfast is required. 
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
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
    ///     Step 4: If the guests wants to book with breakfast, ask for the buffet preferences otherwise continue.
    ///             Breakfast Type None indicates that no breakfast is required.
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
    /// <returns></returns>
    private async Task<DialogTurnResult> BreakfastChoiceStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var cluResult = await Recognizer.RecognizeAsync<HotelBooking>(stepContext.Context, cancellationToken);

        _logger.LogDebug($"CLU Result: {cluResult}");

        if (cluResult.GetTopIntent().intent == HotelBooking.Intent.Reject)
        {
            var bookingDetails = (BookingDetails)stepContext.Options;
            bookingDetails.Breakfast = new BreakfastDetails()
            {
                BreakfastType = BreakfastTypes.None,
                MorningDrink = MorningDrinks.None
            };

            return await stepContext.NextAsync(bookingDetails, cancellationToken);
        }

        var promptMessage =
            MessageFactory.Text(BreakfastTypeMsgText, BreakfastTypeMsgText, InputHints.ExpectingInput);
        return await stepContext.PromptAsync("ChoicePrompt", new PromptOptions
        {
            Prompt = promptMessage,
            Choices = _breakfastChoices,
            Style = ListStyle.HeroCard
        }, cancellationToken);
    }

    /// <summary>
    ///     Step 5: If breakfast Type is None continue with the next step, otherwise continue with asking for the drink preference.
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
    /// <returns></returns>
    private async Task<DialogTurnResult> CoffeeOrTeaStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        if (bookingDetails.Breakfast?.BreakfastType == BreakfastTypes.None)
        {
            return await stepContext.NextAsync(bookingDetails, cancellationToken);
        }

        if (Enum.TryParse<BreakfastTypes>(((FoundChoice)stepContext.Result).Value, out var type))
        {
            bookingDetails.Breakfast = new BreakfastDetails()
            {
                BreakfastType = type,
            };
        }

        var promptMessage = MessageFactory.Text(HotDrinkMsgText, HotDrinkMsgText, InputHints.ExpectingInput);

        return await stepContext.PromptAsync("ChoicePrompt", new PromptOptions
        {
            Prompt = promptMessage,
            Choices = _hotDrinkChoices,
            Style = ListStyle.HeroCard
        }, cancellationToken);
    }

    /// <summary>
    ///     Step 6: If a drink was selected store the information and continue with querying the arrival date.
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
    /// <returns></returns>
    private async Task<DialogTurnResult> ArrivalDateStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        if (stepContext.Result is MorningDrinks)
        {
            if (Enum.TryParse<MorningDrinks>(((FoundChoice)stepContext.Result).Value, out var type))
            {
                bookingDetails.Breakfast ??= new BreakfastDetails()
                {
                    BreakfastType = BreakfastTypes.None,
                };

                bookingDetails.Breakfast.MorningDrink = type;
            }
        }

        if (bookingDetails.Arrival == null || IsAmbiguous(bookingDetails.Arrival))
            return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), bookingDetails.Arrival,
                cancellationToken);

        return await stepContext.NextAsync(bookingDetails.Arrival, cancellationToken);
    }

    /// <summary>
    ///     Step 7: Store the arrival date and query for the number of nights the guests are staying
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
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

    /// <summary>
    ///     Step 8: After the number of nights is received and stored the payment method is the next required information.
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
    /// <returns></returns>
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

    /// <summary>
    ///     Step 9: Ask the user if a personalized parking lot is required
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
    /// <returns></returns>
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

    /// <summary>
    ///     Step 10: Ask the user for any preferences for the filling of the pillow
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
    /// <returns></returns>
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

    /// <summary>
    ///     Step 11: Ask the user for any known food allergies
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
    /// <returns></returns>
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

    /// <summary>
    ///     Step 12: Ask for the name in which the reservation is made.
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
    /// <returns></returns>
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

    /// <summary>
    ///     Step 13: In this step the user is asked to verify his age
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
    /// <returns></returns>
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

    /// <summary>
    ///     Step 14: In the final confirmation step the user is ask to confirm that all gathered information is correct
    ///              and that the reservation can be carried out.
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
    /// <returns></returns>
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

    /// <summary>
    ///     The final step directs the user back to the main dialog with the final result from the dialog or null if the dialog was not successful.
    /// </summary>
    /// <param name="stepContext">The context holds the information for this step and the BookingDetails object</param>
    /// <param name="cancellationToken">This token will indicate if a cancellation is required and cancel the step or other async operations</param>
    /// <returns></returns>
    private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        if (!(bool)stepContext.Result) return await stepContext.EndDialogAsync(null, cancellationToken);

        var bookingDetails = (BookingDetails)stepContext.Options;
        return await stepContext.EndDialogAsync(bookingDetails, cancellationToken);
    }

    /// <summary>
    /// Ambiguity check for returned timex strings. The german and english cultures are checked, others are ignored.
    /// </summary>
    /// <param name="timex">timex string that should be checked for ambiguity</param>
    /// <returns>true if ambiguous otherwise false</returns>
    private static bool IsAmbiguous(string timex)
    {
        CultureInfo[] cultures =
        [
            CultureInfo.CreateSpecificCulture("en-US"),
            CultureInfo.CreateSpecificCulture("de-DE")
        ];

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
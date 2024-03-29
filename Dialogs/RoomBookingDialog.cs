﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HotelBot.NLPModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
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
        $"We have a variaty of pillows available for you to choose.{ControlChars.NewLine}Please select the type you like.";

    private const string AllergiesMsgText = "Please enter all relevant food or other allergies we need to be aware of.";
    private const string AgeVerificationMsgText = "Please verify that you are atleast 18 years old.";

    private const string NameMsgText = "Please gives us your full name for the reservation.";

    private const string ConfirmMsgText =
        "Here we have a summary of your booking. Please confirm that everything is correct or say \"start again\".";

    private const string FinalMsgText = $"Thank you for your booking!{ControlChars.NewLine}" +
                                        $"A request is placed in our system and after a short check from an service desk employee a quote will be send to you for confirmation.{ControlChars.NewLine}" +
                                        $"Your reservation is valid for 7 days and you can always call us if something is wrong or needs to be changed.{ControlChars.NewLine}" +
                                        $"Thank you for using our chatbot service and we are looking forward to welcome you in our hotel!";

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

    private const string BreakfastTypeMsgText = "What kind of breakfast do you like?";

    private const string HotDrinkMsgText = "What kind of hot drink do you like?" + ControlChars.NewLine +
                                           "If you don't know or like to try different options please choose\"I will decide then\".";

    private readonly IList<Choice> _breakfastChoices = ChoiceFactory.ToChoices(Enum.GetNames(typeof(BreakfastTypes)));
    private readonly IList<Choice> _hotDrinkChoices = ChoiceFactory.ToChoices(Enum.GetNames(typeof(MorningDrinks)));

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
        //Create the main Dialog
        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
        {
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

    private async Task<DialogTurnResult> BreakfastChoiceStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var luisResult = await _recognizer.RecognizeAsync<HotelBotResult>(stepContext.Context, cancellationToken);

        _logger.LogDebug($"LUIS Result: {luisResult}");

        if (luisResult.TopIntent().intent == HotelBotResult.Intent.Utilities_Reject)
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

    private async Task<DialogTurnResult> CoffeeOrTeaStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BookingDetails)stepContext.Options;

        if (bookingDetails.Breakfast?.BreakfastType == BreakfastTypes.None)
        {
            return await stepContext.NextAsync(bookingDetails, cancellationToken);
        }

        Enum.TryParse<BreakfastTypes>(((FoundChoice)stepContext.Result).Value, out var type);

        bookingDetails.Breakfast = new BreakfastDetails()
        {
            BreakfastType = type,
        };

        var promptMessage = MessageFactory.Text(HotDrinkMsgText, HotDrinkMsgText, InputHints.ExpectingInput);

        return await stepContext.PromptAsync("ChoicePrompt", new PromptOptions
        {
            Prompt = promptMessage,
            Choices = _hotDrinkChoices,
            Style = ListStyle.HeroCard
        }, cancellationToken);
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

        if (stepContext.Result is MorningDrinks)
        {
            Enum.TryParse<MorningDrinks>(((FoundChoice)stepContext.Result).Value, out var type);

            bookingDetails.Breakfast ??= new BreakfastDetails()
            {
                BreakfastType = BreakfastTypes.None,
            };

            bookingDetails.Breakfast.MorningDrink = type;
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
        var finalActivity = MessageFactory.Text(FinalMsgText, FinalMsgText, InputHints.IgnoringInput);
        
        await stepContext.Context.SendActivityAsync(finalActivity, cancellationToken);
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
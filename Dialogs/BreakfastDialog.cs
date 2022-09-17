using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.VisualBasic;

namespace HotelBot.Dialogs;

public class BreakfastDialog : BaseDialog
{
    private const string BreakfastTypeMsgText = "What kind of breakfast do you like?";

    private const string HotDrinkMsgText = "What kind of hot drink do you like?" + ControlChars.NewLine +
                                           "If you don't know or like to try different options please choose\"I will decide then\".";

    private readonly IList<Choice> _breakfastChoices = ChoiceFactory.ToChoices(Enum.GetNames(typeof(BreakfastTypes)));
    private readonly IList<Choice> _hotDrinkChoices = ChoiceFactory.ToChoices(Enum.GetNames(typeof(MorningDrinks)));

    public BreakfastDialog(string id = nameof(BreakfastDialog))
        : base(id)
    {
        AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
        {
            InitialStepAsync,
            CoffeeOrTeaStepAsync,
            FinalStepAsync
        }));

        // The initial child Dialog to run.
        InitialDialogId = nameof(WaterfallDialog);
    }

    private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var promptMessage = MessageFactory.Text(BreakfastTypeMsgText, BreakfastTypeMsgText, InputHints.ExpectingInput);

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
        var bookingDetails = (BreakfastDetails)stepContext.Options;

        var tryParse = Enum.TryParse<BreakfastTypes>(((FoundChoice)stepContext.Result).Value, out var type);

        bookingDetails.BreakfastType = type;

        var promptMessage = MessageFactory.Text(HotDrinkMsgText, HotDrinkMsgText, InputHints.ExpectingInput);

        return await stepContext.PromptAsync("ChoicePrompt", new PromptOptions
        {
            Prompt = promptMessage,
            Choices = _hotDrinkChoices,
            Style = ListStyle.HeroCard
        }, cancellationToken);
    }

    private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
    {
        var bookingDetails = (BreakfastDetails)stepContext.Options;
        var tryParse = Enum.TryParse<MorningDrinks>(((FoundChoice)stepContext.Result).Value, out var drink);

        bookingDetails.MorningDrink = drink;
        
        return await stepContext.EndDialogAsync(bookingDetails, cancellationToken);
    }
}
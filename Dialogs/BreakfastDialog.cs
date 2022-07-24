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

    private readonly IList<Choice> _breakfastChoices = ChoiceFactory.ToChoices(new List<string>
    {
        "Continental",
        "Full English",
        "Traditional",
        "Vegan",
        "Buffet"
    });

    private readonly IList<Choice> _hotDrinkChoices = ChoiceFactory.ToChoices(new List<string>
    {
        "Coffee",
        "Black Tea",
        "Green Tea",
        "Hot Chocolate",
        "I will decide then"
    });

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
        var timex = (string)stepContext.Options;

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
        var timex = (string)stepContext.Options;

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
        var timex = ((List<DateTimeResolution>)stepContext.Result)[0].Timex;
        return await stepContext.EndDialogAsync(timex, cancellationToken);
    }
}
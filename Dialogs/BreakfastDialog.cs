using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace HotelBot.Dialogs
{
    public class BreakfastDialog : BaseDialog
    {
        private const string PromptMsgText = "What kind of breakfast do you like?";
        private IList<Choice> _breakfastChoices = ChoiceFactory.ToChoices(new List<string>() {
            "Continental",
            "Full English", 
            "Traditional", 
            "Vegan", 
            "Buffet",
        });
        
        
        public BreakfastDialog(string id = nameof(BreakfastDialog))
            : base(id)
        {
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                InitialStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var timex = (string)stepContext.Options;

            var promptMessage = MessageFactory.Text(PromptMsgText, PromptMsgText, InputHints.ExpectingInput);
            
            return await stepContext.PromptAsync("ChoicePrompt", new PromptOptions
            {
                Prompt = promptMessage,
                Choices = _breakfastChoices,
                Style = ListStyle.HeroCard,
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   
            var timex = ((List<DateTimeResolution>)stepContext.Result)[0].Timex;
            return await stepContext.EndDialogAsync(timex, cancellationToken);
        }
    }
}
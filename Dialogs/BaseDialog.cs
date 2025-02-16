using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;
using HotelBot.NLPModel;

namespace HotelBot.Dialogs;

/// <summary>
/// Base dialog from which the other dialogs all derive. In this Base Dialog the request for help and cancellation are handled so that they are always reachable. 
/// </summary>
/// <param name="id">Id of the dialog for identification</param>
/// <param name="recognizer">Recognizer to check for help or cancel intents, provided via dependency injection </param>
public class BaseDialog(string id, IRecognizer recognizer) : ComponentDialog(id)
{
    private const string HelpMsgText =
        "Depending on the question and state of the dialog your are in, there are several options available.\n\n" +
        "**Number of children**\n At this step your are asked for any children younger than 16 years. Just enter the number, if no kids are coming just enter 0.\n\n" +
        "**Yes/No Questions**\n Some questions are simple yes or no questions like **\"Do you like to book breakfast\"** any form of confirmation or rejection, like **\"Yes, please\"** are accepted\n\n" +
        "**Options**\n Some Questions offer you a choice from a range of options, like the pillow type. Just click the option you like or type the Name of the option.\n\n" +
        "**Cancelling**\n You can cancel the booking process at any time by typing **\"Cancel\"** or another form of canellation request. The Bot will then start over again.\n\n" +
        "**To continue reply to the question asked before**";

    private const string CancelMsgText =
        "I got your request to cancel the booking.\n\nAll entered data will be deleted and the bot is reset to the initial step.";

    protected IRecognizer Recognizer { get; set; } = recognizer;

    /// <summary>
    /// Before continuing the dialog this event is fired and used to check for cancellation or help request
    /// </summary>
    /// <param name="innerDc">Context of the dialog</param>
    /// <param name="cancellationToken">Cancellation token to indicate cancellation of the task</param>
    protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc,
        CancellationToken cancellationToken = default)
    {
        var result = await InterruptAsync(innerDc, cancellationToken);
        if (result != null)
        {
            return result;
        }

        return await base.OnContinueDialogAsync(innerDc, cancellationToken);
    }

    /// <summary>
    /// Checks for and handles help and cancellation requests  
    /// </summary>
    /// <param name="innerDc">Context of the dialog</param>
    /// <param name="cancellationToken">Cancellation token to indicate cancellation of the task</param>
    private async Task<DialogTurnResult?> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
    {
        if (innerDc.Context.Activity.Type != ActivityTypes.Message)
            return null;

        var cluResult = await Recognizer.RecognizeAsync<HotelBooking>(innerDc.Context, cancellationToken);

        switch (cluResult.GetTopIntent().intent)
        {
            case HotelBooking.Intent.Help:
                var helpMessage = MessageFactory.Text(HelpMsgText, HelpMsgText, InputHints.ExpectingInput);
                await innerDc.Context.SendActivityAsync(helpMessage, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Waiting);

            case HotelBooking.Intent.Cancel:
                var cancelMessage = MessageFactory.Text(CancelMsgText, CancelMsgText, InputHints.IgnoringInput);
                await innerDc.Context.SendActivityAsync(cancelMessage, cancellationToken);
                return await innerDc.CancelAllDialogsAsync(cancellationToken);
        }

        return null;
    }
}
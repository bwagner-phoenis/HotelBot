using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HotelBot.Bots;

// This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
// to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
// each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
// The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
// and the requirement is that all BotState objects are saved at the end of a turn.
public class DialogBotBase<T> : ActivityHandler
    where T : Dialog
{
    protected readonly BotState ConversationState;
    protected readonly Dialog Dialog;
    protected readonly ILogger<DialogBotBase<T>> Logger;
    protected readonly BotState UserState;

    public DialogBotBase(ConversationState conversationState, UserState userState, T dialog,
        ILogger<DialogBotBase<T>> logger)
    {
        ConversationState = conversationState;
        UserState = userState;
        Dialog = dialog;
        Logger = logger;
    }

    public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
    {
        await base.OnTurnAsync(turnContext, cancellationToken);

        // Save any state changes that might have occurred during the turn.
        await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Running dialog with Message Activity.");

        // Run the Dialog with the new message Activity.
        await Dialog.RunAsync(turnContext, 
            ConversationState.CreateProperty<DialogState>("DialogState"),
            cancellationToken);
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded,
        ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        foreach (var member in membersAdded)
        {
            // Greet anyone that was not the target (recipient) of this message.
            if (member.Id == turnContext.Activity.Recipient.Id) continue;

            var welcomeCard = CreateAdaptiveCardAttachment();
            var response = MessageFactory.Attachment(welcomeCard, ssml: "Welcome to Bot Framework!");
            await turnContext.SendActivityAsync(response, cancellationToken);
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"),
                cancellationToken);
        }
    }

    // Load attachment from embedded resource.
    private Attachment? CreateAdaptiveCardAttachment()
    {
        var cardResourcePath = GetType().Assembly.GetManifestResourceNames()
            .First(name => name.EndsWith("welcomeCard.json"));

        using var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath);
        if (stream is null)
            return null;

        using var reader = new StreamReader(stream);

        var adaptiveCard = reader.ReadToEnd();
        return new Attachment
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = JsonConvert.DeserializeObject(adaptiveCard)
        };
    }
}
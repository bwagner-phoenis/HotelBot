using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotelBot.Bots;

public class WelcomeBot : ActivityHandler
{
    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded,
        ITurnContext<IConversationUpdateActivity> turnContext, 
        CancellationToken cancellationToken)
    {
        await base.OnMembersAddedAsync(membersAdded, turnContext, cancellationToken);

        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(
                    MessageFactory.Text(
                        $"Hello I am the HotelBot!\r\n I am an interactive Chat Bot and there to assist you with booking the right room for your stay!"),
                    cancellationToken);
            }
        }
    }
}
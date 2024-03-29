﻿using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace HotelBot.Dialogs
{
    public class BaseDialog : ComponentDialog
    {
        private const string HelpMsgText = "Show help here";
        private const string CancelMsgText = "Cancelling...";

        public BaseDialog(string id)
            : base(id)
        {
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<DialogTurnResult?> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            if (innerDc.Context.Activity.Type != ActivityTypes.Message) 
                return null;
            
            var text = innerDc.Context.Activity.Text.ToLowerInvariant();

            switch (text)
            {
                case "help":
                case "?":
                    var helpMessage = MessageFactory.Text(HelpMsgText, HelpMsgText, InputHints.ExpectingInput);
                    await innerDc.Context.SendActivityAsync(helpMessage, cancellationToken);
                    return new DialogTurnResult(DialogTurnStatus.Waiting);

                case "cancel":
                case "quit":
                    var cancelMessage = MessageFactory.Text(CancelMsgText, CancelMsgText, InputHints.IgnoringInput);
                    await innerDc.Context.SendActivityAsync(cancelMessage, cancellationToken);
                    return await innerDc.CancelAllDialogsAsync(cancellationToken);
            }

            return null;
        }
    }
}

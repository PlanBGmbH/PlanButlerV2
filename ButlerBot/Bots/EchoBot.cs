using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace ButlerBot.Bots
{
    public class EchoBot<T> : ActivityHandler where T: Dialog
    {
        private const string WelcomeMessage = "Guten Tag, du schreibst gerade mit dem neuen PlanButler Bot. " +
                                              "Möchtest du Essen bestellen oder dir die Bestellmöglichkeiten anschauen?";

        private const string PatternMessage = "In der Aktuellen Version, verstehe ich" +
                                              "'Essen bestellen', 'Bestellmöglichkeiten' und 'Hilfe'";

        protected readonly Dialog Dialog;
        protected readonly BotState ConversationState;
        protected readonly BotState UserState;
        protected readonly ILogger Logger;

        public EchoBot(ConversationState conversationState, UserState userState, T dialog, ILogger<EchoBot<T>> logger)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            Logger = logger;
        }


        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync(PatternMessage, cancellationToken: cancellationToken);      
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text.ToLowerInvariant();
            switch (text)
            {
                case "essen bestellen":
                    await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                    break;
                case "bestellmöglichkeiten":
                    await turnContext.SendActivityAsync($"You said {text}.", cancellationToken: cancellationToken);
                    break;
                case "hilfe":
                    await SendHelpMessage(turnContext, cancellationToken);
                    break;
                default:
                    await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken: cancellationToken);
                    break;
            }
        }

        private static async Task SendHelpMessage(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken:cancellationToken);
            await turnContext.SendActivityAsync(PatternMessage, cancellationToken:cancellationToken);
        }
    }
}

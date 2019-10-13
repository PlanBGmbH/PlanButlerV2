// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.5.0

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace ButlerBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        private const string WelcomeMessage = "Guten Tag, du schreibst gerade mit dem neuen PlanButler Bot. " +
                                              "Möchtest du Essen bestellen oder dir die Bestellmöglichkeiten anschauen?";

        private const string PatternMessage = "In der Aktuellen Version, verstehe ich" +
                                              "'Essen bestellen', 'Bestellmöglichkeiten' und 'Hilfe'";


        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken: cancellationToken);
            await turnContext.SendActivityAsync(PatternMessage, cancellationToken: cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text.ToLowerInvariant();
            switch (text)
            {
                case "essen bestellen":
                    await turnContext.SendActivityAsync($"You said {text}.", cancellationToken: cancellationToken);
                    break;
                case "bestellmöglichkeiten":
                    await turnContext.SendActivityAsync($"You said {text}.", cancellationToken: cancellationToken);
                    break;
                case "help":
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

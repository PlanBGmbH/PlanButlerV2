namespace PlanB.Butler.Bot
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Logging;

    // This bot is derived (view DialogBot<T>) from the TeamsACtivityHandler class currently included as part of this sample.

    public class TeamsBot<T> : DialogBot<T> where T : Dialog
    {
        private const string WelcomeMessage = "Guten Tag, ich bin der PlanButler, dein Essensorganisator. Wie kann ich dir helfen?\n" +
            "Tippe irgendwas ein um fortzufahren.";

        public TeamsBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
            : base(conversationState, userState, dialog, logger)
        {
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken: cancellationToken);
        }

        protected override async Task OnSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            this.Logger.LogInformation("Running dialog with signin/verifystate from an Invoke Activity.");

            // The OAuth Prompt needs to see the Invoke Activity in order to complete the login process.

            // Run the Dialog with the new Invoke Activity.
            await this.Dialog.RunAsync(turnContext, this.ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }
    }
}

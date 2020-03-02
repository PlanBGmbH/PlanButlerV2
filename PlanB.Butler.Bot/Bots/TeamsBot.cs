// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace PlanB.Butler.Bot
{
    /// <summary>
    /// This bot is derived (view DialogBot<T>) from the TeamsACtivityHandler class currently included as part of this sample.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="PlanB.Butler.Bot.DialogBot{T}" />
    public class TeamsBot<T> : DialogBot<T> where T : Dialog
    {
        /// <summary>
        /// TeamsBotsWelcomeMessage.
        /// </summary>

        private static string teamBotsWelcomeMessage = string.Empty;
     
        public TeamsBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
            : base(conversationState, userState, dialog, logger)
        {
            ResourceManager rm = new ResourceManager("PlanB.Butler.Bot.Dictionary.Dialogs", Assembly.GetExecutingAssembly());
            teamBotsWelcomeMessage = rm.GetString("TeamBots_WelcomeMessage");
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {

            await turnContext.SendActivityAsync(teamBotsWelcomeMessage, cancellationToken: cancellationToken);
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

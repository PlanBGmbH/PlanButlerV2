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
    /// This bot is derived (view DialogBot) from the TeamsACtivityHandler class currently included as part of this sample.
    /// </summary>
    /// <typeparam name="T">Bot.</typeparam>
    /// <seealso cref="PlanB.Butler.Bot.DialogBot{T}" />
    public class TeamsBot<T> : DialogBot<T>
        where T : Dialog
    {
        /// <summary>
        /// TeamsBotsWelcomeMessage.
        /// </summary>
        private static string teamBotsWelcomeMessage = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsBot{T}"/> class.
        /// </summary>
        /// <param name="conversationState">State of the conversation.</param>
        /// <param name="userState">State of the user.</param>
        /// <param name="dialog">The dialog.</param>
        /// <param name="logger">The logger.</param>
        public TeamsBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
            : base(conversationState, userState, dialog, logger)
        {
            ResourceManager rm = new ResourceManager("PlanB.Butler.Bot.Dictionary.Dialogs", Assembly.GetExecutingAssembly());
            teamBotsWelcomeMessage = rm.GetString("TeamBots_WelcomeMessage");
        }

        /// <summary>
        /// Override this in a derived class to provide logic for when members other than the bot
        /// join the conversation, such as your bot's welcome logic.
        /// </summary>
        /// <param name="membersAdded">A list of all the members added to the conversation, as
        /// described by the conversation update activity.</param>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <remarks>
        /// When the <see cref="M:Microsoft.Bot.Builder.ActivityHandler.OnConversationUpdateActivityAsync(Microsoft.Bot.Builder.ITurnContext{Microsoft.Bot.Schema.IConversationUpdateActivity},System.Threading.CancellationToken)" />
        /// method receives a conversation update activity that indicates one or more users other than the bot
        /// are joining the conversation, it calls this method.
        /// </remarks>
        /// <seealso cref="M:Microsoft.Bot.Builder.ActivityHandler.OnConversationUpdateActivityAsync(Microsoft.Bot.Builder.ITurnContext{Microsoft.Bot.Schema.IConversationUpdateActivity},System.Threading.CancellationToken)" />
        /// <returns>Task.</returns>
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(teamBotsWelcomeMessage, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Call signin verify state asynchronous.
        /// </summary>
        /// <param name="turnContext">The turn context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        protected override async Task OnSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            this.Logger.LogInformation("Running dialog with signin/verifystate from an Invoke Activity.");

            // The OAuth Prompt needs to see the Invoke Activity in order to complete the login process.

            // Run the Dialog with the new Invoke Activity.
            await this.Dialog.RunAsync(turnContext, this.ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }
    }
}

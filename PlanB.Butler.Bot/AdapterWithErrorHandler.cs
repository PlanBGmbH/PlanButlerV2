// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace PlanB.Butler.Bot
{
    using System;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;

    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        public AdapterWithErrorHandler(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger, TelemetryInitializerMiddleware telemetryInitializerMiddleware, ConversationState conversationState = null)
            : base(configuration, logger)
        {
            Use(telemetryInitializerMiddleware);
            this.OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogError($"Exception caught : {exception.Message}");

                // Send a catch-all apology to the user.
                await turnContext.SendActivityAsync("Sorry, it looks like something went wrong.");
            };
        }
    }
}

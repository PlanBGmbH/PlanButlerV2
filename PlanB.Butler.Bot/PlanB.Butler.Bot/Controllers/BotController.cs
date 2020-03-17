// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Options;

namespace PlanB.Butler.Bot.Controllers
{
    /// <summary>
    /// This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    /// implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    /// achieved by specifying a more specific type for the bot constructor argument.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
    [Route("api/messages")]
    [ApiController]
    public class BotController : ControllerBase
    {
        /// <summary>
        /// The adapter.
        /// </summary>
        private readonly IBotFrameworkHttpAdapter adapter;

        /// <summary>
        /// The bot.
        /// </summary>
        private readonly IBot bot;

        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotController"/> class.
        /// </summary>
        /// <param name="botFrameworkHttpAdapter">The adapter.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="config">The configuration.</param>
        public BotController(IBotFrameworkHttpAdapter botFrameworkHttpAdapter, IBot bot, IOptions<BotConfig> config)
        {
            this.adapter = botFrameworkHttpAdapter;
            this.bot = bot;
            this.botConfig = config;
        }

        /// <summary>
        /// Posts the asynchronous.
        /// </summary>
        [HttpPost]
        public async Task PostAsync()
        {
            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await this.adapter.ProcessAsync(this.Request, this.Response, this.bot);
        }
    }
}

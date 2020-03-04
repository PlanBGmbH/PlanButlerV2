// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using PlanB.Butler.Bot;

namespace PlanB.Butler.Bot.Dialogs
{
    /// <summary>
    /// MainDialog.
    /// </summary>
    /// <seealso cref="PlanB.Butler.Bot.Dialogs.InterruptDialog" />
    public class MainDialog : InterruptDialog
    {
        /// <summary>
        /// The client factory.
        /// </summary>
        private readonly IHttpClientFactory clientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainDialog"/> class.
        /// </summary>
        /// <param name="telemetryClient">The telemetry client.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        public MainDialog(IBotTelemetryClient telemetryClient, IOptions<BotConfig> config, IHttpClientFactory httpClientFactory)
            : base(nameof(MainDialog), config, telemetryClient, httpClientFactory)
        {
            // Set the telemetry client for this and all child dialogs.
            this.TelemetryClient = telemetryClient;
            this.clientFactory = httpClientFactory;

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
                {
                    this.InitialStepAsync,
                };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // The initial child Dialog to run.
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
        }
    }
}

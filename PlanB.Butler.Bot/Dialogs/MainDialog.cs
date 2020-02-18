namespace PlanB.Butler.Bot
{
    using System.Collections.Generic;
    using System.Linq;
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

    public class MainDialog : InterruptDialog
    {
        public MainDialog(IBotTelemetryClient telemetryClient, IOptions<BotConfig> config)
            : base(nameof(MainDialog), config)
        {
            // Set the telemetry client for this and all child dialogs.
            this.TelemetryClient = telemetryClient;

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

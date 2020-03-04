using System;
using System.Threading;
using System.Threading.Tasks;

using BotLibraryV2;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Extensions.Options;

namespace PlanB.Butler.Bot.Dialogs
{
    /// <summary>
    /// ExcellDialog.
    /// </summary>
    /// <seealso cref="Microsoft.Bot.Builder.Dialogs.ComponentDialog" />
    public class ExcellDialog : ComponentDialog
    {
        [Obsolete("This has to be replaced")]
        private static string[] months = { "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember" };
        private static string indexer = "0";

        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        public ExcellDialog(IOptions<BotConfig> config, IBotTelemetryClient telemetryClient)
              : base(nameof(ExcellDialog))
        {
            this.botConfig = config;


            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                this.NameStepAsync,
                this.SelectMonthAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.AddDialog(new TextPrompt(nameof(TextPrompt)));
            this.AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            // The initial child Dialog to run.
            this.InitialDialogId = nameof(WaterfallDialog);



        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(
                                        nameof(ChoicePrompt),
                                        new PromptOptions
                                        {
                                            Prompt = MessageFactory.Text("Für Welchen Monat willst du die Abbrechnung?"),
                                            Choices = ChoiceFactory.ToChoices(months),
                                            Style = ListStyle.HeroCard,
                                        }, cancellationToken);

        }

        private async Task<DialogTurnResult> SelectMonthAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Month"] = ((FoundChoice)stepContext.Result).Value;

            for (int i = 0; i < months.Length; i++)
            {
                if (stepContext.Values["Month"].ToString() == months[i])
                {
                    indexer = Convert.ToString(i + 1);
                }
            }
            var orderList = await BotMethods.GetSalaryDeduction(indexer, this.botConfig.Value.GetSalaryDeduction);
            bool test = ExcelGenerator.Run(orderList);
            await stepContext.EndDialogAsync();
            return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
        }
    }
}

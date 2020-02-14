namespace ButlerBot
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using BotLibraryV2;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Choices;
    using Newtonsoft.Json;

    public class ExcellDialog : ComponentDialog
    {
        private static string[] months = { "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember" };
        private static string indexer = "0";
        public ExcellDialog()
              : base(nameof(ExcellDialog))
        {

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
                                            Prompt = MessageFactory.Text("FürWlchen Monat willst du die Abbrechnung?"),
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
            var orderList = await BotMethods.GetSalaryDeduction(indexer);
            bool test = getExcel.Run(orderList);
            await stepContext.EndDialogAsync();
            return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
        }
    }
}

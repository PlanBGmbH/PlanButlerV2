namespace ButlerBot
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Choices;
    using Microsoft.Bot.Schema;

    public class OverviewDialog : ComponentDialog
    {

        // In this Array you can Easy modify your choice List.
        private static string[] choices = { "Essen Bestellen",  "Für einen anderen Tag Essen bestellen", "Bestellung entfernen", "Monatliche Belastung anzeigen", "Tagesbestellung" };
        private static ComponentDialog[] dialogs;

        public OverviewDialog()
            : base(nameof(OverviewDialog))
        {
            OrderDialog orderDialog = new OrderDialog();
            NextOrder nextorderDialog = new NextOrder();
            PlanDialog planDialog = new PlanDialog();
            CreditDialog creditDialog = new CreditDialog();
            OrderForOtherDayDialog orderForAnotherDay = new OrderForOtherDayDialog();
            DeleteOrderDialog deletOrderDialog = new DeleteOrderDialog();
            List<ComponentDialog> dialogsList = new List<ComponentDialog>();
            DailyCreditDialog dailyCreditDialog = new DailyCreditDialog();
            //dialogsList.Add(orderDialog);
            dialogsList.Add(nextorderDialog);
            dialogsList.Add(orderForAnotherDay);
            //dialogsList.Add(planDialog);
            dialogsList.Add(deletOrderDialog);
            dialogsList.Add(creditDialog);
            dialogsList.Add(dailyCreditDialog);
            dialogs = dialogsList.ToArray();

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
                {
                    this.InitialStepAsync,
                    this.ForwardStepAsync,
                    this.FinalStepAsync,
                };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.AddDialog(new OrderDialog());
            this.AddDialog(new CreditDialog());
            this.AddDialog(new PlanDialog());
            this.AddDialog(new OrderForOtherDayDialog());
            this.AddDialog(new DeleteOrderDialog());
            this.AddDialog(new NextOrder());
            this.AddDialog(new DailyCreditDialog());
            this.AddDialog(new TextPrompt(nameof(TextPrompt)));
            this.AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            this.AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Wähle eines der unteren Ergeignisse aus oder schreibe Hilfe um zu erfahren was ich sonst noch alles kann."), cancellationToken);

            // Cards are sent as Attachments in the Bot Framework.
            // So we need to create a list of attachments for the reply activity.
            var attachments = new List<Attachment>();

            // Reply to the activity we received with an activity.
            var reply = MessageFactory.Attachment(attachments);
            List<string> choiceList = new List<string>();
            for (int i = 0; i < choices.Length; i++)
            {
                choiceList.Add(choices[i]);
            }

            return await stepContext.PromptAsync(
            nameof(ChoicePrompt),
            new PromptOptions
            {
                Prompt = MessageFactory.Text($"Was möchtest du tun?"),
                Choices = ChoiceFactory.ToChoices(choiceList),
                Style = ListStyle.HeroCard,
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ForwardStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["mainChoise"] = ((FoundChoice)stepContext.Result).Value;
            int indexer = 0;
            var text = stepContext.Values["mainChoise"];
            for (int i = 0; i < choices.Length; i++)
            {
                if (stepContext.Values["mainChoise"].ToString().ToLower() == choices[i].ToLower())
                {
                    indexer = i;
                }
            }

            if (text != null)
            {
                return await stepContext.BeginDialogAsync(dialogs[indexer].Id, null, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Tut mir Leid. Ich habe dich nicht verstanden. Bitte benutze Befehle, die ich kenne."), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}

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
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

    public class OverviewDialog : ComponentDialog
    {
        private static Plan plan = new Plan();
        private static int dayId;
        private static string[] weekDaysEN = { "monday", "tuesday", "wednesday", "thursday", "friday" };
        private static int indexer = 0;
        private static bool valid;

        // In this Array you can Easy modify your choice List.
        private static string[] choices = { "Essen Bestellen", "Für einen anderen Tag Essen bestellen", "Bestellung entfernen", "Monatliche Belastung anzeigen", "Tagesbestellung" };
        private static ComponentDialog[] dialogs;

        public OverviewDialog()
            : base(nameof(OverviewDialog))
        {
            OrderDialog orderDialog = new OrderDialog();
            NextOrder nextorderDialog = new NextOrder();
            PlanDialog planDialog = new PlanDialog();
            CreditDialog creditDialog = new CreditDialog();
            OrderForOtherDayDialog orderForAnotherDay = new OrderForOtherDayDialog();
            DeleteOrderDialog DeleteOrderDialog = new DeleteOrderDialog();
            List<ComponentDialog> dialogsList = new List<ComponentDialog>();
            DailyCreditDialog dailyCreditDialog = new DailyCreditDialog();   
            ExcellDialog excellDialog = new ExcellDialog();
            //dialogsList.Add(orderDialog);
            dialogsList.Add(nextorderDialog);
            dialogsList.Add(orderForAnotherDay);
            //dialogsList.Add(planDialog);
            dialogsList.Add(DeleteOrderDialog);
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
            this.AddDialog(new ExcellDialog());
            this.AddDialog(new TextPrompt(nameof(TextPrompt)));
            this.AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            this.AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Wähle eines der unteren Ereignisse aus oder schreibe Hilfe um zu erfahren was ich sonst noch alles kann."), cancellationToken);

            // Cards are sent as Attachments in the Bot Framework.
            // So we need to create a list of attachments for the reply activity.
            var attachments = new List<Attachment>();
            for (int i = 0; i < weekDaysEN.Length; i++)
            {
                if (weekDaysEN[i] == DateTime.Now.DayOfWeek.ToString().ToLower() && DateTime.Now.Hour < 12)
                {
                    indexer = i;
                }
                else if (weekDaysEN[i] == DateTime.Now.DayOfWeek.ToString().ToLower() && weekDaysEN[i] != "friday")
                {
                    indexer = i + 1;
                }
            }
            try
            {
                string food = BotMethods.GetDocument("eatingplan", "ButlerOverview.json");
                plan = JsonConvert.DeserializeObject<Plan>(food);
                dayId = plan.Planday.FindIndex(x => x.Name == DateTime.Now.DayOfWeek.ToString().ToLower());
                valid = true;
            }
            catch
            {
                valid = false;
            }
            List<string> choise = new List<string>();
            var day = plan.Planday[dayId];
            if (day.Restaurant1 != null)
            {
                choise.Add(day.Restaurant1);
            }
            if (day.Restaurant2 != null)
            {
                choise.Add(day.Restaurant2);
            }
            string msg = "";
            bool temp = false;
            foreach (var item in choise)
            {

                if (temp == false)
                {
                    msg = $"Heute wird bei dem Restaurant: {item} Essen bestellt ";
                    temp = true;
                }
                else
                {
                    msg += $"und bei dem Restaurant: {item}";
                }

            }
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg));
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

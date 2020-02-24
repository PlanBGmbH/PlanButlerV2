// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;

using BotLibraryV2;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PlanB.Butler.Bot;

namespace PlanB.Butler.Bot
{
    /// <summary>
    /// OverviewDialog.
    /// </summary>
    /// <seealso cref="Microsoft.Bot.Builder.Dialogs.ComponentDialog" />
    public class OverviewDialog : ComponentDialog
    {
        private static Plan plan = new Plan();
        private static int dayId;
        private static string[] weekDaysEN = { "monday", "tuesday", "wednesday", "thursday", "friday" };
        private static int indexer = 0;
        private static bool valid;

        private static ResourceManager rm = new ResourceManager("PlanB.Butler.Bot.Dictionary.main", Assembly.GetExecutingAssembly());
        private static string help = rm.GetString("help");
        private static string orderFood = rm.GetString("orderFood"); 
        private static string deleteOrder = rm.GetString("deleteOrder");
        private static string showMonthBurden = rm.GetString("showMonthBurden");
        private static string orderOtherDay = rm.GetString("orderOtherDay");
        private static string daysOrder = rm.GetString("daysOrder");
        private static string ordered = rm.GetString("ordered");
        private static string todaysRestaurant = rm.GetString("todaysRestaurant");
        private static string andAtRestauarant = rm.GetString("andAtRestauarant");
        private static string whatNow = rm.GetString("whatNow"); 
        private static string errorOtherDay2 = rm.GetString("errorOtherDay2");

        // In this Array you can Easy modify your choice List.
        private static string[] choices = { orderFood, orderOtherDay, deleteOrder, showMonthBurden , daysOrder };
        private static ComponentDialog[] dialogs;

        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="OverviewDialog"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public OverviewDialog(IOptions<BotConfig> config, IBotTelemetryClient telemetryClient)
            : base(nameof(OverviewDialog))
        {
            this.botConfig = config;

            OrderDialog orderDialog = new OrderDialog(config, telemetryClient );
            NextOrder nextorderDialog = new NextOrder(config, telemetryClient);
            PlanDialog planDialog = new PlanDialog(config, telemetryClient);
            CreditDialog creditDialog = new CreditDialog(config, telemetryClient);
            OrderForOtherDayDialog orderForAnotherDay = new OrderForOtherDayDialog(config, telemetryClient);
            DeleteOrderDialog deleteOrderDialog = new DeleteOrderDialog(config, telemetryClient);
            List<ComponentDialog> dialogsList = new List<ComponentDialog>();
            DailyCreditDialog dailyCreditDialog = new DailyCreditDialog(config, telemetryClient);
            ExcellDialog excellDialog = new ExcellDialog(config, telemetryClient);

            // dialogsList.Add(orderDialog);
            dialogsList.Add(nextorderDialog);
            dialogsList.Add(orderForAnotherDay);

            // dialogsList.Add(planDialog);
            dialogsList.Add(deleteOrderDialog);
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
            this.AddDialog(new OrderDialog(config,telemetryClient));
            this.AddDialog(new CreditDialog(config, telemetryClient));
            this.AddDialog(new PlanDialog(config, telemetryClient));
            this.AddDialog(new OrderForOtherDayDialog(config, telemetryClient));
            this.AddDialog(new DeleteOrderDialog(config, telemetryClient));
            this.AddDialog(new NextOrder(config, telemetryClient));
            this.AddDialog(new DailyCreditDialog(config, telemetryClient));
            this.AddDialog(new ExcellDialog(config, telemetryClient));
            this.AddDialog(new TextPrompt(nameof(TextPrompt)));
            this.AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            this.AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            this.InitialDialogId = nameof(WaterfallDialog);

    }

    /// <summary>
    /// Initials the step asynchronous.
    /// </summary>
    /// <param name="stepContext">The step context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(help), cancellationToken);
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
                string food = BotMethods.GetDocument("eatingplan", "ButlerOverview.json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey);
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

            string msg = string.Empty;
            bool temp = false;
            foreach (var item in choise)
            {
                if (temp == false)
                {
                    msg = $" {todaysRestaurant} {item} {ordered} ";
                    temp = true;
                }
                else
                {
                    msg += $"{andAtRestauarant} {item}";
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
                Prompt = MessageFactory.Text(whatNow),
                Choices = ChoiceFactory.ToChoices(choiceList),
                Style = ListStyle.HeroCard,
            }, cancellationToken);
        }

        /// <summary>
        /// Forwards the step asynchronous.
        /// </summary>
        /// <param name="stepContext">The step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
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
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(errorOtherDay2), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        /// <summary>
        /// Finals the step asynchronous.
        /// </summary>
        /// <param name="stepContext">The step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}

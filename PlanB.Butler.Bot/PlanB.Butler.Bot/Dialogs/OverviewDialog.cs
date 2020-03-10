// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using PlanB.Butler.Bot.Services;

namespace PlanB.Butler.Bot.Dialogs
{
    /// <summary>
    /// OverviewDialog.
    /// </summary>
    /// <seealso cref="Microsoft.Bot.Builder.Dialogs.ComponentDialog" />
    public class OverviewDialog : ComponentDialog
    {
        /// <summary>
        /// The client factory.
        /// </summary>
        private readonly IHttpClientFactory clientFactory;

        private static Plan plan = new Plan();
        private static int dayId;
        [Obsolete("Why is this still in use?")]
        private static string[] weekDaysEN = { "monday", "tuesday", "wednesday", "thursday", "friday" };
        private static int indexer = 0;
        private static bool valid;

        /// <summary>
        /// OverviewDialogHelp.
        /// OverviewDialogOrderFood
        /// OverviewDialogDeleteOrder
        /// OverviewDialogShowDepts
        /// OverviewDDialogOtherDay
        /// OverviewDDialogOrder
        /// OverviewDialogWhatNow
        /// OverviewDialogError.
        /// </summary>
        private static ComponentDialog[] dialogs;

        private static string overviewDialogHelp = string.Empty;
        private static string overviewDialogOrderFood = string.Empty;
        private static string overviewDialogDeleteOrder = string.Empty;
        private static string overviewDialogShowDepts = string.Empty;
        private static string overviewDialogOtherDay = string.Empty;
        private static string overviewDialogDaysOrder = string.Empty;
        private static string overviewDialogWhatNow = string.Empty;
        private static string overviewDialogError = string.Empty;
        private static string otherDayDialogOrder = string.Empty;

        // In this Array you can Easy modify your choice List.
        private static string[] choices;

        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="OverviewDialog" /> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="telemetryClient">The telemetry client.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        public OverviewDialog(IOptions<BotConfig> config, IBotTelemetryClient telemetryClient, IHttpClientFactory httpClientFactory)
            : base(nameof(OverviewDialog))
        {
            this.botConfig = config;
            this.clientFactory = httpClientFactory;

            ResourceManager rm = new ResourceManager("PlanB.Butler.Bot.Dictionary.Dialogs", Assembly.GetExecutingAssembly());

            overviewDialogHelp = rm.GetString("OverviewDialog_Help");
            overviewDialogOrderFood = rm.GetString("OverviewDialog_OrderFood");
            overviewDialogDeleteOrder = rm.GetString("OverviewDialog_DeleteOrder");
            overviewDialogShowDepts = rm.GetString("OverviewDialog_ShowDepts");
            overviewDialogOtherDay = rm.GetString("OverviewDialog_OtherDay");
            overviewDialogDaysOrder = rm.GetString("OverviewDialog_DaysOrder");
            overviewDialogWhatNow = rm.GetString("OverviewDialog_WhatNow");
            overviewDialogError = rm.GetString("OtherDayDialog_Error2");
            otherDayDialogOrder = rm.GetString("OtherDayDialog_Order");

            choices = new string[] { overviewDialogOrderFood, overviewDialogOtherDay, overviewDialogDeleteOrder, overviewDialogShowDepts, overviewDialogDaysOrder };
            OrderDialog orderDialog = new OrderDialog(this.botConfig, telemetryClient, this.clientFactory);
            NextOrder nextorderDialog = new NextOrder(this.botConfig, telemetryClient, this.clientFactory);
            PlanDialog planDialog = new PlanDialog(this.botConfig, telemetryClient);
            CreditDialog creditDialog = new CreditDialog(this.botConfig, telemetryClient);
            OrderForOtherDayDialog orderForAnotherDay = new OrderForOtherDayDialog(this.botConfig, telemetryClient, this.clientFactory);
            DeleteOrderDialog deleteOrderDialog = new DeleteOrderDialog(this.botConfig, telemetryClient, this.clientFactory);
            List<ComponentDialog> dialogsList = new List<ComponentDialog>();
            DailyCreditDialog dailyCreditDialog = new DailyCreditDialog(this.botConfig, telemetryClient);
            ExcellDialog excellDialog = new ExcellDialog(this.botConfig, telemetryClient);

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
            this.AddDialog(new OrderDialog(this.botConfig, telemetryClient, this.clientFactory));
            this.AddDialog(new CreditDialog(this.botConfig, telemetryClient));
            this.AddDialog(new PlanDialog(this.botConfig, telemetryClient));
            this.AddDialog(new OrderForOtherDayDialog(this.botConfig, telemetryClient, this.clientFactory));
            this.AddDialog(new DeleteOrderDialog(this.botConfig, telemetryClient, this.clientFactory));
            this.AddDialog(new NextOrder(this.botConfig, telemetryClient, this.clientFactory));
            this.AddDialog(new DailyCreditDialog(this.botConfig, telemetryClient));
            this.AddDialog(new ExcellDialog(this.botConfig, telemetryClient));
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
        /// <returns>DialogTurnResult.</returns>
        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(overviewDialogHelp), cancellationToken);
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

            //IMealService mealService = new MealService(this.clientFactory.CreateClient(), this.botConfig.Value);
            //var meals = await mealService.GetMeals(string.Empty, string.Empty);
            //var mealEnumerator = meals.GetEnumerator();
            //PlanDay day = new PlanDay();
            //while (mealEnumerator.MoveNext())
            //{
            //    if (string.IsNullOrEmpty(day.Restaurant1))
            //    {
            //        day.Restaurant1 = mealEnumerator.Current.Restaurant;
            //    }

            //    if (string.IsNullOrEmpty(day.Restaurant2) && day.Restaurant1 != mealEnumerator.Current.Restaurant)
            //    {
            //        day.Restaurant2 = mealEnumerator.Current.Restaurant;
            //    }
            //}

            List<string> restaurants = new List<string>();
            //var day = plan.Planday[dayId];
            //if (day.Restaurant1 != null)
            //{
            //    restaurants.Add(day.Restaurant1);
            //}

            //if (day.Restaurant2 != null)
            //{
            //    restaurants.Add(day.Restaurant2);
            //}

            string msg = string.Empty;
            bool temp = false;

            // TODO: What is the idea of this?
            foreach (var restaurant in restaurants)
            {
                if (temp == false)
                {
                    var otherDayDialog_Order1 = MessageFactory.Text(string.Format(otherDayDialogOrder, restaurant));
                    msg = $" {otherDayDialog_Order1}";
                    temp = true;
                }
                else
                {
                    // TODO: ??
                }
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(""));

            // Reply to the activity we received with an activity.
            var reply = MessageFactory.Attachment(attachments);
            return await stepContext.PromptAsync(
            nameof(ChoicePrompt),
            new PromptOptions
            {
                Prompt = MessageFactory.Text(overviewDialogWhatNow),
                Choices = ChoiceFactory.ToChoices(choices.ToList()),
                Style = ListStyle.HeroCard,
            }, cancellationToken);
        }

        /// <summary>
        /// Forwards the step asynchronous.
        /// </summary>
        /// <param name="stepContext">The step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>DialogTurnResult.</returns>
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
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(overviewDialogError), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        /// <summary>
        /// Finals the step asynchronous.
        /// </summary>
        /// <param name="stepContext">The step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>DialogTurnResult.</returns>
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}

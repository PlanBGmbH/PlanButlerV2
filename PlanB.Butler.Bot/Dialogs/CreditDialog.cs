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
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace PlanB.Butler.Bot.Dialogs
{
    public class CreditDialog : ComponentDialog
    {
        /// <summary>
        /// OtherDayDialogNamePrompt.
        /// CreditDialogNoOrder
        /// CreditDialogNoBill
        /// CreditDialogNoBillLastMonth
        /// CreditDialogMonthDepts
        /// CreditDialogYes
        /// CreditDialogNo.
        /// </summary>

        private static string otherDayDialogNamePrompt = string.Empty;
        private static string creditDialogNoOrder = string.Empty;
        private static string creditDialogNoBill = string.Empty;
        private static string creditDialogNoBillLastMonth = string.Empty;
        private static string creditDialogLastMonthDepts = string.Empty;
        private static string creditDialogMonthDepts = string.Empty;
        private static string creditDialogYes = string.Empty;
        private static string creditDialogNo = string.Empty;



        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        public CreditDialog(IOptions<BotConfig> config, IBotTelemetryClient telemetryClient)
            : base(nameof(CreditDialog))
        {
            ResourceManager rm = new ResourceManager("PlanB.Butler.Bot.Dictionary.Dialogs", Assembly.GetExecutingAssembly());

            otherDayDialogNamePrompt = rm.GetString("OtherDayDialog_NamePrompt");
            creditDialogNoOrder = rm.GetString("CreditDialog_NoOrder");
            creditDialogNoBill = rm.GetString("CreditDialog_NoBill");
            creditDialogNoBillLastMonth = rm.GetString("CreditDialog_NoBillLastMonth");
            creditDialogLastMonthDepts = rm.GetString("CreditDialog_LastMonthDepts");
            creditDialogMonthDepts = rm.GetString("CreditDialog_MonthDepts");
            creditDialogYes = rm.GetString("yes");
            creditDialogNo = rm.GetString("no");


            this.botConfig = config;
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                this.NameStepAsync,
                this.GetMoneyStepAsync1,
                this.GetMoneyStepAsync2,
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
            if (stepContext.Context.Activity.From.Name != "User")
            {
                stepContext.Values["name"] = stepContext.Context.Activity.From.Name;
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text(otherDayDialogNamePrompt) }, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> GetMoneyStepAsync1(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.Activity.From.Name == "User")
            {
                stepContext.Values["name"] = (string)stepContext.Result;
            }



            try
            {
                MoneyLog money = JsonConvert.DeserializeObject<MoneyLog>(BotMethods.GetDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey));

                var userId = money.User.FindIndex(x => x.Name == (string)stepContext.Values["name"]);

                var creditDialogMonthDepts1 = MessageFactory.Text(string.Format(creditDialogMonthDepts, money.User[userId].Owe)); //monatliche Belastung 

                if (userId != -1)
                {
                    await stepContext.Context.SendActivityAsync(creditDialogMonthDepts1, cancellationToken);
                    await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    return await stepContext.BeginDialogAsync(nameof(OverviewDialog));
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text(creditDialogNoOrder),
                        Choices = ChoiceFactory.ToChoices(new List<string> { creditDialogYes, creditDialogNo }),
                        Style = ListStyle.HeroCard,
                    });
                }
            }
            catch
            {
                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text(creditDialogNoBill),
                    Choices = ChoiceFactory.ToChoices(new List<string> { creditDialogYes, creditDialogNo }),
                    Style = ListStyle.HeroCard,
                });
            }
        }
            private async Task<DialogTurnResult> GetMoneyStepAsync2(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                stepContext.Values["Choise"] = ((FoundChoice)stepContext.Result).Value;

                if (stepContext.Values["Choise"].ToString().ToLower() == "ja")
                {
                    try
                    {
                        var lastmonth = DateTime.Now.Month - 1;
                        MoneyLog money = JsonConvert.DeserializeObject<MoneyLog>(BotMethods.GetDocument("moneylog", "money_" + lastmonth.ToString() + "_" + DateTime.Now.Year + ".json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey));

                        var userId = money.User.FindIndex(x => x.Name == (string)stepContext.Values["name"]);

                        var creditDialog_LastMonthDepts1 = MessageFactory.Text(string.Format(creditDialogLastMonthDepts, money.User[userId].Owe));
                        if (userId != -1)
                        {
                            await stepContext.Context.SendActivityAsync(creditDialog_LastMonthDepts1, cancellationToken);
                        }
                        else
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text(creditDialogNoBillLastMonth), cancellationToken);
                        }
                    }
                    catch
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(creditDialogNoBill), cancellationToken);
                    }
                }

                await stepContext.EndDialogAsync();
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog));
            }
        }
    }

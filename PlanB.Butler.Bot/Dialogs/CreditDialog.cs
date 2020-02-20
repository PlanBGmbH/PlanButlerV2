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

namespace PlanB.Butler.Bot
{
    public class CreditDialog : ComponentDialog
    {
        private static ResourceManager rm = new ResourceManager("PlanB.Butler.Bot.Dictionary.main", Assembly.GetExecutingAssembly());
        private static string name = rm.GetString("name");
        private static string monthBurden = rm.GetString("monthBurden");
        private static string euro = rm.GetString("euro");
        private static string noOrderMonth = rm.GetString("noOrderMonth");
        private static string yes = rm.GetString("yes");
        private static string no = rm.GetString("no");
        private static string noBillMonth = rm.GetString("noBillMonth");
        private static string noBillLastMonth = rm.GetString("noBillLastMonth");
        private static string lastMonthBurden = rm.GetString("lastMonthBruden");


        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        public CreditDialog(IOptions<BotConfig> config, IBotTelemetryClient telemetryClient)
            : base(nameof(CreditDialog))
        {
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
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text(name) }, cancellationToken);
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
                if (userId != -1)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{monthBurden} {money.User[userId].Owe} {euro}"), cancellationToken);
                    await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    return await stepContext.BeginDialogAsync(nameof(OverviewDialog));
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text(noOrderMonth),
                        Choices = ChoiceFactory.ToChoices(new List<string> { yes, no }),
                        Style = ListStyle.HeroCard,
                    });
                }
            }
            catch
            {
                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text(noBillMonth),
                    Choices = ChoiceFactory.ToChoices(new List<string> { yes, no }),
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
                    if (userId != -1)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{lastMonthBurden} {money.User[userId].Owe} { euro}"), cancellationToken);
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(noBillMonth), cancellationToken);
                    }
                }
                catch
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(noBillLastMonth), cancellationToken);
                }
            }

            await stepContext.EndDialogAsync();
            return await stepContext.BeginDialogAsync(nameof(OverviewDialog));
        }
    }
}

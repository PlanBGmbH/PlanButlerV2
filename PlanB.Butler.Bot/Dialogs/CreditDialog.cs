// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
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
        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        public CreditDialog(IOptions<BotConfig> config)
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
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text($"Bitte gib deinen Namen ein.") }, cancellationToken);
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
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Diesen Monat beträgt deine Belastung: {money.User[userId].Owe}€"), cancellationToken);
                    await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    return await stepContext.BeginDialogAsync(nameof(OverviewDialog));
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Du hast diesen Monat noch nichts bestellt, soll ich für letzten Monat nachschauen?"),
                        Choices = ChoiceFactory.ToChoices(new List<string> { "Ja", "Nein" }),
                        Style = ListStyle.HeroCard,
                    });
                }
            }
            catch
            {
                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Diesen Monat liegt noch keine Rechnung vor, soll ich für letzten Monat nachschauen?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Ja", "Nein" }),
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
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Letzten Monat betrugt deine Belastung: {money.User[userId].Owe}€"), cancellationToken);
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Ich hab für letzten Monat auch keine Rechnung von dir gefunden. Leider kann ich dir nicht weiterhelfen.\n:("), cancellationToken);
                    }
                }
                catch
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Letzten Monat gab es auch keine Rechnung.\n:("), cancellationToken);
                }
            }

            await stepContext.EndDialogAsync();
            return await stepContext.BeginDialogAsync(nameof(OverviewDialog));
        }
    }
}

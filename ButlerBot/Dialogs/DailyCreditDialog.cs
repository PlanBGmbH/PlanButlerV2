namespace ButlerBot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
     using BotLibraryV2;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Choices;
    using Newtonsoft.Json;

    public class DailyCreditDialog : ComponentDialog
    {
        public DailyCreditDialog()
            : base(nameof(DailyCreditDialog))
        {
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

            var msg = "";
            int dayNumber = DateTime.Now.DayOfYear;
            SalaryDeduction money = JsonConvert.DeserializeObject<SalaryDeduction>(BotMethods.GetDocument("salarydeduction", "orders_" + dayNumber.ToString() + "_" + DateTime.Now.Year + ".json"));
            var userId = money.Order.FindIndex(x => x.Name == (string)stepContext.Values["name"]);
            try
            {

                OrderBlob orderBlob = new OrderBlob();
                int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                var dayId = orderBlob.Day.FindIndex(x => x.Name == DateTime.Now.DayOfWeek.ToString().ToLower());
                var nameID = orderBlob.Day[dayId].Order.FindAll(x => x.Name == (string)stepContext.Values["name"]);
                msg += $"Heute beträgt die Belastung: {Environment.NewLine}";
                if (nameID.Count != 0)
                {

                    string message = $"Du hast heute {nameID.LastOrDefault().Meal} bei {nameID.LastOrDefault().Restaurant} bestellt.";
                    if (nameID.Count > 0)
                    {
                        message = string.Empty;
                        string orders = string.Empty;
                        foreach (var item in nameID)
                        {

                            if (item.CompanyStatus.ToLower().ToString() == "kunde" || item.CompanyStatus.ToLower().ToString() == "privat" || item.CompanyStatus.ToLower().ToString() == "praktikant")
                            {
                                orders = $"Für den Externen {item.CompanyName} wurde das Essen {item.Meal} {item.Quantaty} mal bestellt und kostet {item.Price}€ {Environment.NewLine}";
                            }
                            else
                            {
                                orders = $"Für dich wurde das Essen {item.Meal} {item.Quantaty} mal bestellt und kostet {item.Price}€  {Environment.NewLine}";
                            }

                            msg += $"{orders}";
                        }
                    }
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

            if (userId != -1)
            {
                // Get the Order from the BlobStorage, the current day ID and nameId from the user

                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
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

        private async Task<DialogTurnResult> GetMoneyStepAsync2(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Choise"] = ((FoundChoice)stepContext.Result).Value;

            if (stepContext.Values["Choise"].ToString().ToLower() == "ja")
            {
                try
                {
                    var lastmonth = DateTime.Now.Month - 1;
                    MoneyLog money = JsonConvert.DeserializeObject<MoneyLog>(BotMethods.GetDocument("moneylog", "money_" + lastmonth.ToString() + "_" + DateTime.Now.Year + ".json"));

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

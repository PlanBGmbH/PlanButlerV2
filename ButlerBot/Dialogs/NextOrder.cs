using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BotLibraryV2;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace ButlerBot
{
    /// <summary>
    /// NextOrder.
    /// </summary>
    public class NextOrder : ComponentDialog
    {
        private static Plan plan = new Plan();
        private static int dayId;
        private static bool valid;
        private static string dayName;
        private static string leftQuantity = " ";
        private static string companyStatus = " ";
        private static string companyName = " ";
        private static string[] weekDays = { "Montag", "Dienstag", "Mitwoch", "Donnerstag", "Freitag" };
        private static string[] weekDaysEN = { "monday", "tuesday", "wednesday", "thursday", "friday" };
        private static List<Order> orderList = new List<Order>();
        private static int indexer = 0;
        private static int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
        private const double grand = 3.30;
        private static HttpStatusCode statusOrder;
        private static HttpStatusCode statusSalary;
        private static HttpStatusCode statusMoney;



        /// <summary>
        /// Initializes a new instance of the <see cref="NextOrder"/> class.
        /// NextOrderConstructor.
        /// </summary>
        public NextOrder()
            : base(nameof(NextOrder))
        {
            for (int i = 0; i < weekDays.Length; i++)
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

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                CompanyStepAsync,
                NameStepAsync,
                QuantatyStepAsync,
                RestaurantStepAsync,
                FoodStepAsync,
                MealQuantatyStepAsync,
                SetMealQuantatyStepAsync,
                GetMealQuantatyStepAsync,
                PriceStepAsync,
                SummaryStepAsync,
                SecondFoodStepAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.AddDialog(new TextPrompt(nameof(TextPrompt)));
            this.AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            this.AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> CompanyStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the Plan
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
            stepContext.Values["name"] = stepContext.Context.Activity.From.Name;

            if (companyStatus == "kunde")
            {
                stepContext.Values["companyStatus"] = companyStatus;
                return await stepContext.NextAsync();
            }
            else
            {
                if (DateTime.Now.IsDaylightSavingTime())
                {


                    if (DateTime.Now.Hour + 1 > 12)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Es ist nach 12 Uhr. Bitte bestelle für einen anderen Tag."));
                        return await stepContext.BeginDialogAsync(nameof(OrderForOtherDayDialog));
                    }
                }
                else
                {
                    if (DateTime.Now.Hour + 2 > 12)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Es ist nach 12 Uhr. Bitte bestelle für einen anderen Tag."));
                        return await stepContext.BeginDialogAsync(nameof(OrderForOtherDayDialog));
                    }
                }

                return await stepContext.PromptAsync(
                              nameof(ChoicePrompt),
                              new PromptOptions
                              {
                                  Prompt = MessageFactory.Text("Für wen willst du bestellen?"),
                                  Choices = ChoiceFactory.ToChoices(new List<string> { "Für mich", "Privat", "Praktikant", "Kunde" }),
                                  Style = ListStyle.HeroCard,
                              }, cancellationToken);
            }
        }

        private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                stepContext.Values["companyStatus"] = ((FoundChoice)stepContext.Result).Value;
                companyStatus = (string)stepContext.Values["companyStatus"];
                if (companyStatus.ToLower().ToString() == "kunde")
                {
                    if (companyName == " ")
                    {
                        return await stepContext.PromptAsync(
                                                     nameof(TextPrompt),
                                                     new PromptOptions { Prompt = MessageFactory.Text("Für welche Firma soll bestellt werden?") },
                                                     cancellationToken);
                    }
                    else
                    {
                        return await stepContext.NextAsync(null, cancellationToken);
                    }
                }
                else if (companyStatus.ToLower().ToString() == "praktikant" || companyStatus.ToLower().ToString() == "privat")
                {
                    return await stepContext.PromptAsync(
                              nameof(TextPrompt),
                              new PromptOptions { Prompt = MessageFactory.Text("Für wen ist das Essen?") },
                              cancellationToken);
                }
                else
                    return await stepContext.NextAsync(null, cancellationToken);
            }
            catch (Exception ex)
            {
                if (companyName == " " && companyStatus.ToLower().ToString() == "kunde")
                {
                    return await stepContext.PromptAsync(
                                                 nameof(TextPrompt),
                                                 new PromptOptions { Prompt = MessageFactory.Text("Für welche Firma soll bestellt werden?") },
                                                 cancellationToken);
                }
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private static async Task<DialogTurnResult> QuantatyStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Values["companyStatus"].ToString().ToLower() == "kunde")
            {
                if (companyName == " ")
                {
                    stepContext.Values["companyName"] = (string)stepContext.Result;
                    companyName = (string)stepContext.Values["companyName"];
                }
                else
                {
                    stepContext.Values["companyName"] = companyName;
                }

            }
            else if (stepContext.Values["companyStatus"].ToString().ToLower() == "privat" || stepContext.Values["companyStatus"].ToString().ToLower() == "praktikant")
            {
                stepContext.Values["companyName"] = (string)stepContext.Result;
            }

            if (leftQuantity == " ")
            {
                if (stepContext.Values["companyStatus"].ToString().ToLower() == "kunde")
                {
                    return await stepContext.PromptAsync(
                        nameof(TextPrompt),
                        new PromptOptions
                        {
                            Prompt = MessageFactory.Text("Wie viele Essen möchtest du bestellen?"),
                        }, cancellationToken);
                }
                else
                {
                    return await stepContext.NextAsync(null, cancellationToken);
                }
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private static async Task<DialogTurnResult> RestaurantStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {


            if (stepContext.Values["companyStatus"].ToString().ToLower() == "kunde")
            {
                if (leftQuantity == " ")
                {
                    leftQuantity = (string)stepContext.Result;
                }
            }
            else
            {
                string val = "1";
                stepContext.Values["quantaty"] = val;
            }

            return await stepContext.PromptAsync(
                nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Danke, bei welchem Restaurant möchtest du Bestellen?"),
                    Choices = GetChoice("restaurant", plan),
                    Style = ListStyle.HeroCard,
                }, cancellationToken);
        }



        private static async Task<DialogTurnResult> FoodStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["restaurant"] = ((FoundChoice)stepContext.Result).Value;


            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Du willst bei {stepContext.Values["restaurant"]} bestellen."), cancellationToken);

            if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[indexer].Restaurant1.ToLower())
            {
                return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Welches Essen möchtest du bestellen?"),
                        Choices = GetChoice("food1", plan),
                        Style = ListStyle.HeroCard,
                    }, cancellationToken);
            }
            else if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[indexer].Restaurant2.ToLower())
            {
                return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Welches Essen möchtest du bestellen?"),
                        Choices = GetChoice("food2", plan),
                        Style = ListStyle.HeroCard,
                    }, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Da ist wohl etwas schief gelaufen, bitte fang nochmal von vorne an."), cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }


        private static async Task<DialogTurnResult> MealQuantatyStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["food"] = ((FoundChoice)stepContext.Result).Value;
            string msg = " ";
            if (leftQuantity != " ")
            {
                msg = $"Möchtest du das Essen einmal bestellen?";
                return await stepContext.PromptAsync(
              nameof(ChoicePrompt),
              new PromptOptions
              {
                  Prompt = MessageFactory.Text(msg),
                  Choices = ChoiceFactory.ToChoices(new List<string> { "Ja", "Nein" }),
                  Style = ListStyle.HeroCard,
              }, cancellationToken);
            }
            else
            {
                if (stepContext.Values["companyStatus"].ToString().ToLower() == "kunde")
                {
                    msg = $"Wie oft soll {stepContext.Values["food"]} bestellt werden?";
                    return await stepContext.PromptAsync(
                       nameof(TextPrompt),
                       new PromptOptions
                       {
                           Prompt = MessageFactory.Text(msg),
                       }, cancellationToken);
                }
            }

            return await stepContext.NextAsync(null, cancellationToken);

        }

        private static async Task<DialogTurnResult> SetMealQuantatyStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string msg = " ";
            try
            {
                stepContext.Values["Choice"] = ((FoundChoice)stepContext.Result).Value;
                if (stepContext.Values["Choice"].ToString().ToLower() == "ja")
                {
                    if (stepContext.Values["companyStatus"].ToString().ToLower() == "kunde")
                    {
                        string val = "1";
                        stepContext.Values["quantaty"] = val;
                        leftQuantity = Convert.ToString(Convert.ToInt32(leftQuantity) - 1);
                    }
                }
                else
                {
                    msg = $"Wie oft soll {stepContext.Values["food"]} bestellt werden?";
                    return await stepContext.PromptAsync(
                       nameof(TextPrompt),
                       new PromptOptions
                       {
                           Prompt = MessageFactory.Text(msg),
                       }, cancellationToken);
                }

            }
            catch (Exception)
            {
                string val = "1";
                stepContext.Values["quantaty"] = val;
            }

            return await stepContext.NextAsync(null, cancellationToken);

        }

        private static async Task<DialogTurnResult> GetMealQuantatyStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            if (stepContext.Values["companyStatus"].ToString().ToLower() == "kunde")
            {
                if (stepContext.Values["Choice"].ToString().ToLower() == "nein")
                {
                    stepContext.Values["quantaty"] = (string)stepContext.Result;
                    int quantaty = Convert.ToInt32(stepContext.Values["quantaty"]);
                    leftQuantity = Convert.ToString(Convert.ToInt32(leftQuantity) - quantaty);
                }
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }



        /// <summary>
        /// PriceStepAsync.
        /// </summary>
        /// <param companyName="stepContext"></param>
        /// <param companyName="cancellationToken"></param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        private static async Task<DialogTurnResult> PriceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[indexer].Restaurant1.ToLower())
            {
                int foodId = plan.Planday[indexer].Meal1.FindIndex(x => x.Name == (string)stepContext.Values["food"]);
                stepContext.Values["price"] = plan.Planday[indexer].Meal1[foodId].Price * Convert.ToInt32(stepContext.Values["quantaty"]);
            }
            else if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[dayId].Restaurant2.ToLower())
            {
                int foodId = plan.Planday[indexer].Meal2.FindIndex(x => x.Name == (string)stepContext.Values["food"]);
                stepContext.Values["price"] = plan.Planday[indexer].Meal2[foodId].Price * Convert.ToInt32(stepContext.Values["quantaty"]);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Da ist wohl etwas schief gelaufen, bitte fang nochmal von vorne an."), cancellationToken);
                return await stepContext.EndDialogAsync();
            }

            string msg = string.Empty;
            if (leftQuantity != " ")
            {
                if (leftQuantity == "0")
                {
                    msg = $"Danke für deine Bestellung. Hier ist eine kleine Zusammenfassung:" + Environment.NewLine;
                    foreach (var item in orderList)
                    {
                        msg += $"Du hast für die Firma {item.CompanyName}, {item.Quantaty} mal " +
                       $"{item.Meal} bei {item.Restaurant} betstellt. Es werden {item.Price}€ berechnet." + Environment.NewLine;
                    }
                    if (Convert.ToInt32(stepContext.Values["quantaty"]) >= 1)
                    {
                        msg += $"Du hast für die Firma {stepContext.Values["companyName"]}, {stepContext.Values["quantaty"]} mal " +
                        $"{stepContext.Values["food"]} bei {stepContext.Values["restaurant"]} betstellt. Es werden {stepContext.Values["price"]}€ berechnet.";
                    }
                }
            }
            else
            {
                if (stepContext.Values["companyStatus"].ToString().ToLower() == "kunde")
                {
                    msg = $"Danke für deine Bestellung. Hier ist eine kleine Zusammenfassung: Du hast für die Firma {stepContext.Values["companyName"]}, {stepContext.Values["quantaty"]} mal " +
                        $"{stepContext.Values["food"]} bei {stepContext.Values["restaurant"]} betstellt. Es werden {stepContext.Values["price"]}€ berechnet.";
                }
                else if (stepContext.Values["companyStatus"].ToString().ToLower() == "für mich")
                {
                    try
                    {

                        var orderBlob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                        var dayID = orderBlob.Day.FindIndex(x => x.Name == weekDaysEN[indexer]);
                        if (dayID == -1)
                        {
                            if (Convert.ToDouble(stepContext.Values["price"]) <= grand)
                            {
                                msg = $"Danke {stepContext.Values["name"]} für deine Bestellung. Hier ist eine kleine Zusammenfassung: Du hast bei dem Restaurant {stepContext.Values["restaurant"]}, " +
                                  $"das Essen {stepContext.Values["food"]} bestellt. Dir werden 0€ berechnet.";
                            }
                            else
                            {
                                msg = $"Danke {stepContext.Values["name"]} für deine Bestellung. Hier ist eine kleine Zusammenfassung: Du hast bei dem Restaurant {stepContext.Values["restaurant"]}, " +
                               $"das Essen {stepContext.Values["food"]} bestellt. Dir werden {Math.Round(Convert.ToDouble(stepContext.Values["price"]) - grand, 2)}€ berechnet.";
                            }
                        }
                        else
                        {
                            var order = orderBlob.Day[dayID].Order;
                            var nameAsString = Convert.ToString(stepContext.Values["name"]);
                            var nameId = order.FindIndex(x => x.Name == nameAsString);
                            if (nameId == -1)
                            {
                                if (Convert.ToDouble(stepContext.Values["price"]) <= grand)
                                {
                                    msg = $"Danke {stepContext.Values["name"]} für deine Bestellung. Hier ist eine kleine Zusammenfassung: Du hast bei dem Restaurant {stepContext.Values["restaurant"]}, " +
                                      $"das Essen {stepContext.Values["food"]} bestellt. Dir werden 0€ berechnet.";
                                }
                                else
                                {
                                    msg = $"Danke {stepContext.Values["name"]} für deine Bestellung. Hier ist eine kleine Zusammenfassung: Du hast bei dem Restaurant {stepContext.Values["restaurant"]}, " +
                                   $"das Essen {stepContext.Values["food"]} bestellt. Dir werden {Math.Round(Convert.ToDouble(stepContext.Values["price"]) - grand, 2)}€ berechnet.";
                                }
                            }
                            else
                            {
                                msg = $"Danke {stepContext.Values["name"]} für deine Bestellung. Hier ist eine kleine Zusammenfassung: Du hast bei dem Restaurant {stepContext.Values["restaurant"]}, " +
                                $"das Essen {stepContext.Values["food"]} bestellt. Dir werden {Math.Round(Convert.ToDouble(stepContext.Values["price"]), 2)}€ berechnet.";
                            }
                        }

                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                        return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                        {
                            Prompt = MessageFactory.Text("Passt das so?"),
                            Choices = ChoiceFactory.ToChoices(new List<string> { "Ja", "Nein" }),
                            Style = ListStyle.HeroCard,
                        });
                    }
                    catch (Exception)
                    {
                        if (stepContext.Values["companyStatus"].ToString().ToLower() == "für mich")
                        {
                            msg = $"Danke {stepContext.Values["name"]} für deine Bestellung. Hier ist eine kleine Zusammenfassung: Du hast bei dem Restaurant {stepContext.Values["restaurant"]}, " +
                        $"das Essen {stepContext.Values["food"]} bestellt. Es werden {Math.Round(Convert.ToDouble(stepContext.Values["price"]) - grand, 2)}€ berechnet.";
                        }
                        else
                        {
                            msg = $"Danke {stepContext.Values["companyName"]} für deine Bestellung. Hier ist eine kleine Zusammenfassung: Du hast bei dem Restaurant {stepContext.Values["restaurant"]}, " +
                            $"das Essen {stepContext.Values["food"]} bestellt. Es werden {stepContext.Values["price"] }€ berechnet.";
                        }

                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                        return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                        {
                            Prompt = MessageFactory.Text("Passt das so?"),
                            Choices = ChoiceFactory.ToChoices(new List<string> { "Ja", "Nein" }),
                            Style = ListStyle.HeroCard,
                        });
                    }
                }
                else
                {
                    if (stepContext.Values["companyStatus"].ToString().ToLower() == "privat")
                    {
                        msg = $"Danke, du hast für {stepContext.Values["companyName"]} bei dem Restaurant {stepContext.Values["restaurant"]}, " +
                         $"das Essen {stepContext.Values["food"]} bestellt. Es werden dir {stepContext.Values["price"] }€ berechnet.";
                    }
                    else
                    {
                        msg = $"Danke, du hast für {stepContext.Values["companyName"]} bei dem Restaurant {stepContext.Values["restaurant"]}, " +
                   $"das Essen {stepContext.Values["food"]} bestellt. Es wird PlanB {stepContext.Values["price"] }€ berechnet.";
                    }
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);


                    return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Passt das so?"),
                        Choices = ChoiceFactory.ToChoices(new List<string> { "Ja", "Nein" }),
                        Style = ListStyle.HeroCard,
                    });

                }
            }

            if (!string.IsNullOrEmpty(msg))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
            }

            if (Convert.ToInt32(leftQuantity) > 0 && stepContext.Values["companyStatus"].ToString().ToLower() == "kunde")
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Passt das so?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Ja", "Nein" }),
                    Style = ListStyle.HeroCard,
                });
            }
        }

        private static async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                stepContext.Values["Choice"] = ((FoundChoice)stepContext.Result).Value;
                if (stepContext.Values["Choice"].ToString().ToLower() == "ja")
                {
                    var order = new Order();
                    if (stepContext.Values["companyStatus"].ToString().ToLower() == "für mich")
                    {
                        try
                        {
                            order.Date = DateTime.Now;
                            order.CompanyStatus = (string)stepContext.Values["companyStatus"];
                            order.Name = (string)stepContext.Values["name"];
                            order.CompanyName = "intern";
                            order.Restaurant = (string)stepContext.Values["restaurant"];
                            order.Quantaty = Convert.ToInt32(stepContext.Values["quantaty"]);
                            order.Meal = (string)stepContext.Values["food"];
                            var orderblob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                            var dayID = orderblob.Day.FindIndex(x => x.Name == weekDaysEN[indexer]);
                            if (dayID == -1)
                            {
                                if (order.Price <= grand)
                                {
                                    order.Price = 0;
                                }
                                else
                                {
                                    order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]) - grand, 2);
                                }
                            }
                            else
                            {
                                var orderDay = orderblob.Day[dayID].Order;
                                var nameAsString = Convert.ToString(stepContext.Values["companyName"]);
                                var nameId = orderDay.FindIndex(x => x.CompanyName == nameAsString);
                                if (nameId == -1)
                                {
                                    if (order.Price <= grand)
                                    {
                                        order.Price = 0;
                                    }
                                    else
                                    {
                                        order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]) - grand, 2);
                                    }
                                }
                                else
                                {
                                    order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]), 2);
                                }
                            }
                            var bufferorder = order;
                            HttpStatusCode statusOrder = BotMethods.UploadOrder(order);
                            HttpStatusCode statusSalary = BotMethods.UploadOrderforSalaryDeduction(bufferorder);
                            HttpStatusCode statusMoney = BotMethods.UploadMoney(bufferorder);
                            if (statusMoney == HttpStatusCode.OK || (statusMoney == HttpStatusCode.Created && statusOrder == HttpStatusCode.OK) || (statusOrder == HttpStatusCode.Created && statusSalary == HttpStatusCode.OK) || statusSalary == HttpStatusCode.Created)
                            {
                                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Die Bestellung wurde gespeichert."), cancellationToken);
                            }
                            else
                            {
                                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bei deiner Bestellung ist etwas schief gegangen. Bitte bestellen sie noch einmal"), cancellationToken);
                                BotMethods.DeleteOrderforSalaryDeduction(bufferorder);
                                BotMethods.DeleteMoney(bufferorder);
                                BotMethods.DeleteOrder(bufferorder);
                                await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
                            }
                        }
                        catch (Exception)
                        {
                            order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]) - grand, 2);
                        }
                    }
                    else if (stepContext.Values["companyStatus"].ToString().ToLower() == "private" || stepContext.Values["companyStatus"].ToString().ToLower() == "praktikant")
                    {
                        order.Date = DateTime.Now;
                        order.CompanyStatus = (string)stepContext.Values["companyStatus"];
                        order.CompanyName = (string)stepContext.Values["companyName"];
                        order.Name = (string)stepContext.Values["name"];
                        order.Restaurant = (string)stepContext.Values["restaurant"];
                        order.Quantaty = Convert.ToInt32(stepContext.Values["quantaty"]);
                        order.Meal = (string)stepContext.Values["food"];
                        order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]), 2);
                        order.Grand = 0;
                        var bufferorder = order;
                        HttpStatusCode statusOrder = BotMethods.UploadOrder(order);
                        HttpStatusCode statusSalary = BotMethods.UploadOrderforSalaryDeduction(bufferorder);
                        HttpStatusCode statusMoney = BotMethods.UploadMoney(bufferorder);
                        if (statusMoney == HttpStatusCode.OK || (statusMoney == HttpStatusCode.Created && statusOrder == HttpStatusCode.OK) || (statusOrder == HttpStatusCode.Created && statusSalary == HttpStatusCode.OK) || statusSalary == HttpStatusCode.Created)
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Die Bestellung wurde gespeichert."), cancellationToken);
                        }
                        else
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bei deiner Bestellung ist etwas schief gegangen. Bitte bestellen sie noch einmal"), cancellationToken);
                            BotMethods.DeleteOrderforSalaryDeduction(bufferorder);
                            BotMethods.DeleteMoney(bufferorder);
                            BotMethods.DeleteOrder(bufferorder);
                            await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                            return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
                        }
                    }

                    if (stepContext.Values["companyStatus"].ToString().ToLower() == "kunde")
                    {
                        order.Date = DateTime.Now;
                        order.CompanyStatus = (string)stepContext.Values["companyStatus"];
                        order.CompanyName = (string)stepContext.Values["companyName"];
                        order.Name = (string)stepContext.Values["name"];
                        order.Restaurant = (string)stepContext.Values["restaurant"];
                        order.Quantaty = Convert.ToInt32(stepContext.Values["quantaty"]);
                        order.Meal = (string)stepContext.Values["food"];
                        order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]), 2);
                        order.Grand = 0;
                        orderList.Add(order);
                        foreach (var item in orderList)
                        {
                            Order tempOrder = new Order();
                            tempOrder.Date = item.Date;
                            tempOrder.CompanyStatus = item.CompanyStatus;
                            tempOrder.CompanyName = item.CompanyName;
                            tempOrder.Name = item.Name;
                            tempOrder.Restaurant = item.Restaurant;
                            tempOrder.Quantaty = item.Quantaty;
                            tempOrder.Meal = item.Meal;
                            tempOrder.Price = item.Price;
                            tempOrder.Grand = 0;
                            var bufferorder = tempOrder;
                            statusOrder = BotMethods.UploadOrder(tempOrder);
                            statusSalary = BotMethods.UploadOrderforSalaryDeduction(bufferorder);
                            statusMoney = BotMethods.UploadMoney(bufferorder);
                            if (statusMoney == HttpStatusCode.OK || (statusMoney == HttpStatusCode.Created && statusOrder == HttpStatusCode.OK) || (statusOrder == HttpStatusCode.Created && statusSalary == HttpStatusCode.OK) || statusSalary == HttpStatusCode.Created)
                            {

                            }
                            else
                            {
                                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bei deiner Bestellung ist etwas schief gegangen. Bitte bestellen sie noch einmal"), cancellationToken);
                                BotMethods.DeleteOrderforSalaryDeduction(bufferorder);
                                BotMethods.DeleteMoney(bufferorder);
                                BotMethods.DeleteOrder(bufferorder);
                                await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
                            }
                        }

                        companyStatus = " ";
                        companyName = " ";
                        leftQuantity = " ";
                        orderList.Clear();
                    }

                    if (leftQuantity == " ")
                    {
                        return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                        {
                            Prompt = MessageFactory.Text("Willst du nochmal für jemand Essen bestellen?"),
                            Choices = ChoiceFactory.ToChoices(new List<string> { "Ja", "Nein" }),
                            Style = ListStyle.HeroCard,
                        });
                    }
                    else
                    {
                        companyStatus = "kunde";
                        companyName = (string)stepContext.Values["companyName"];
                        await stepContext.EndDialogAsync(null, cancellationToken);
                        return await stepContext.BeginDialogAsync(nameof(NextOrder), null, cancellationToken);
                    }
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Okay deine Bestellung wird nicht gespeichert."), cancellationToken);
                    companyStatus = " ";
                    companyName = " ";
                    leftQuantity = " ";
                    orderList.Clear();
                    await stepContext.EndDialogAsync(null, cancellationToken);
                    return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
                }
            }
            catch (Exception)
            {
                Order order = new Order();
                order.Date = DateTime.Now;
                order.CompanyStatus = (string)stepContext.Values["companyStatus"];
                order.CompanyName = (string)stepContext.Values["companyName"];
                order.Name = (string)stepContext.Values["name"];
                order.Restaurant = (string)stepContext.Values["restaurant"];
                order.Quantaty = Convert.ToInt32(stepContext.Values["quantaty"]);
                order.Meal = (string)stepContext.Values["food"];
                order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]), 2);
                order.Grand = 0;
                if (Convert.ToInt32(stepContext.Values["quantaty"]) >= 1)
                {
                    orderList.Add(order);
                }

                companyStatus = "kunde";
                companyName = (string)stepContext.Values["companyName"];
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(NextOrder), null, cancellationToken);
            }

        }


        private static async Task<DialogTurnResult> SecondFoodStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["SecondFoodChoise"] = ((FoundChoice)stepContext.Result).Value;

            if (stepContext.Values["SecondFoodChoise"].ToString().ToLower() == "ja")
            {
                return await stepContext.BeginDialogAsync(nameof(NextOrder), null, cancellationToken);
            }
            else
            {
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
            }
        }
        /// <summary>
        /// Gets the chioses corresponding to the identifier you sepcify
        /// </summary>
        /// <param name="identifier">The identifier is used to define what choises you want</param>
        /// <param name="plan">The plan Object</param>
        /// <returns>Returnds the specified choises</returns>
        private static IList<Choice> GetChoice(string identifier, Plan plan)
        {
            List<string> choise = new List<string>();
            var day = plan.Planday[dayId];
            if (identifier == "restaurant")
            {
                if (day.Restaurant1 != null)
                {
                    choise.Add(day.Restaurant1);
                }

                if (day.Restaurant2 != null)
                {
                    choise.Add(day.Restaurant2);
                }
            }
            else if (identifier == "food1")
            {
                foreach (var food in day.Meal1)
                {
                    choise.Add(food.Name);
                }
            }
            else if (identifier == "food2")
            {
                foreach (var food in day.Meal2)
                {
                    choise.Add(food.Name);
                }
            }

            return ChoiceFactory.ToChoices(choise);
        }
    }
}



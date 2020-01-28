namespace ButlerBot
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using ButlerBot.Classes;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Choices;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

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
                string food = GetDocument("eatingplan", "ButlerOverview.json");
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
                              new PromptOptions { Prompt = MessageFactory.Text("Für wen ist das Essen das?") },
                              cancellationToken);
                }
                else
                {
                    stepContext.Values["companyName"] = String.Empty;
                    return await stepContext.NextAsync(null, cancellationToken);
                }
            }
            catch (Exception)
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

                        var orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
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
                                msg = $"Danke {stepContext.Values["name"]} für deine Bestellung. Hier ist eine kleine Zusammenfassung: Du hast bei dem Restaurant {stepContext.Values["restaurant"]}, " +
                                $"das Essen {stepContext.Values["food"]} bestellt. Dir werden {Math.Round(Convert.ToDouble(stepContext.Values["price"]) - grand, 2)}€ berechnet.";
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
                    msg = $"Danke {stepContext.Values["companyName"]} für deine Bestellung. Hier ist eine kleine Zusammenfassung: Du hast bei dem Restaurant {stepContext.Values["restaurant"]}, " +
                         $"das Essen {stepContext.Values["food"]} bestellt. Es werden {stepContext.Values["price"] }€ berechnet.";
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
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
                            var orderblob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
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
                            HttpStatusCode statusOrder = UploadOrder(order);
                            HttpStatusCode statusSalary = UploadOrderforSalaryDeduction(bufferorder);
                            HttpStatusCode statusMoney = UploadMoney(bufferorder);
                            if (statusMoney == HttpStatusCode.OK || (statusMoney == HttpStatusCode.Created && statusOrder == HttpStatusCode.OK) || (statusOrder == HttpStatusCode.Created && statusSalary == HttpStatusCode.OK) || statusSalary == HttpStatusCode.Created)
                            {
                                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Die Bestellung wurde gespeichert."), cancellationToken);
                            }
                            else
                            {
                                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bei deiner Bestellung ist etwas schief gegangen. Bitte bestellen sie noch einmal"), cancellationToken);
                                DeletOrderforSalaryDeduction(bufferorder);
                                DeletMoney(bufferorder);
                                DeletOrder(bufferorder);
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
                        HttpStatusCode statusOrder = UploadOrder(order);
                        HttpStatusCode statusSalary = UploadOrderforSalaryDeduction(bufferorder);
                        HttpStatusCode statusMoney = UploadMoney(bufferorder);
                        if (statusMoney == HttpStatusCode.OK || (statusMoney == HttpStatusCode.Created && statusOrder == HttpStatusCode.OK) || (statusOrder == HttpStatusCode.Created && statusSalary == HttpStatusCode.OK) || statusSalary == HttpStatusCode.Created)
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Die Bestellung wurde gespeichert."), cancellationToken);
                        }
                        else
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bei deiner Bestellung ist etwas schief gegangen. Bitte bestellen sie noch einmal"), cancellationToken);
                            DeletOrderforSalaryDeduction(bufferorder);
                            DeletMoney(bufferorder);
                            DeletOrder(bufferorder);
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
                            statusOrder = UploadOrder(tempOrder);
                            statusSalary = UploadOrderforSalaryDeduction(bufferorder);
                            statusMoney = UploadMoney(bufferorder);
                            if (statusMoney == HttpStatusCode.OK || (statusMoney == HttpStatusCode.Created && statusOrder == HttpStatusCode.OK) || (statusOrder == HttpStatusCode.Created && statusSalary == HttpStatusCode.OK) || statusSalary == HttpStatusCode.Created)
                            {

                            }
                            else
                            {
                                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bei deiner Bestellung ist etwas schief gegangen. Bitte bestellen sie noch einmal"), cancellationToken);
                                DeletOrderforSalaryDeduction(bufferorder);
                                DeletMoney(bufferorder);
                                DeletOrder(bufferorder);
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
        /// 
        /// </summary>
        /// <param companyName="order"></param>
        private static HttpStatusCode UploadMoney(Order order)
        {
            try
            {
                MoneyLog money = JsonConvert.DeserializeObject<MoneyLog>(GetDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json"));
                var _money = money;
                if (order.CompanyStatus.ToString().ToLower() == "für mich" || order.CompanyStatus.ToString().ToLower() == "privat")
                {
                    var userId = _money.User.FindIndex(x => x.Name == order.Name);

                    // enters if the current user is not in the list. 
                    if (userId == -1)
                    {
                        User user = new User() { Name = order.Name, Owe = order.Price };
                        _money.User.Add(user);

                        HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money));
                        return status;
                    }
                    else // enters if everything is normal
                    {
                        var newOwe = money.User[userId].Owe;
                        newOwe += order.Price;
                        _money.User[userId].Owe = newOwe;

                        HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money));
                        return status;
                    }
                }
                else
                {
                    var userId = _money.User.FindIndex(x => x.Name == order.CompanyName);

                    // enters if the current user is not in the list. 
                    if (userId == -1)
                    {
                        User user = new User() { Name = order.CompanyName, Owe = order.Price };
                        _money.User.Add(user);

                        HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money));
                        return status;
                    }
                    else // enters if everything is normal
                    {
                        var newOwe = money.User[userId].Owe;
                        newOwe += order.Price;
                        _money.User[userId].Owe = newOwe;

                        HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money));
                        return status;
                    }
                }

            }
            catch // enters if blob dont exist
            {
                List<User> users = new List<User>();
                User user = new User() { Name = order.CompanyName, Owe = order.Price };
                users.Add(user);
                MoneyLog money = new MoneyLog() { Monthnumber = DateTime.Now.Month, Title = "moneylog", User = users };

                HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money));
                return status;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param companyName="order"></param>
        private static HttpStatusCode UploadOrder(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                var dayId = orderBlob.Day.FindIndex(x => x.Name == weekDaysEN[indexer]);
                if (dayId == -1) // enters if the current day is not in the list 
                {
                    List<Order> orders = new List<Order>();
                    orders.Add(order);
                    orderBlob.Day.Add(new Day() { Name = weekDaysEN[indexer], Order = orders, Weeknumber = weeknumber });
                    orderBlob.Title = "orders/" + DateTime.Now.Month + "/" + DateTime.Now.Year;

                    HttpStatusCode status = PutDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(orderBlob));
                    return status;
                }
                else // enters if everything is normal
                {
                    orderBlob.Day[dayId].Order.Add(order);
                    HttpStatusCode status = PutDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(orderBlob));
                    return status;
                }
            }
            catch // enters if blob dont exist
            {
                List<Day> day = new List<Day>();
                List<Order> orders = new List<Order>();

                orders.Add(order);
                day.Add(new Day() { Name = DateTime.Now.DayOfWeek.ToString().ToLower(), Order = orders, Weeknumber = weeknumber });

                orderBlob.Title = "orders/" + DateTime.Now.Month + "/" + DateTime.Now.Year;
                orderBlob.Day = day;

                HttpStatusCode status = PutDocument("orders", "orders_" + weeknumber.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(orderBlob));
                return status;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param companyName="order"></param>
        private static HttpStatusCode UploadOrderforSalaryDeduction(Order order)
        {
            SalaryDeduction salaryDeduction = new SalaryDeduction();
            int dayNumber = DateTime.Now.DayOfYear;
            try
            {
                salaryDeduction = JsonConvert.DeserializeObject<SalaryDeduction>(GetDocument("salarydeduction", "orders_" + dayNumber.ToString() + "_" + DateTime.Now.Year + ".json"));
                salaryDeduction.Order.Add(order);
                HttpStatusCode status = PutDocument("salarydeduction", "orders_" + dayNumber.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction));
                return status;
            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                salaryDeduction.Daynumber = dayNumber;
                salaryDeduction.Name = "SalaryDeduction";

                orders.Add(order);
                salaryDeduction.Order = orders;

                HttpStatusCode status = PutDocument("salarydeduction", "orders_" + dayNumber.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction));
                return status;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param companyName="container"></param>
        /// <param companyName="resourceName"></param>
        /// <param companyName="body"></param>
        /// <returns></returns>
        private static HttpStatusCode PutDocument(string container, string resourceName, string body)
        {
            Util.BackendCommunication backendcom = new Util.BackendCommunication();
            HttpStatusCode taskUrl = backendcom.PutDocument(container, resourceName, body, "q.planbutler");
            return taskUrl;
        }

        /// <summary>
        /// Gets a document from our StorageAccount
        /// </summary>
        /// <param companyName="container">Describes the needed container</param>
        /// <param companyName="resourceName">Describes the needed resource</param>
        /// <returns>Returns a JSON you specified with container and resourceName</returns>
        private static string GetDocument(string container, string resourceName)
        {
            Util.BackendCommunication backendcom = new Util.BackendCommunication();
            string taskUrl = backendcom.GetDocument(container, resourceName);
            return taskUrl;
        }

        /// <summary>
        /// delets the entry of your order.
        /// </summary>
        /// <param companyName="order"></param>
        private static void DeletMoney(Order order)
        {
            try
            {
                MoneyLog money = JsonConvert.DeserializeObject<MoneyLog>(GetDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json"));
                var _money = money;
                var userId = _money.User.FindIndex(x => x.Name == order.CompanyName);
                _money.User.RemoveAt(userId);
                PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money));
            }
            catch // enters if blob dont exist
            {
                List<User> users = new List<User>();
                User user = new User() { Name = order.CompanyName, Owe = order.Price };
                users.Add(user);
                MoneyLog money = new MoneyLog() { Monthnumber = DateTime.Now.Month, Title = "moneylog", User = users };

                PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money));
            }
        }

        /// <summary>
        /// delets the entry of your order.
        /// </summary>
        /// <param companyName="order">.</param>
        private static void DeletOrder(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                var valueDay = orderBlob.Day.FindIndex(x => x.Name == dayName);
                var nameId = orderBlob.Day[valueDay].Order.FindIndex(x => x.CompanyName == order.CompanyName);
                orderBlob.Day[valueDay].Order.RemoveAt(nameId);
                PutDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(orderBlob));
            }
            catch // enters if blob dont exist
            {
                List<Day> day = new List<Day>();
                List<Order> orders = new List<Order>();

                orders.Add(order);
                day.Add(new Day() { Name = DateTime.Now.DayOfWeek.ToString().ToLower(), Order = orders, Weeknumber = weeknumber });

                orderBlob.Title = "orders/" + DateTime.Now.Month + "/" + DateTime.Now.Year;
                orderBlob.Day = day;

                PutDocument("orders", "orders_" + weeknumber.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(orderBlob));
            }
        }

        /// <summary>
        /// delets the entry of your order.
        /// </summary>
        /// <param companyName="order"></param>
        private static void DeletOrderforSalaryDeduction(Order order)
        {
            SalaryDeduction salaryDeduction = new SalaryDeduction();
            var dayId = order.Date.Date.DayOfYear;
            salaryDeduction = JsonConvert.DeserializeObject<SalaryDeduction>(GetDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year + ".json"));
            var nameId = salaryDeduction.Order.FindIndex(x => x.CompanyName == order.CompanyName);
            salaryDeduction.Order.RemoveAt(nameId);
            try
            {
                PutDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction));
            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                salaryDeduction.Daynumber = dayId;
                salaryDeduction.Name = "SalaryDeduction";

                orders.Add(order);
                salaryDeduction.Order = orders;

                PutDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction));
            }
        }

        /// <summary>
        /// Gets the chioses corresponding to the identifier you sepcify
        /// </summary>
        /// <param companyName="identifier">The identifier is used to define what choises you want</param>
        /// <param companyName="plan">The plan Object</param>
        /// <returns>Returnds the specified choises</returns>
        private static IList<Choice> GetChoice(string identifier, Plan plan)
        {
            List<string> choise = new List<string>();
            var day = plan.Planday[indexer];
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


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

    public class OrderDialog : ComponentDialog
    {
        private static Plan plan = new Plan();
        private static int dayId;
        private const double grand = 3.30;
        private static bool valid;
        private static string dayName;

        public OrderDialog()
            : base(nameof(OrderDialog))
        {
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
                {
                TimeDayStepAsync,
                NameStepAsync,
                RestaurantStepAsync,
                FoodStepAsync,
                PriceStepAsync,
                SummaryStepAsync,
                SecondFoodStepAsync,
                };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.AddDialog(new TextPrompt(nameof(TextPrompt)));
            this.AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            this.AddDialog(new NextOrder());

            // The initial child Dialog to run.
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> TimeDayStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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
            OrderBlob orderBlob = new OrderBlob();
            try
            {
                int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                var dayId = orderBlob.Day.FindIndex(x => x.Name == DateTime.Now.DayOfWeek.ToString().ToLower());
                if (dayId != -1)
                {
                    var nameID = orderBlob.Day[dayId].Order.FindIndex(x => x.Name == stepContext.Context.Activity.From.Name);
                    if (DateTime.Now.Hour > 12)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Es ist schon nach 12 Uhr"));
                        return await stepContext.BeginDialogAsync(nameof(OrderForOtherDayDialog));
                    }
                    else if (nameID != -1)
                    {
                        var temp = orderBlob.Day[dayId].Order.FindAll(x => x.Name == stepContext.Context.Activity.From.Name);
                        foreach (var item in temp)
                        {
                            if (item.CompanyStatus.ToLower().ToString() == "für mich")
                            {
                                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Du hast heute schon etwas bestellt"));
                                return await stepContext.BeginDialogAsync(nameof(NextOrder));
                            }

                        }

                        return await stepContext.NextAsync();
                    }
                    else
                    {
                        return await stepContext.NextAsync();
                    }
                }
                else
                {
                    return await stepContext.NextAsync();
                }
            }
            catch (Exception)
            {
                return await stepContext.NextAsync();
            }
        }

        private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (valid)
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
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Ein fehler ist aufgetreten. [Keine daten in der Bestellhistorie]"), cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }

        private static async Task<DialogTurnResult> RestaurantStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.Activity.From.Name == "User")
            {
                stepContext.Values["name"] = (string)stepContext.Result;
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Danke {stepContext.Values["name"]}"), cancellationToken);

            return await stepContext.PromptAsync(
                nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Bei welchem Restaurant möchtest du Bestellen?"),
                    Choices = GetChoice("restaurant", plan),
                    Style = ListStyle.HeroCard,
                }, cancellationToken);
        }

        private static async Task<DialogTurnResult> FoodStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["restaurant"] = ((FoundChoice)stepContext.Result).Value;

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Du willst bei {stepContext.Values["restaurant"]} bestellen."), cancellationToken);

            if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[dayId].Restaurant1.ToLower())
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
            else if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[dayId].Restaurant2.ToLower())
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
                await stepContext.EndDialogAsync();
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
            }
        }

        private static async Task<DialogTurnResult> PriceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["food"] = ((FoundChoice)stepContext.Result).Value;

            var rnd = new Random();

            if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[dayId].Restaurant1.ToLower())
            {
                int foodId = plan.Planday[dayId].Meal1.FindIndex(x => x.Name == (string)stepContext.Values["food"]);
                stepContext.Values["price"] = plan.Planday[dayId].Meal1[foodId].Price;
            }
            else if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[dayId].Restaurant2.ToLower())
            {
                int foodId = plan.Planday[dayId].Meal2.FindIndex(x => x.Name == (string)stepContext.Values["food"]);
                stepContext.Values["price"] = plan.Planday[dayId].Meal2[foodId].Price;
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Da ist wohl etwas schief gelaufen, bitte fang nochmal von vorne an."), cancellationToken);
                return await stepContext.EndDialogAsync();
            }
            var msg = string.Empty;
            var temp = "";
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

           

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Passt das so?"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Ja", "Nein" }),
                Style = ListStyle.HeroCard,
            });
        }

        private static async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Choise"] = ((FoundChoice)stepContext.Result).Value;

            if (stepContext.Values["Choise"].ToString().ToLower() == "ja")
            {
                var order = new Order();

                order.Date = DateTime.Now;
                order.CompanyStatus = "intern";
                order.Name = (string)stepContext.Values["name"];
                order.Restaurant = (string)stepContext.Values["restaurant"];
                order.Quantaty = 1;
                order.Meal = (string)stepContext.Values["food"];
                order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]), 2);
                if (order.Price <= grand)
                {
                    order.Price = 0;
                }
                else
                {
                    order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]) - grand, 2);
                }

                order.Grand = grand;
                var bufferorder = order;
                HttpStatusCode statusOrder = UploadOrder(order);
                HttpStatusCode statusSalary = UploadOrderforSalaryDeduction(order);
                HttpStatusCode statusMoney = UploadMoney(order);
                if (statusMoney == HttpStatusCode.OK || (statusMoney == HttpStatusCode.Created && statusOrder == HttpStatusCode.OK) || (statusOrder == HttpStatusCode.Created && statusSalary == HttpStatusCode.OK) || statusSalary == HttpStatusCode.Created)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Deine bestellung wurde gespeichert."), cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bei deiner bestellung ist etwas schief gegangen. Bitte bestellen sie noch einmal"), cancellationToken);
                    DeletOrderforSalaryDeduction(bufferorder);
                    DeleteMoney(bufferorder);
                    DeletOrder(bufferorder);
                    await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
                }

                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Willst du nochmal für jemand Essen bestellen?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Ja", "Nein" }),
                    Style = ListStyle.HeroCard,
                });
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Okay deine Bestellung wird nicht gespeichert."), cancellationToken);
                await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
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
        /// <param name="order"></param>
        private static HttpStatusCode UploadMoney(Order order)
        {
            try
            {
                MoneyLog money = JsonConvert.DeserializeObject<MoneyLog>(GetDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json"));
                var _money = money;
                var userId = _money.User.FindIndex(x => x.Name == order.Name);
                if (userId == -1) // enters if the current user is not in the list 
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
            catch // enters if blob dont exist
            {
                List<User> users = new List<User>();
                User user = new User() { Name = order.Name, Owe = order.Price };
                users.Add(user);
                MoneyLog money = new MoneyLog() { Monthnumber = DateTime.Now.Month, Title = "moneylog", User = users };

                HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money));
                return status;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        private static HttpStatusCode UploadOrder(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                var dayId = orderBlob.Day.FindIndex(x => x.Name == DateTime.Now.DayOfWeek.ToString().ToLower());
                if (dayId == -1) // enters if the current day is not in the list 
                {
                    List<Order> orders = new List<Order>();
                    orders.Add(order);
                    orderBlob.Day.Add(new Day() { Name = DateTime.Now.DayOfWeek.ToString().ToLower(), Order = orders, Weeknumber = weeknumber });
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
        /// <param name="order"></param>
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
        /// <param name="container"></param>
        /// <param name="resourceName"></param>
        /// <param name="body"></param>
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
        /// <param name="container">Describes the needed container</param>
        /// <param name="resourceName">Describes the needed resource</param>
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
        /// <param name="order"></param>
        private static void DeleteMoney(Order order)
        {
            try
            {
                MoneyLog money = JsonConvert.DeserializeObject<MoneyLog>(GetDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json"));
                var _money = money;
                var userId = _money.User.FindIndex(x => x.Name == order.Name);
                _money.User.RemoveAt(userId);
                PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money));
            }
            catch // enters if blob dont exist
            {
                List<User> users = new List<User>();
                User user = new User() { Name = order.Name, Owe = order.Price };
                users.Add(user);
                MoneyLog money = new MoneyLog() { Monthnumber = DateTime.Now.Month, Title = "moneylog", User = users };

                PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money));
            }
        }

        /// <summary>
        /// delets the entry of your order.
        /// </summary>
        /// <param name="order"></param>
        private static void DeletOrder(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                var valueDay = orderBlob.Day.FindIndex(x => x.Name == dayName);
                var nameId = orderBlob.Day[valueDay].Order.FindIndex(x => x.Name == order.Name);
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
        /// <param name="order"></param>
        private static void DeletOrderforSalaryDeduction(Order order)
        {
            SalaryDeduction salaryDeduction = new SalaryDeduction();
            var dayId = order.Date.Date.DayOfYear;
            salaryDeduction = JsonConvert.DeserializeObject<SalaryDeduction>(GetDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year + ".json"));
            var nameId = salaryDeduction.Order.FindIndex(x => x.Name == order.Name);
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
namespace ButlerBot
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
     using BotLibraryV2;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Choices;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

    public class DeleteOrderDialog : ComponentDialog
    {
        static Plan plan = new Plan();
        static Plan orderedfood = new Plan();
        static int valueDay;
        const double grand = 3.30;
        static string dayName;
        static string[] weekDays = { "Montag", "Dienstag", "Mitwoch", "Donnerstag", "Freitag" };
        static string[] weekDaysEN = { "monday", "tuesday", "wednesday", "thursday", "friday" };

        public DeleteOrderDialog()
            : base(nameof(DeleteOrderDialog))
        {
            // Get the Plan
            string food = GetDocument("eatingplan", "ButlerOverview.json");
            plan = JsonConvert.DeserializeObject<Plan>(food);
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
                {
                this.InitialStepAsync,
                this.NameStepAsync,
                RemoveStepAsync,
                DeletOrderStep,
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

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Cards are sent as Attachments in the Bot Framework.
            // So we need to create a list of attachments for the reply activity.
            var attachments = new List<Attachment>();
            List<string> currentWeekDays = new List<string>();
            int indexer = 0;

            // Reply to the activity we received with an activity.
            var reply = MessageFactory.Attachment(attachments);

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

            for (int i = indexer; i < weekDays.Length; i++)
            {
                currentWeekDays.Add(weekDays[i]);
            }

            if (currentWeekDays != null)
            {
                return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text($"Wann möchtest du deine bestellung löschen?"),
                        Choices = ChoiceFactory.ToChoices(currentWeekDays),
                        Style = ListStyle.HeroCard,
                    }, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Tut mir Leid. Ich habe dich nicht verstanden. Bitte benutze Befehle, die ich kenne."), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["mainChoise"] = ((FoundChoice)stepContext.Result).Value;
            var text = stepContext.Values["mainChoise"];
            int indexer = 0;
            for (int i = 0; i < weekDays.Length; i++)
            {
                if (weekDays[i] == text)
                {
                    indexer = i;
                }
                else if (weekDays[i] == text && weekDaysEN[i] != "friday")
                {
                    indexer = i + 1;
                }
            }

            valueDay = plan.Planday.FindIndex(x => x.Name == weekDaysEN[indexer]);
            dayName = weekDaysEN[indexer];
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

        private async Task<DialogTurnResult> RemoveStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                if (stepContext.Context.Activity.From.Name == "User")
                {
                    stepContext.Values["name"] = (string)stepContext.Result;
                }

                var order = new Order();

                order.CompanyStatus = "intern";
                order.Name = (string)stepContext.Values["name"];
                order = GetOrder(order);
                var temp = order.Meal;
                return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text($"Soll {temp} gelöscht werden?"),
                        Choices = ChoiceFactory.ToChoices(new List<string> { "Ja", "Nein" }),
                        Style = ListStyle.HeroCard,
                    }, cancellationToken);
            }
            catch (Exception)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"An diesem Tag gibt es keine Bestellung.\n:("), cancellationToken);
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
            }
        }

        private static async Task<DialogTurnResult> DeletOrderStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["mainChoise"] = ((FoundChoice)stepContext.Result).Value;
            var text = stepContext.Values["mainChoise"];
            if (text.ToString().ToLower() == "ja")
            {
                var order = new Order();
                order.CompanyStatus = "intern";
                order.Name = (string)stepContext.Values["name"];
                var bufferOrder = GetOrder(order);
                order = bufferOrder;
                var temst = 0;
                DeleteOrder(order);

                DeletOrderforSalaryDeduction(bufferOrder);
                DeleteMoney(bufferOrder);

                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Okay deine Bestellung wurde entfernt"), cancellationToken);
                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Willst du nochmal eine Bestellung entfernen?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Ja", "Nein" }),
                    Style = ListStyle.HeroCard,
                });
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Okay deine Bestellung wurde entfernt."), cancellationToken);
                await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
            }
        }

        private static async Task<DialogTurnResult> SecondFoodStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["SecondFoodChoise"] = ((FoundChoice)stepContext.Result).Value;

            if (stepContext.Values["SecondFoodChoise"].ToString().ToLower() == "ja")
            {
                return await stepContext.BeginDialogAsync(nameof(DeleteOrderDialog), null, cancellationToken);
            }
            else
            {
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
            }
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
                var userId = money.User.FindIndex(x => x.Name == order.Name);
                money.User[userId].Owe = Math.Round(money.User[userId].Owe - order.Price, 2);
                PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money));
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
        private static void DeleteOrder(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                var valueDay = orderBlob.Day.FindIndex(x => x.Name == dayName);
                var collection = orderBlob.Day[valueDay].Order.FindAll(x => x.Name == order.Name);
                foreach (var item in collection)
                {
                    if (item.Meal == order.Meal)
                    {
                        orderBlob.Day[valueDay].Order.Remove(item);
                    }
                }
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
        /// gets the order entry.
        /// </summary>
        /// <param name="order"></param>
        private static Order GetOrder(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
            var valueDay = orderBlob.Day.FindIndex(x => x.Name == dayName);
            var bufferOrder = orderBlob.Day[valueDay].Order;
            var nameID = bufferOrder.Find(x => x.Name == order.Name);
            return nameID;
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
            var collection = salaryDeduction.Order.FindAll(x => x.Name == order.Name);
            foreach (var item in collection)
            {
                if (item.Meal == order.Meal)
                {
                    salaryDeduction.Order.Remove(item);
                }
            }



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
        /// Puts the Document in the DB.
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
    }
}
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
    public class OrderForOtherDayDialog : ComponentDialog
    {
        private static Plan plan = new Plan();
        private static int valueDay;
        private const double grand = 3.30;
        private static string dayName;
        private static string[] weekDays = { "Montag", "Dienstag", "Mitwoch", "Donnerstag", "Freitag" };
        private static string[] weekDaysEN = { "monday", "tuesday", "wednesday", "thursday", "friday" };
        private static List<string> meal1List = new List<string>();
        private static List<string> meal1ListwithMoney = new List<string>();
        private static List<string> meal2List = new List<string>();
        private static List<string> meal2ListWithMoney = new List<string>();
        private static int indexer = 0;
        private static string userName = string.Empty;

        public OrderForOtherDayDialog()
            : base(nameof(OrderForOtherDayDialog))
        {
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
                {
                this.InitialStepAsync,
                this.NameStepAsync,
                RestaurantStepAsync,
                FoodStepAsync,
                PriceStepAsync,
                SummaryStepAsync,
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
            // Get the Plan
            string food = BotMethods.GetDocument("eatingplan", "ButlerOverview.json");
            plan = JsonConvert.DeserializeObject<Plan>(food);
            // Cards are sent as Attachments in the Bot Framework.
            // So we need to create a list of attachments for the reply activity.
            var attachments = new List<Attachment>();
            List<string> currentWeekDays = new List<string>();

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
                        Prompt = MessageFactory.Text($"Für wann möchtest du Essen bestellen?"),
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
            string text = Convert.ToString(stepContext.Values["mainChoise"]);

            for (int i = 0; i < weekDaysEN.Length; i++)
            {
                if (text == weekDays[i])
                {
                    indexer = i;
                }
            }

            if (text != null)
            {
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
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Tut mir Leid. Ich habe dich nicht verstanden. Bitte benutze Befehle, die ich kenne."), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
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

            if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[indexer].Restaurant1.ToLower())
            {
                stepContext.Values["rest1"] = "yes";
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
                stepContext.Values["rest1"] = "no";
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

            var obj = ((FoundChoice)stepContext.Result).Value;
            if (stepContext.Values["rest1"].ToString() == "yes")
            {
                for (int i = 0; i < meal1List.Count; i++)
                {
                    if (meal1ListwithMoney[i] == obj)
                    {
                        stepContext.Values["food"] = meal1List[i];
                        i = meal1List.Count;
                    }

                }
            }
            else
            {
                for (int i = 0; i < meal2List.Count; i++)
                {
                    if (meal2ListWithMoney[i] == obj)
                    {
                        stepContext.Values["food"] = meal2List[i];
                        i = meal2List.Count;
                    }
                }
            }


            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            var rnd = new Random();

            if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[indexer].Restaurant1.ToLower())
            {
                int foodId = plan.Planday[indexer].Meal1.FindIndex(x => x.Name == (string)stepContext.Values["food"]);
                stepContext.Values["price"] = plan.Planday[indexer].Meal1[foodId].Price;
            }
            else if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[indexer].Restaurant2.ToLower())
            {
                int foodId = plan.Planday[indexer].Meal2.FindIndex(x => x.Name == (string)stepContext.Values["food"]);
                stepContext.Values["price"] = plan.Planday[indexer].Meal2[foodId].Price;
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Da ist wohl etwas schief gelaufen, bitte fang nochmal von vorne an."), cancellationToken);
                return await stepContext.EndDialogAsync();
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private static async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var order = new Order();

            order.Date = DateTime.Now;
            order.CompanyStatus = "intern";
            order.Name = (string)stepContext.Values["name"];
            order.Restaurant = (string)stepContext.Values["restaurant"];
            order.Quantaty = 1;
            order.Meal = (string)stepContext.Values["food"];
            order.Price = Convert.ToDouble(stepContext.Values["price"]);
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                var orderblob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));

                if (Convert.ToDouble(stepContext.Values["price"]) <= grand)
                {
                    order.Price = 0;
                }
                else
                {
                    order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]) - grand, 2);
                }
                order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]) - grand, 2);
            }
            catch (Exception)
            {
                if (Convert.ToDouble(stepContext.Values["price"]) <= grand)
                {
                    order.Price = 0;
                }
                else
                {
                    order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]) - grand, 2);
                }
            }

            order.Grand = grand;
            var bufferorder = order;
            string day = stepContext.Values["mainChoise"].ToString();
            for (int i = 0; i < weekDays.Length; i++)
            {
                if (day == weekDays[i])
                {
                    indexer = i;
                }
            }

            HttpStatusCode statusOrder = BotMethods.UploadForOtherDay(order, weekDaysEN[indexer]);
            HttpStatusCode statusSalary = BotMethods.UploadOrderforSalaryDeductionForAnotherDay(order, weekDaysEN[indexer]);
            HttpStatusCode statusMoney = BotMethods.UploadMoney(order);
            if (statusMoney == HttpStatusCode.OK || (statusMoney == HttpStatusCode.Created && statusOrder == HttpStatusCode.OK) || (statusOrder == HttpStatusCode.Created && statusSalary == HttpStatusCode.OK) || statusSalary == HttpStatusCode.Created)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Die Bestellung wurde gespeichert."), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bei deiner Bestellung ist etwas schief gegangen. Bitte bestellen sie noch einmal"), cancellationToken);
                BotMethods.DeleteOrderforSalaryDeduction(bufferorder);
                BotMethods.DeleteMoney(bufferorder, weekDaysEN[indexer]);
                BotMethods.DeleteOrder(bufferorder);
                await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
            }

            await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
        }

        /// <summary>
        /// Gets the chioses corresponding to the identifier you sepcify.
        /// </summary>
        /// <param name="identifier">The identifier is used to define what choises you want</param>
        /// <param name="plan">The plan Object</param>
        /// <returns>Returnds the specified choises.</returns>
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
                    meal1List.Add(food.Name);
                }
                foreach (var food in day.Meal1)
                {
                    choise.Add(food.Name + " " + food.Price + "€");
                    meal1ListwithMoney.Add(food.Name + " " + food.Price + "€");
                }
            }
            else if (identifier == "food2")
            {
                foreach (var food in day.Meal2)
                {
                    meal2List.Add(food.Name);
                }
                foreach (var food in day.Meal2)
                {
                    choise.Add(food.Name + " " + food.Price + "€");
                    meal2ListWithMoney.Add(food.Name + " " + food.Price + "€");
                }
            }

            return ChoiceFactory.ToChoices(choise);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        ///

    }
}
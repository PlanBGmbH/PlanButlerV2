using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using BotLibraryV2;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PlanB.Butler.Bot.Services;

namespace PlanB.Butler.Bot.Dialogs
{
    public class OrderDialog : ComponentDialog
    {
        private static Plan plan = new Plan();
        private static int dayId;
        private const double grand = 3.30;
        private static bool valid;
        private static string dayName;

        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        /// <summary>
        /// The client factory.
        /// </summary>
        private readonly IHttpClientFactory clientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderDialog"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="telemetryClient">The telemetry client.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        public OrderDialog(IOptions<BotConfig> config, IBotTelemetryClient telemetryClient, IHttpClientFactory httpClientFactory)
            : base(nameof(OrderDialog))
        {
            this.TelemetryClient = telemetryClient;
            this.botConfig = config;
            this.clientFactory = httpClientFactory;

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
            this.AddDialog(new NextOrder(config, telemetryClient, this.clientFactory));

            // The initial child Dialog to run.
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> TimeDayStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
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



            // Get the Plan
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

            OrderBlob orderBlob = new OrderBlob();
            var tmp = BotMethods.GetSalaryDeduction("2", this.botConfig.Value.GetSalaryDeduction);
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            orderBlob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey));

            var nameID = orderBlob.OrderList.FindIndex(x => x.Name == stepContext.Context.Activity.From.Name);
            if (DateTime.Now.Hour - 1 >= 12)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Es ist schon nach 12 Uhr"));
                return await stepContext.BeginDialogAsync(nameof(OrderForOtherDayDialog));
            }
            else if (nameID != -1)
            {
                var temp = orderBlob.OrderList.FindAll(x => x.Name == stepContext.Context.Activity.From.Name);
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

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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
                string orderDocument = JsonConvert.SerializeObject(order);

                var state = new Dictionary<string, string>
                {
                    { "orderDocument", orderDocument },
                };

                this.TelemetryClient.TrackTrace("Order", Severity.Information, state);
                HttpStatusCode statusOrder = await BotMethods.UploadOrder(order, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                HttpStatusCode statusSalary = await BotMethods.UploadOrderforSalaryDeduction(order, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                HttpStatusCode statusMoney = await BotMethods.UploadMoney(order, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                if (statusMoney == HttpStatusCode.OK || (statusMoney == HttpStatusCode.Created && statusOrder == HttpStatusCode.OK) || (statusOrder == HttpStatusCode.Created && statusSalary == HttpStatusCode.OK) || statusSalary == HttpStatusCode.Created)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Deine bestellung wurde gespeichert."), cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bei deiner bestellung ist etwas schief gegangen. Bitte bestellen sie noch einmal"), cancellationToken);
                    BotMethods.DeleteOrderforSalaryDeduction(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                    BotMethods.DeleteMoney(bufferorder, dayName, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                    BotMethods.DeleteOrder(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
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
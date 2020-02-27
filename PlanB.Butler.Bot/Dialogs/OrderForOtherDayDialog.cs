// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;

using BotLibraryV2;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace PlanB.Butler.Bot
{
    /// <summary>
    /// OrderForOtherDayDialog.
    /// </summary>
    /// <seealso cref="Microsoft.Bot.Builder.Dialogs.ComponentDialog" />
    public class OrderForOtherDayDialog : ComponentDialog
    {
        private static Plan plan = new Plan();
        private static int valueDay;
        private const double grand = 3.30;
        private static string dayName;
        private static List<string> meal1List = new List<string>();
        private static List<string> meal1ListwithMoney = new List<string>();
        private static List<string> meal2List = new List<string>();
        private static List<string> meal2ListWithMoney = new List<string>();
        private static int indexer = 0;
        private static string userName = string.Empty;
        private static int daysDivVal;
        private static CultureInfo culture = new CultureInfo("de-DE");
        private static DayOfWeek[] weekDays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        /// <summary>
        /// OtherDayDialogTimePrompt.
        /// OtherDayDialogError
        /// OtherDayDialogError1
        /// OtherDayDialogError2
        /// OtherDayDialogNamePrompt
        /// OtherDayDialogThanks
        /// NextOrderDialogRestaurantPrompt
        /// NextOrderDialogFoodPrompt
        /// OtherDayDialogOrder
        /// NextOrderDialogSaveOrder.
        /// </summary>

        private static string otherDayDialogTimePrompt = string.Empty;
        private static string otherDayDialogError = string.Empty;
        private static string otherDayDialogError1 = string.Empty;
        private static string otherDayDialogError2 = string.Empty;
        private static string otherDayDialogNamePrompt = string.Empty;
        private static string otherDayDialogThanks = string.Empty;
        private static string nextOrderDialogRestaurantPrompt = string.Empty;
        private static string nextOrderDialogFoodPrompt = string.Empty;
        private static string otherDayDialogOrder = string.Empty;
        private static string nextOrderDialogSaveOrder = string.Empty;

        private IBotTelemetryClient telemetryClient;
        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        public OrderForOtherDayDialog(IOptions<BotConfig> config, IBotTelemetryClient telemetryClient)
            : base(nameof(OrderForOtherDayDialog))
        {
            ResourceManager rm = new ResourceManager("PlanB.Butler.Bot.Dictionary.Dialogs", Assembly.GetExecutingAssembly());
            otherDayDialogTimePrompt = rm.GetString("OtherDayDialog_TimePrompt");
            otherDayDialogError = rm.GetString("OtherDayDialog_Error");
            otherDayDialogError1 = rm.GetString("OtherDayDialog_Error1");
            otherDayDialogError2 = rm.GetString("OtherDayDialog_Error2");
            otherDayDialogNamePrompt = rm.GetString("OtherDayDialog_NamePrompt");
            otherDayDialogThanks = rm.GetString("OtherDayDialog_Thanks");
            nextOrderDialogRestaurantPrompt = rm.GetString("NextOrderDialog_RestaurantPrompt");
            nextOrderDialogFoodPrompt = rm.GetString("NextOrderDialog_FoodPrompt");
            otherDayDialogOrder = rm.GetString("OtherDayDialog_Order");
            nextOrderDialogSaveOrder = rm.GetString("NextOrderDialog_SaveOrder");

            this.telemetryClient = telemetryClient;
            this.botConfig = config;

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
            this.AddDialog(new NextOrder(config, telemetryClient));

            // The initial child Dialog to run.
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the Plan
            string food = BotMethods.GetDocument("eatingplan", "ButlerOverview.json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey);
            plan = JsonConvert.DeserializeObject<Plan>(food);
            // Cards are sent as Attachments in the Bot Framework.
            // So we need to create a list of attachments for the reply activity.
            var attachments = new List<Attachment>();
            List<string> currentWeekDays = new List<string>();

            // Reply to the activity we received with an activity.
            var reply = MessageFactory.Attachment(attachments);

            for (int i = 0; i < weekDays.Length; i++)
            {
                if (weekDays[i].ToString().ToLower() == DateTime.Now.DayOfWeek.ToString().ToLower() && DateTime.Now.Hour < 12)
                {
                    indexer = i;
                }
                else if (weekDays[i].ToString().ToLower() == DateTime.Now.DayOfWeek.ToString().ToLower() && weekDays[i].ToString().ToLower() != "friday")
                {
                    indexer = i + 1;
                }
            }
            for (int i = indexer; i < weekDays.Length; i++)
            {
                currentWeekDays.Add(culture.DateTimeFormat.GetDayName(weekDays[i]));
            }

            if (currentWeekDays != null)
            {
                return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text(otherDayDialogTimePrompt),
                        Choices = ChoiceFactory.ToChoices(currentWeekDays),
                        Style = ListStyle.HeroCard,
                    }, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(otherDayDialogError), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["mainChoise"] = ((FoundChoice)stepContext.Result).Value;
            string text = Convert.ToString(stepContext.Values["mainChoise"]);
            daysDivVal = 0;
            for (int i = 0; i < weekDays.Length; i++)
            {
                if (text.ToString().ToLower() == culture.DateTimeFormat.GetDayName(weekDays[i]).ToString().ToLower())
                {
                    daysDivVal = (int)weekDays[i];
                }
            }
            if (daysDivVal == null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(otherDayDialogError1), cancellationToken);
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
            }
            else
            {
                daysDivVal = daysDivVal - indexer - 1;
            }


            if (text != null)
            {
                valueDay = plan.Planday.FindIndex(x => x.Name == weekDays[indexer].ToString().ToLower());
                dayName = weekDays[indexer].ToString().ToLower();

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
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(otherDayDialogError2), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private static async Task<DialogTurnResult> RestaurantStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.Activity.From.Name == "User")
            {
                stepContext.Values["name"] = (string)stepContext.Result;
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($" {otherDayDialogThanks} {stepContext.Values["name"]}"), cancellationToken);

            return await stepContext.PromptAsync(
                nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text(nextOrderDialogRestaurantPrompt),
                    Choices = GetChoice("restaurant", plan),
                    Style = ListStyle.HeroCard,
                }, cancellationToken);
        }

        private static async Task<DialogTurnResult> FoodStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["restaurant"] = ((FoundChoice)stepContext.Result).Value;

            var otherDayDialogOrder1 = MessageFactory.Text(string.Format(otherDayDialogOrder, stepContext.Values["restaurant"]));

            await stepContext.Context.SendActivityAsync(otherDayDialogOrder1, cancellationToken);

            if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[indexer].Restaurant1.ToLower())
            {
                stepContext.Values["rest1"] = "yes";
                return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text(nextOrderDialogFoodPrompt),
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
                        Prompt = MessageFactory.Text(nextOrderDialogFoodPrompt),
                        Choices = GetChoice("food2", plan),
                        Style = ListStyle.HeroCard,
                    }, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(otherDayDialogError1), cancellationToken);
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
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(otherDayDialogError1), cancellationToken);
                return await stepContext.EndDialogAsync();
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var order = new Order();
            DateTime date = DateTime.Now;
            var stringDate = date.ToString("yyyy-MM-dd");
            order.Date = date;
            order.CompanyStatus = "intern";
            order.Name = (string)stepContext.Values["name"];
            order.Restaurant = (string)stepContext.Values["restaurant"];
            order.Quantaty = 1;
            order.Meal = (string)stepContext.Values["food"];
            order.Price = Convert.ToDouble(stepContext.Values["price"]);
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                var orderblob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey));

                order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]), 2);
                order.Grand = 0;
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
            var state = new Dictionary<string, string>();
            string orderJson = JsonConvert.SerializeObject(order);
            state.Add("Order", orderJson);
            this.telemetryClient.TrackTrace("Order", Severity.Information, state);
            DateTime dateForOrder = DateTime.Now.AddDays(daysDivVal);
            order.Date = dateForOrder;
            HttpStatusCode statusOrder = await BotMethods.UploadForOtherDay(order, dateForOrder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
            HttpStatusCode statusSalary = await BotMethods.UploadOrderforSalaryDeductionForAnotherDay(order, dateForOrder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
            HttpStatusCode statusMoney = await BotMethods.UploadMoney(order, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
            if (statusMoney == HttpStatusCode.OK || (statusMoney == HttpStatusCode.Created && statusOrder == HttpStatusCode.OK) || (statusOrder == HttpStatusCode.Created && statusSalary == HttpStatusCode.OK) || statusSalary == HttpStatusCode.Created)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(nextOrderDialogSaveOrder), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(otherDayDialogError2), cancellationToken);
                BotMethods.DeleteOrderforSalaryDeduction(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                BotMethods.DeleteMoney(bufferorder, weekDays[valueDay].ToString().ToLower(), this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                BotMethods.DeleteOrder(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
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
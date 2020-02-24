// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Resources;
using System.Reflection;

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
        private static CultureInfo culture = new CultureInfo("de-DE");
        private static DayOfWeek[] weekDays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        private static List<Order> orderList = new List<Order>();
        private static List<string> meal1List = new List<string>();
        private static List<string> meal1ListwithMoney = new List<string>();
        private static List<string> meal2List = new List<string>();
        private static List<string> meal2ListWithMoney = new List<string>();

        private static int indexer = 0;
        private static int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
        private const double grand = 3.30;
        private static HttpStatusCode statusOrder;
        private static HttpStatusCode statusSalary;
        private static HttpStatusCode statusMoney;


        private static ResourceManager rm = new ResourceManager("PlanB.Butler.Bot.Dictionary.main", Assembly.GetExecutingAssembly());
        private static string nameTrainee = rm.GetString("nameTrainee");
        private static string me = rm.GetString("me");
        private static string trainee = rm.GetString("trainee");
        private static string costumer = rm.GetString("costumer");
        private static string company = rm.GetString("company");
        private static string restaurant = rm.GetString("restaurant");
        private static string order1 = rm.GetString("order1");
        private static string order2 = rm.GetString("order2");
        private static string food = rm.GetString("food");
        private static string error1 = rm.GetString("error1");
        private static string error2 = rm.GetString("error2");
        private static string saveOrder = rm.GetString("saveOrder");

        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;
        private IBotTelemetryClient telemetryClient;
        /// <summary>
        /// Initializes a new instance of the <see cref="NextOrder" /> class.
        /// NextOrderConstructor.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public NextOrder(IOptions<BotConfig> config, IBotTelemetryClient telemetryClient)
            : base(nameof(NextOrder))
        {
            this.botConfig = config;
            this.telemetryClient = telemetryClient;

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



            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                CompanyStepAsync,
                NameStepAsync,
                RestaurantStepAsync,
                QuantatyStepAsync,
                FoodStepAsync,
                MealQuantatyStepAsync,
                PriceStepAsync,
                SummaryStepAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps) { TelemetryClient = telemetryClient });
            this.AddDialog(new TextPrompt(nameof(TextPrompt)));
            this.AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            this.AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> CompanyStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
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
            stepContext.Values["name"] = stepContext.Context.Activity.From.Name;
            if (companyStatus == "extern")
            {
                stepContext.Values["companyStatus"] = companyStatus;
                return await stepContext.NextAsync();
            }
            else
            {
                //if (DateTime.Now.IsDaylightSavingTime())
                //{

                //    if (DateTime.Now.Hour > 12)
                //    {
                //        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Es ist nach 12 Uhr. Bitte bestelle für einen anderen Tag."));
                //        return await stepContext.BeginDialogAsync(nameof(OrderForOtherDayDialog));
                //    }
                //}
                //else
                //{
                //    if (DateTime.Now.Hour + 1 > 12)
                //    {
                //        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Es ist nach 12 Uhr. Bitte bestelle für einen anderen Tag."));
                //        return await stepContext.BeginDialogAsync(nameof(OrderForOtherDayDialog));
                //    }
                //}


                return await stepContext.PromptAsync(
                              nameof(ChoicePrompt),
                              new PromptOptions
                              {
                                  Prompt = MessageFactory.Text(nameTrainee),
                                  Choices = ChoiceFactory.ToChoices(new List<string> { me, trainee, costumer }),
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
                if (companyStatus.ToLower().ToString() == "kunde" || companyStatus == "extern")
                {
                    if (companyName == " ")
                    {
                        return await stepContext.PromptAsync(
                                                     nameof(TextPrompt),
                                                     new PromptOptions { Prompt = MessageFactory.Text(company) },
                                                     cancellationToken);
                    }
                    else
                    {
                        return await stepContext.NextAsync(null, cancellationToken);
                    }
                }
                else if (companyStatus.ToLower().ToString() == "praktikant")
                {
                    return await stepContext.PromptAsync(
                              nameof(TextPrompt),
                              new PromptOptions { Prompt = MessageFactory.Text(nameTrainee) },
                              cancellationToken);
                }
                else
                    return await stepContext.NextAsync(null, cancellationToken);
            }
            catch (Exception ex)
            {
                if (companyName == " " && companyStatus.ToLower().ToString() == "extern")
                {
                    return await stepContext.PromptAsync(
                                                 nameof(TextPrompt),
                                                 new PromptOptions { Prompt = MessageFactory.Text(company) },
                                                 cancellationToken);
                }
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private static async Task<DialogTurnResult> RestaurantStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {


            if (stepContext.Values["companyStatus"].ToString().ToLower() == "kunde" || stepContext.Values["companyStatus"].ToString().ToLower() == "extern")
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
            else if (stepContext.Values["companyStatus"].ToString().ToLower() == "praktikant")
            {
                stepContext.Values["companyName"] = (string)stepContext.Result;
            }

            if (string.IsNullOrEmpty(plan.Planday[indexer].Restaurant2))
            {
                stepContext.Values["restaurant"] = plan.Planday[indexer].Restaurant1;
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {

                return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text(restaurant),
                        Choices = GetChoice("restaurant", plan),
                        Style = ListStyle.HeroCard,
                    }, cancellationToken);

            }
        }

        private static async Task<DialogTurnResult> QuantatyStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string amountFood = rm.GetString("amountFood");
            try
            {
                stepContext.Values["restaurant"] = ((FoundChoice)stepContext.Result).Value;
            }
            catch (Exception)
            {

            }


            if (leftQuantity == " ")
            {
                if (stepContext.Values["companyStatus"].ToString().ToLower() == "kunde")
                {
                    return await stepContext.PromptAsync(
                        nameof(TextPrompt),
                        new PromptOptions
                        {
                            Prompt = MessageFactory.Text(amountFood),
                        }, cancellationToken);
                }
                else
                {
                    return await stepContext.NextAsync(null, cancellationToken);
                }
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private static async Task<DialogTurnResult> FoodStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{order1} {stepContext.Values["restaurant"]} {order2}"), cancellationToken);

            if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[indexer].Restaurant1.ToLower())
            {
                stepContext.Values["rest1"] = "yes";
                return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text(food),
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
                        Prompt = MessageFactory.Text(food),
                        Choices = GetChoice("food2", plan),
                        Style = ListStyle.HeroCard,
                    }, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(error1), cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }


        private static async Task<DialogTurnResult> MealQuantatyStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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
                stepContext.Values["price"] = plan.Planday[indexer].Meal1[foodId].Price;
            }
            else if (stepContext.Values["restaurant"].ToString().ToLower() == plan.Planday[dayId].Restaurant2.ToLower())
            {
                int foodId = plan.Planday[indexer].Meal2.FindIndex(x => x.Name == (string)stepContext.Values["food"]);
                stepContext.Values["price"] = plan.Planday[indexer].Meal2[foodId].Price;
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            DateTime date = DateTime.Now;
            var stringDate = date.ToString("yyyy-MM-dd");

            var order = new Order();
            if (stepContext.Values["companyStatus"].ToString().ToLower() == "für mich" || stepContext.Values["companyStatus"].ToString().ToLower() == "intern")
            {
                order.Date = DateTime.Now;
                order.CompanyStatus = "intern";
                order.Name = (string)stepContext.Values["name"];
                order.CompanyName = "PlanB";
                order.Restaurant = (string)stepContext.Values["restaurant"];
                order.Quantaty = Convert.ToInt32(stepContext.Values["quantaty"]);
                order.Meal = (string)stepContext.Values["food"];
                order.Price = Convert.ToDouble(stepContext.Values["price"]);


                try
                {
                    var orderblob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey));
                    var nameId = orderblob.OrderList.FindIndex(x => x.Name == order.Name);
                    order.Grand = 0;
                    order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]), 2);
                }
                catch (Exception)
                {
                    order.Grand = 3.3;
                    if (order.Price <= grand)
                    {
                        order.Price = 0;
                    }
                    else

                        order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]) - grand, 2);
                }

                var bufferorder = order;
                HttpStatusCode statusOrder = BotMethods.UploadOrder(order, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                HttpStatusCode statusSalary = BotMethods.UploadOrderforSalaryDeduction(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                HttpStatusCode statusMoney = BotMethods.UploadMoney(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                if (statusMoney == HttpStatusCode.OK || (statusMoney == HttpStatusCode.Created && statusOrder == HttpStatusCode.OK) || (statusOrder == HttpStatusCode.Created && statusSalary == HttpStatusCode.OK) || statusSalary == HttpStatusCode.Created)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(saveOrder), cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(error2), cancellationToken);
                    BotMethods.DeleteOrderforSalaryDeduction(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                    BotMethods.DeleteMoney(bufferorder, weekDays[indexer].ToString().ToLower(), this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                    BotMethods.DeleteOrder(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                    await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
                }
            }
            else if (stepContext.Values["companyStatus"].ToString().ToLower() == "kunde" || stepContext.Values["companyStatus"].ToString().ToLower() == "extern")
            {
                order.Date = DateTime.Now;
                order.CompanyStatus = "extern";
                order.CompanyName = (string)stepContext.Values["companyName"];
                order.Name = (string)stepContext.Values["name"];
                order.Restaurant = (string)stepContext.Values["restaurant"];
                order.Quantaty = 1;
                order.Meal = (string)stepContext.Values["food"];
                order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]), 2);
                order.Grand = 0;
                orderList.Add(order);
                companyStatus = "extern";
                companyName = " ";
                if (leftQuantity != " ")
                {
                    leftQuantity = Convert.ToString(Convert.ToInt32(leftQuantity) - 1);
                }

                if (Convert.ToInt32(leftQuantity) >= 1)
                {
                    await stepContext.EndDialogAsync(null, cancellationToken);
                    return await stepContext.BeginDialogAsync(nameof(NextOrder), null, cancellationToken);
                }
                if (leftQuantity == "0")
                {

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
                        statusOrder = BotMethods.UploadOrder(tempOrder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                        statusSalary = BotMethods.UploadOrderforSalaryDeduction(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                        statusMoney = BotMethods.UploadMoney(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                        if (statusMoney == HttpStatusCode.OK || (statusMoney == HttpStatusCode.Created && statusOrder == HttpStatusCode.OK) || (statusOrder == HttpStatusCode.Created && statusSalary == HttpStatusCode.OK) || statusSalary == HttpStatusCode.Created)
                        {

                        }
                        else
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text(error2), cancellationToken);
                            BotMethods.DeleteOrderforSalaryDeduction(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                            BotMethods.DeleteMoney(bufferorder, weekDays[indexer].ToString().ToLower(), this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                            BotMethods.DeleteOrder(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                            await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                            return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
                        }
                        companyStatus = " ";
                        companyName = " ";
                        leftQuantity = " ";
                    }
                    if (leftQuantity == " ")
                    {
                        orderList.Clear();
                    }
                }
            }
            else if (stepContext.Values["companyStatus"].ToString().ToLower() == "praktikant" || stepContext.Values["companyStatus"].ToString().ToLower() == "intership")
            {
                order.Date = DateTime.Now;
                order.CompanyStatus = "internship";
                order.CompanyName = (string)stepContext.Values["companyName"];
                order.Name = (string)stepContext.Values["name"];
                order.Restaurant = (string)stepContext.Values["restaurant"];
                order.Quantaty = Convert.ToInt32(stepContext.Values["quantaty"]);
                order.Meal = (string)stepContext.Values["food"];
                order.Price = Math.Round(Convert.ToDouble(stepContext.Values["price"]), 2);
                order.Grand = 0;
                var bufferorder = order;
                statusOrder = BotMethods.UploadOrder(order, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                statusSalary = BotMethods.UploadOrderforSalaryDeduction(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
                statusMoney = BotMethods.UploadMoney(bufferorder, this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);
            }
            var state = new Dictionary<string, string>();
            string orderJson = JsonConvert.SerializeObject(order);
            state.Add("Order", orderJson);
            this.telemetryClient.TrackTrace("Order", Severity.Information, state);
            await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync(nameof(DailyCreditDialog), null, cancellationToken);

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
    }
}



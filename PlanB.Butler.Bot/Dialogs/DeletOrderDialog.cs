﻿using System;
using System.Collections.Generic;
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
using PlanB.Butler.Bot;

namespace PlanB.Butler.Bot
{
    /// <summary>
    /// DeleteOrderDialog.
    /// </summary>
    /// <seealso cref="Microsoft.Bot.Builder.Dialogs.ComponentDialog" />
    public class DeleteOrderDialog : ComponentDialog
    {
        static Plan plan = new Plan();
        static Plan orderedfood = new Plan();
        static int valueDay;
        const double grand = 3.30;
        static string dayName;
        static string[] weekDays = { "Montag", "Dienstag", "Mitwoch", "Donnerstag", "Freitag" };
        static string[] weekDaysEN = { "monday", "tuesday", "wednesday", "thursday", "friday" };
        static int indexer = 0;
        static string[] companyStatus = { "intern", "extern", "internship" };
        static string[] companyStatusD = { "Für mich", "Kunde", "Praktikant" };
        static Order obj = new Order();

        private static ResourceManager rm = new ResourceManager("PlanB.Butler.Bot.Dictionary.main", Assembly.GetExecutingAssembly());
        private static string DeletDialog_TimePrompt = rm.GetString("DeletDialog_TimePrompt");
        private static string DeletDialog_WhoPrompt = rm.GetString("DeletDialog_WhoPrompt");
        private static string NextOrderDialog_Myself = rm.GetString("NextOrderDialog_Myself");
        private static string NextOrderDialog_Trainee = rm.GetString("NextOrderDialog_Trainee");
        private static string NextOrderDialog_Costumer = rm.GetString("NextOrderDialog_Costumer");
        private static string DeletDialog_NoOrder = rm.GetString("DeletDialog_NoOrder");
        private static string DeletDialog_DeleteSucess = rm.GetString("DeletDialog_DeleteSucess");
        private static string DeletDialog_DeletePrompt = rm.GetString("DeletDialog_DeletePrompt");        
        private static string yes = rm.GetString("yes");
        private static string no = rm.GetString("no");
        private static string OtherDayDialog_Error2 = rm.GetString("OtherDayDialog_Error2");

        

        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteOrderDialog"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public DeleteOrderDialog(IOptions<BotConfig> config)
            : base(nameof(DeleteOrderDialog))
        {
            this.botConfig = config;

            // Get the Plan
            string food = BotMethods.GetDocument("eatingplan", "ButlerOverview.json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey);

            plan = JsonConvert.DeserializeObject<Plan>(food);
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
                {
                this.InitialStepAsync,
                CompanyStepAsync,
                this.NameStepAsync,
                RemoveStepAsync,
                DeleteOrderStep,
                };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.AddDialog(new TextPrompt(nameof(TextPrompt)));
            this.AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            this.AddDialog(new NextOrder(config));

            // The initial child Dialog to run.
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Cards are sent as Attachments in the Bot Framework.
            // So we need to create a list of attachments for the reply activity.
            var attachments = new List<Attachment>();
            List<string> currentWeekDays = new List<string>();

            // Reply to the activity we received with an activity.
            var reply = MessageFactory.Attachment(attachments);


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
                        Prompt = MessageFactory.Text(DeletDialog_TimePrompt),
                        Choices = ChoiceFactory.ToChoices(currentWeekDays),
                        Style = ListStyle.HeroCard,
                    }, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(OtherDayDialog_Error2), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private static async Task<DialogTurnResult> CompanyStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["mainChoise"] = ((FoundChoice)stepContext.Result).Value;
            string text = stepContext.Values["mainChoise"].ToString();
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

            stepContext.Values["name"] = stepContext.Context.Activity.From.Name;
            return await stepContext.PromptAsync(
               nameof(ChoicePrompt),
               new PromptOptions
               {
                   Prompt = MessageFactory.Text(DeletDialog_WhoPrompt),
                   Choices = ChoiceFactory.ToChoices(new List<string> { NextOrderDialog_Myself, NextOrderDialog_Costumer, NextOrderDialog_Trainee }),
                   Style = ListStyle.HeroCard,
               }, cancellationToken);
        }


        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["companyStatus"] = ((FoundChoice)stepContext.Result).Value;
            for (int i = 0; i < companyStatusD.Length; i++)
            {
                if (stepContext.Values["companyStatus"].ToString() == companyStatusD[i])
                {
                    stepContext.Values["companyStatus"] = companyStatus[i];
                }
            }

            valueDay = plan.Planday.FindIndex(x => x.Name == weekDaysEN[indexer]);
            dayName = weekDaysEN[indexer];
            stepContext.Values["name"] = stepContext.Context.Activity.From.Name;
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> RemoveStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {                   
            try
            {
                var order = new Order();
                order.CompanyStatus = stepContext.Values["companyStatus"].ToString();
                order.Name = (string)stepContext.Values["name"].ToString();
                List<Order> mealVal = new List<Order>();
                OrderBlob orderBlob = new OrderBlob();
                string[] weekDaysList = { "monday", "tuesday", "wednesday", "thursday", "friday" };
                int indexDay = 0;
                int indexCurentDay = 0;
                string currentDay = DateTime.Now.DayOfWeek.ToString().ToLower();
                DateTime date = DateTime.Now;
                var stringDate = string.Empty;
                for (int i = 0; i < weekDaysList.Length; i++)
                {
                    if (currentDay == weekDaysList[i])
                    {
                        indexCurentDay = i;
                    }
                    if (weekDaysEN[indexer] == weekDaysList[i])
                    {
                        indexDay = i;
                    }
                }
                if (indexDay == indexCurentDay)
                {
                    stringDate = date.ToString("yyyy-MM-dd");
                }
                else
                {
                    indexCurentDay = indexDay - indexCurentDay;
                    date = DateTime.Now.AddDays(indexCurentDay);
                    stringDate = date.ToString("yyyy-MM-dd");
                }
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey));
                var collection = orderBlob.OrderList.FindAll(x => x.Name == order.Name);
                obj = collection.FindLast(x => x.CompanyStatus == order.CompanyStatus);

                var DeletDialog_DeletePrompt1 = string.Format(DeletDialog_DeletePrompt,obj.Meal); //Should ... be deleted?

                return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions

                    {
                        Prompt = MessageFactory.Text(DeletDialog_DeletePrompt1), 
                        Choices = ChoiceFactory.ToChoices(new List<string> { yes, no }),
                        Style = ListStyle.HeroCard,
                    }, cancellationToken);
            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(DeletDialog_NoOrder), cancellationToken);
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> DeleteOrderStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["choise"] = ((FoundChoice)stepContext.Result).Value;
            var text = stepContext.Values["choise"];
            if (text.ToString().ToLower() == "ja")
            {
                var bufferOrder = obj;
                var order = bufferOrder;

                DeleteOrder(order, this.botConfig.Value.ServiceBusConnectionString);

                DeleteOrderforSalaryDeduction(bufferOrder, this.botConfig.Value.ServiceBusConnectionString);
                BotMethods.DeleteMoney(bufferOrder, weekDaysEN[indexer], this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey, this.botConfig.Value.ServiceBusConnectionString);

                await stepContext.Context.SendActivityAsync(MessageFactory.Text(DeletDialog_DeleteSucess), cancellationToken);
                await stepContext.EndDialogAsync();
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog));
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(DeletDialog_DeleteSucess), cancellationToken);
                await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
            }
        }

        public Order GetOrder(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            orderBlob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey));

            var bufferOrder = orderBlob.OrderList.FindAll(x => x.Name == order.Name);

            var temp = bufferOrder.FindAll(x => x.CompanyStatus == order.CompanyStatus);
            var orderValue = temp[temp.Count - 1];
            return orderValue;
        }
        public void DeleteOrder(Order order, string serviceBusConnectionString)
        {
            string date = order.Date.ToString("yyyy-MM-dd");
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + date + "_" + order.Name + ".json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey));
                int orderID = orderBlob.OrderList.FindIndex(x => x.Name == order.Name);
                orderBlob.OrderList.RemoveAt(orderID);
                BotMethods.PutDocument("orders", "orders_" + date + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder", serviceBusConnectionString);
            }
            catch (Exception ex) // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                orders.Add(order);
                BotMethods.PutDocument("orders", "orders_" + date + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder", serviceBusConnectionString);
            }
        }

        /// <summary>
        /// Deletes the orderfor salary deduction.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="serviceBusConnectionString">The service bus connection string.</param>
        public void DeleteOrderforSalaryDeduction(Order order, string serviceBusConnectionString)
        {
            SalaryDeduction salaryDeduction = new SalaryDeduction();
            var dayId = order.Date.Date.DayOfYear;
            salaryDeduction = JsonConvert.DeserializeObject<SalaryDeduction>(BotMethods.GetDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year + ".json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey));
            var collection = salaryDeduction.Order.FindAll(x => x.Name == order.Name);
            var temp = collection.FindAll(x => x.CompanyStatus == order.CompanyStatus);
            salaryDeduction.Order.Remove(temp[temp.Count - 1]);

            try
            {
                BotMethods.PutDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction), "q.planbutlerupdatesalary", serviceBusConnectionString);
            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                salaryDeduction.Daynumber = dayId;
                salaryDeduction.Name = "SalaryDeduction";

                orders.Add(order);
                salaryDeduction.Order = orders;

                BotMethods.PutDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction), "q.planbutlerupdatesalary", serviceBusConnectionString);
            }
        }
    }
}
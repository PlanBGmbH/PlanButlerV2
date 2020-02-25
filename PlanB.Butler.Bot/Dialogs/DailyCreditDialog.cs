// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;

using BotLibraryV2;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace PlanB.Butler.Bot
{
    /// <summary>
    /// DailyCreditDialog.
    /// </summary>
    /// <seealso cref="Microsoft.Bot.Builder.Dialogs.ComponentDialog" />
    public class DailyCreditDialog : ComponentDialog
    {
        private static ResourceManager rm = new ResourceManager("PlanB.Butler.Bot.Dictionary.main", Assembly.GetExecutingAssembly());
        private static string DailyCreditDialog_Depts = rm.GetString("DailyCreditDialog_Depts");
        private static string DailyCreditDialog_OrderMe = rm.GetString("DailyCreditDialog_OrderMe");
        private static string DailyCreditDialog_OrderCostumer = rm.GetString("DailyCreditDialog_OrderCostumer");
        private static string DailyCreditDialog_OrderTrainee = rm.GetString("DailyCreditDialog_OrderTrainee");
        private static string DailyCreditDialog_SumMe = rm.GetString("DailyCreditDialog_SumMe");
        private static string DailyCreditDialog_SumCostumer = rm.GetString("DailyCreditDialog_SumCostumer");
        private static string DailyCreditDialog_SumTrainee = rm.GetString("DailyCreditDialog_SumTrainne");
        private static string DailyCreditDialog_OrderedAt = rm.GetString("DailyCreditDialog_OrderedAt");
        
        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        public Activity TextPrompt { get; private set; }

        public DailyCreditDialog(IOptions<BotConfig> config)
            : base(nameof(DailyCreditDialog))
        {
            this.botConfig = config;

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
            stepContext.Values["name"] = stepContext.Context.Activity.From.Name;
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> GetMoneyStepAsync1(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var msg = string.Empty;

            int dayNumber = DateTime.Now.DayOfYear;
            SalaryDeduction money = JsonConvert.DeserializeObject<SalaryDeduction>(BotMethods.GetDocument("salarydeduction", "orders_" + dayNumber.ToString() + "_" + DateTime.Now.Year + ".json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey));
            var userId = money.Order.FindIndex(x => x.Name == (string)stepContext.Values["name"]);
            try
            {
                string name = stepContext.Values["name"].ToString();
                var orderList = await BotMethods.GetDailyUserOverview(name, this.botConfig.Value.GetDailyUserOverviewFunc);
                OrderBlob orderBlob = new OrderBlob();
               // var DailyCreditDialog_Depts = MessageFactory(string.Format(DailyCreditDialog_Depts)); Daily Depts 

                msg += $"{DailyCreditDialog_Depts} {Environment.NewLine}";
                string message = "";
                string orders = $"{DailyCreditDialog_OrderMe} {Environment.NewLine}";
                double sum = 0;
                string corders = $"{DailyCreditDialog_OrderCostumer}  {Environment.NewLine}";
                double csum = 0;
                string iorders = $"{DailyCreditDialog_OrderTrainee}  {Environment.NewLine}";
                double isum = 0;
                bool check = false;
                bool cchecker = false;
                bool ichecker = false;
                foreach (var item in orderList)
                {
                    foreach (var items in item.OrderList)
                    {

                        if (items.CompanyStatus.ToLower().ToString() == "extern")
                        {
                            corders += $"{items.CompanyName} \t/ {items.Restaurant} \t/ {items.Meal} \t/ {items.Price}€ {Environment.NewLine}";
                            csum += Convert.ToDouble(items.Price);
                            cchecker = true;
                        }
                        else if (items.CompanyStatus.ToLower().ToString() == "internship")
                        {
                            iorders += $"{items.CompanyName} \t/ {items.Restaurant} \t/ {items.Meal} \t/ {items.Price}€ {Environment.NewLine}";
                            isum += Convert.ToDouble(items.Price);
                            ichecker = true;
                        }
                        else
                        {
                            string DailyCreditDialog_OrderedAt1 = string.Format(DailyCreditDialog_OrderedAt, items.Meal, items.Restaurant);

                            if (check = false)
                            {
                                message= DailyCreditDialog_OrderedAt1;
                            }
                            orders += $"{items.Name} \t/ {items.Restaurant} \t/ {items.Meal} \t/ {items.Price}€  {Environment.NewLine}";
                            sum += Convert.ToDouble(items.Price);
                            check = true;
                        }
                    }
                }
                if (check)
                {
                    var DailyCreditDialog_SumMe1 = string.Format(DailyCreditDialog_SumMe, sum);
                    orders += DailyCreditDialog_SumMe1;
                }
                if (cchecker)
                {
                    var DailyCreditDialog_SumCostumer1 = string.Format(DailyCreditDialog_SumCostumer, csum);                  
                    orders += corders;
                    corders += DailyCreditDialog_SumCostumer1;
                }
                if (ichecker)
                {
                    var DailyCreditDialog_SumTrainee1 = string.Format(DailyCreditDialog_SumTrainee, isum);
                    iorders += DailyCreditDialog_SumTrainee1;
                    orders += iorders;
                }
                msg += $"{orders}";
            }
            catch
            {

            }

            // Get the Order from the BlobStorage, the current day ID and nameId from the user

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
            await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            return await stepContext.BeginDialogAsync(nameof(OverviewDialog));

        }

        private async Task<DialogTurnResult> GetMoneyStepAsync2(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.EndDialogAsync();
            return await stepContext.BeginDialogAsync(nameof(OverviewDialog));
        }


    }
}

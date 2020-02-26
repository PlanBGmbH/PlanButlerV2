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
        /// <summary>
        /// DailyCreditDialogDepts.
        /// DailyCreditDialogOrderMe
        /// DailyCreditDialogOrderCostumer
        /// DailyCreditDialogOrderTrainee
        /// DailyCreditDialogSumMe
        /// DailyCreditDialogSumCostumer
        /// DailyCreditDialogSumTrainee
        /// DailyCreditDialogOrderedAt.
        /// </summary>
        
        private static readonly string DailyCreditDialogDepts = rm.GetString("DailyCreditDialog_Depts");
        private static readonly string DailyCreditDialogOrderMe = rm.GetString("DailyCreditDialog_OrderMe");
        private static readonly string DailyCreditDialogOrderCostumer = rm.GetString("DailyCreditDialog_OrderCostumer");
        private static readonly string DailyCreditDialogOrderTrainee = rm.GetString("DailyCreditDialog_OrderTrainee");
        private static readonly string DailyCreditDialogSumMe = rm.GetString("DailyCreditDialog_SumMe");
        private static readonly string DailyCreditDialogSumCostumer = rm.GetString("DailyCreditDialog_SumCostumer");
        private static readonly string DailyCreditDialogSumTrainee = rm.GetString("DailyCreditDialog_SumTrainne");
        private static readonly string DailyCreditDialogOrderedAt = rm.GetString("DailyCreditDialog_OrderedAt");
        private static ResourceManager rm = new ResourceManager("PlanB.Butler.Bot.Dictionary.main", Assembly.GetExecutingAssembly());

        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        public DailyCreditDialog(IOptions<BotConfig> config, IBotTelemetryClient telemetryClient)
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

                msg += $"{DailyCreditDialogDepts} {Environment.NewLine}";
                string message = string.Empty;
                string orders = $"{DailyCreditDialogOrderMe} {Environment.NewLine}";
                double sum = 0;
                string corders = $"{DailyCreditDialogOrderCostumer}  {Environment.NewLine}";
                double csum = 0;
                string iorders = $"{DailyCreditDialogOrderTrainee}  {Environment.NewLine}";
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
                            var dailyCreditDialogOrderedAt = string.Format(DailyCreditDialogOrderedAt, items.Meal, items.Restaurant);

                            if (check = false)
                            {
                                message = dailyCreditDialogOrderedAt;
                            }
                            orders += $"{items.Name} \t/ {items.Restaurant} \t/ {items.Meal} \t/ {items.Price}€  {Environment.NewLine}";
                            sum += Convert.ToDouble(items.Price);
                            check = true;
                        }
                    }
                }

                if (check)
                {
                    var dailyCreditDialogSumMe = string.Format(DailyCreditDialogSumMe, sum);
                    orders += $"{dailyCreditDialogSumMe} {Environment.NewLine}";
                }

                if (cchecker)
                {
                    var dailyCreditDialogSumCostumer = string.Format(DailyCreditDialogSumCostumer, csum);
                    orders += corders;
                    corders += $"{dailyCreditDialogSumCostumer} {Environment.NewLine}";
                }

                if (ichecker)
                {
                    var dailyCreditDialogSumTrainee = string.Format(DailyCreditDialogSumTrainee, csum);
                    iorders += $"{dailyCreditDialogSumTrainee} {Environment.NewLine}";
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

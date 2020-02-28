// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
    /// InterruptDialog.
    /// </summary>
    /// <seealso cref="Microsoft.Bot.Builder.Dialogs.ComponentDialog" />
    public class InterruptDialog : ComponentDialog
    {
        /// <summary>
        /// InterruptDialogHelpText.
        /// </summary>

        private static string interruptDialogHelpText = string.Empty;


        private static Plan plan = new Plan();
        private readonly IOptions<BotConfig> botConfig;

        public InterruptDialog(string v, IOptions<BotConfig> config, IBotTelemetryClient telemetryClient)
            : base(nameof(InterruptDialog))
        {
            ResourceManager rm = new ResourceManager("PlanB.Butler.Bot.Dictionary.Dialogs", Assembly.GetExecutingAssembly());
            interruptDialogHelpText = rm.GetString("InterruptDialog_HelpText");

            this.botConfig = config;
            this.AddDialog(new OverviewDialog(config, telemetryClient));
            this.AddDialog(new ExcellDialog(config, telemetryClient));
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await this.InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await this.InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<DialogTurnResult> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {

            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                var text = innerDc.Context.Activity.Text.ToLowerInvariant();

                if (text == "help" || text == "hilfe")
                {
                    // Help message!                   
                    await innerDc.Context.SendActivityAsync(MessageFactory.Text(interruptDialogHelpText), cancellationToken);
                    await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
                    return await innerDc.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
                }
                else if ((text.Contains("wurde") || text.Contains("worden")) && text.Contains("heute") && text.Contains("bestellt"))
                {
                    // Get the Order from the BlobStorage and the current day ID
                    int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
                    //orderBlob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                    HttpRequestMessage req = new HttpRequestMessage();
                    List<OrderBlob> tmp = await BotMethods.GetDailyOverview(this.botConfig.Value.GetDailyOverviewFunc);

                    string orderlist = string.Empty;

                    foreach (var item in tmp)
                    {
                        foreach (var items in item.OrderList)
                        {
                            if (items.Quantaty > 1)
                            {
                                orderlist += $"{items.Name}: {items.Meal} x{items.Quantaty}  {Environment.NewLine}";
                            }
                            else
                            {
                                orderlist += $"{items.Name}: {items.Meal}  {Environment.NewLine}";
                            }
                        }
                    }

                    await innerDc.Context.SendActivityAsync(MessageFactory.Text($"Es wurde bestellt:  {Environment.NewLine}{orderlist}"), cancellationToken);
                    await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
                    return await innerDc.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
                }
                else if (text.Contains("ich") && text.Contains("heute") && text.Contains("bestellt"))
                {
                    // Get the Order from the BlobStorage, the current day ID and nameId from the user
                    OrderBlob orderBlob = new OrderBlob();
                    int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
                    orderBlob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey));
                    var nameID = orderBlob.OrderList.FindAll(x => x.Name == innerDc.Context.Activity.From.Name);

                    if (nameID.Count != 0)
                    {
                        string message = $"Du hast heute {nameID.LastOrDefault().Meal} bei {nameID.LastOrDefault().Restaurant} bestellt.";
                        if (nameID.Count > 1)
                        {
                            message = string.Empty;
                            string orders = string.Empty;
                            foreach (var item in nameID)
                            {

                                if (item.CompanyStatus.ToLower().ToString() == "kunde" || item.CompanyStatus.ToLower().ToString() == "privat" || item.CompanyStatus.ToLower().ToString() == "praktikant")
                                {
                                    orders += $"Für {item.CompanyName}: {item.Meal} x{item.Quantaty}  {Environment.NewLine}";
                                }
                                else if (item.CompanyStatus == "intern")
                                {
                                    orders += $"{item.Name}: {item.Meal}  {Environment.NewLine}";
                                }
                                else
                                {
                                    orders += $"{item.Name}: {item.Meal}  {Environment.NewLine}";
                                }

                                if (nameID.LastOrDefault() != item)
                                {
                                    orders += " und ";
                                }
                            }

                            message = $"Du hast heute {orders} bestellt";
                        }

                        await innerDc.Context.SendActivityAsync(MessageFactory.Text(message), cancellationToken);
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(MessageFactory.Text($"Du hast heute noch nichts bestellt!"), cancellationToken);
                    }

                    await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
                    return await innerDc.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
                }
                else if (text.Contains("ende") || text.Contains("exit"))
                {
                    await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
                    return await innerDc.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
                }
                else if (text.Contains("excel"))
                {
                    await innerDc.Context.SendActivityAsync(MessageFactory.Text($"Einen Moment ich suche schnell alles zusammen!"), cancellationToken);

                    await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
                    return await innerDc.BeginDialogAsync(nameof(ExcellDialog), null, cancellationToken);

                    //await innerDc.Context.SendActivityAsync(MessageFactory.Text(message), cancellationToken);
                }
            }
            return null;
        }


    }
}

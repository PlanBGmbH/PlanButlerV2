namespace ButlerBot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BotLibraryV2;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Connector.Teams;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Newtonsoft.Json;
    using OfficeOpenXml;

    public class InterruptDialog : ComponentDialog
    {
        private static Plan plan = new Plan();

        public InterruptDialog(string v)
            : base(nameof(InterruptDialog))
        {
            this.AddDialog(new OverviewDialog());
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
                    string message = "Hallo,\nIch bin dein persönlicher Essensbestell Bot.\nZu den schon angezeigten befehlen kann ich noch\n" +
                        " - Ein weiteres essen für z.B. Praktikanten oder Kunden bestellen. Daür bitte 'Weiteres Essen bestellen' eingeben.\n" +
                        " - Ein essen für einen anderen Tag in dieser Woche bestellen. Dafür bitte 'für einen anderen Tag Essen bestellen eingeben.\n" +
                        " - Fragen was man heute bestellt hat zur Kontrolle. Dafür bitte 'Was habe ich heute bestellt' eingeben.";
                    await innerDc.Context.SendActivityAsync(MessageFactory.Text(message), cancellationToken);
                    await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
                    return await innerDc.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
                }
                else if ((text.Contains("wurde") || text.Contains("worden")) && text.Contains("heute") && text.Contains("bestellt"))
                {
                    // Get the Order from the BlobStorage and the current day ID
                    OrderBlob orderBlob = new OrderBlob();
                    int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
                    orderBlob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));

                    string orderlist = string.Empty;

                    foreach (var item in orderBlob.OrderList)
                    {
                        if (item.Quantaty > 1)
                        {
                            orderlist += $"{item.Name}: {item.Meal} x{item.Quantaty}  {Environment.NewLine}";
                        }
                        else
                        {
                            orderlist += $"{item.Name}: {item.Meal}  {Environment.NewLine}";
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
                    orderBlob = JsonConvert.DeserializeObject<OrderBlob>(BotMethods.GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
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
                    return await innerDc.BeginDialogAsync(nameof(OverviewDialog));
                }
                else if (text.Contains("excel"))
                {
                    await innerDc.Context.SendActivityAsync(MessageFactory.Text($"Einen Moment ich suche schnell alles zusammen!"), cancellationToken);
                    // string[] name = innerDc.Context.Activity.From.Name.Split(' ');
                   // getExcel.Run();// name[0] + name[1][0]
                    string monthid = "";
                    if (DateTime.Now.Month < 10)
                    {
                        monthid = "0" + DateTime.Now.Month.ToString();
                    }
                    else
                    {
                        monthid = DateTime.Now.Month.ToString();
                    }
                    var temp = BotMethods.GetDocument("excel", "Monatsuebersicht_" + monthid + "_" + DateTime.Now.Year + ".xlsx");
                    //session.send(temp);
                    
                    //await innerDc.Context.SendActivityAsync(MessageFactory.Text(message), cancellationToken);
                }
            }
            return null;
        }

    }
}

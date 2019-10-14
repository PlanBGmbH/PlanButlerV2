using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace ButlerBot
{
    public class OrderDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<Order> _OrderAccessor;
        public OrderDialog(UserState userState)
            : base(nameof(OrderDialog))
        {
            _OrderAccessor = userState.CreateProperty<Order>("Order");

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
                {
                NameStepAsync,
                RestaurantStepAsync,
                FoodStepAsync,
                PriceStepAsync,
                SummaryStepAsync
                };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Bitte gib deinen Namen ein.") }, cancellationToken);
        }

        private static async Task<DialogTurnResult> RestaurantStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["name"] = (string)stepContext.Result;

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Danke {stepContext.Values["name"]}"), cancellationToken);

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                            new PromptOptions
                            {
                                Prompt = MessageFactory.Text("In welchem Restaurant möchtest du Bestellen?"),
                                Choices = ChoiceFactory.ToChoices(new List<string> { "Bieg", "Pizza Mario" }),
                            }, cancellationToken);
        }

        private static async Task<DialogTurnResult> FoodStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["restaurant"] = ((FoundChoice)stepContext.Result).Value;

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Du willst bei {stepContext.Values["restaurant"]} bestellen."), cancellationToken);

            if ((string)stepContext.Values["restaurant"] == "Bieg")
            {
                return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Welches Essen möchtest du bestellen?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Käsespätzle", "SchniPoSa", "Rindsrouladen" }),
                }, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync(nameof(ChoicePrompt),
               new PromptOptions
               {
                   Prompt = MessageFactory.Text("Welches Essen möchtest du bestellen?"),
                   Choices = ChoiceFactory.ToChoices(new List<string> { "Pizza Opa", "Schinken Pizza", "Salami Pizza" }),
               }, cancellationToken);
            }
        }

        private static async Task<DialogTurnResult> PriceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["food"] = ((FoundChoice)stepContext.Result).Value;

            var rnd = new Random();

            stepContext.Values["price"] = Convert.ToString(rnd.Next());

            var msg = $"Danke fürs bestellen {stepContext.Values["name"]}, du hast {stepContext.Values["food"]} von {stepContext.Values["restaurant"]} ausgewählt" +
                $" und deine Bestellung wird {stepContext.Values["price"]} kosten.";

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Passt das so?") }, cancellationToken);
        }

        private static async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var order = new Order();

                order.Name = (string)stepContext.Values["name"];
                order.Restaurant = (string)stepContext.Values["restaurant"];
                order.Food = (string)stepContext.Values["food"];
                order.Price = (int)stepContext.Values["price"];
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Okay deine Bestellung wird nicht gespeichert."), cancellationToken);
            }
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
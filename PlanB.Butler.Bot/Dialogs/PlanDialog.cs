namespace PlanB.Butler.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
     using BotLibraryV2;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Choices;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;

    public class PlanDialog : ComponentDialog
    {
        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        public PlanDialog(IOptions<BotConfig> config, IBotTelemetryClient telemetryClient)
            : base(nameof(PlanDialog))
        {
            this.botConfig = config;

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
               RestaurantStepAsync,
               SendPictureStepAsync,
               ReturnStepAsync
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.AddDialog(new TextPrompt(nameof(TextPrompt)));
            this.AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            this.AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> RestaurantStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Von welchem Restaurant möchtest du die Speisekarte sehen?"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Bieg", "Delphi", "Leib und Seele", "Liederhalle" ,"Feasy","La Boussola"}),
                Style = ListStyle.HeroCard,
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> SendPictureStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["restaurant"] = ((FoundChoice)stepContext.Result).Value;
            string restaurant = stepContext.Values["restaurant"].ToString();

            var picture = BotMethods.GetDocument("pictures", restaurant.Replace(' ', '_') + ".txt", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey);
            if (!picture.Contains("BlobNotFound"))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Okay, hier der Essensplan von " + restaurant), cancellationToken);
                await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(new Attachment("image/png", picture)), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Für " + restaurant + " ist leider keine Speisekarte vorhanden."), cancellationToken);
            }

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Willst du einen weitere Karte anschauen?"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Ja", "Nein" }),
                Style = ListStyle.HeroCard,
            });
        }

        private static async Task<DialogTurnResult> ReturnStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Choise"] = ((FoundChoice)stepContext.Result).Value;
            if (stepContext.Values["Choise"].ToString().ToLower() == "ja")
            {
                await stepContext.EndDialogAsync();
                return await stepContext.BeginDialogAsync(nameof(PlanDialog),null,cancellationToken);
            }
            else
            {
                await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
            }
        }
    }
}

namespace PlanB.Butler.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Resources;
    using System.Threading;
    using System.Threading.Tasks;
    using BotLibraryV2;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Choices;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;

    public class PlanDialog : ComponentDialog
    {
        /// <summary>
        /// PlanDialogMenuCardPrompt.
        /// PlanDialogMenuCard
        /// PlanDialogOtherMenuCard
        /// PlanDialogNoMenuCard
        /// PlanDialogYes
        /// PlanDialogNo.
        /// </summary>

        private static string planDialogMenuCardPrompt = string.Empty;
        private static string planDialogMenuCard = string.Empty;
        private static string planDialogOtherMenuCard = string.Empty;
        private static string planDialogNoMenuCard = string.Empty;
        private static string planDialogYes = string.Empty;
        private static string planDialogNo = string.Empty;

        private readonly IStringLocalizer<PlanDialog> _localizer;
        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly IOptions<BotConfig> botConfig;

        public PlanDialog(IOptions<BotConfig> config, IBotTelemetryClient telemetryClient)
            : base(nameof(PlanDialog))
        {
            ResourceManager rm = new ResourceManager("PlanB.Butler.Bot.Dictionary.Dialogs", Assembly.GetExecutingAssembly());
            planDialogMenuCardPrompt = rm.GetString("PlanDialog_MenuCardPrompt");
            planDialogMenuCard = rm.GetString("PlanDialog_MenuCard");
            planDialogOtherMenuCard = rm.GetString("PlanDialog_OtherMenuCard");
            planDialogNoMenuCard = rm.GetString("PlanDialog_NoMenuCard");
            planDialogYes = rm.GetString("yes");
            planDialogNo = rm.GetString("no");

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
                Prompt = MessageFactory.Text(planDialogMenuCardPrompt),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Bieg", "Delphi", "Leib und Seele", "Liederhalle", "Feasy", "La Boussola" }),
                Style = ListStyle.HeroCard,
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> SendPictureStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["restaurant"] = ((FoundChoice)stepContext.Result).Value;
            string restaurant = stepContext.Values["restaurant"].ToString();

            var planDialogNoMenuCard1 = MessageFactory.Text(string.Format(planDialogNoMenuCard, restaurant));
            var planDialogMenuCard1 = MessageFactory.Text(string.Format(planDialogMenuCard, restaurant));

            var picture = BotMethods.GetDocument("pictures", restaurant.Replace(' ', '_') + ".txt", this.botConfig.Value.StorageAccountUrl, this.botConfig.Value.StorageAccountKey);
            if (!picture.Contains("BlobNotFound"))
            {
                
                await stepContext.Context.SendActivityAsync(planDialogMenuCard1, cancellationToken);
                await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(new Attachment("image/png", picture)), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(planDialogNoMenuCard1, cancellationToken);
            }

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text(planDialogOtherMenuCard),
                Choices = ChoiceFactory.ToChoices(new List<string> { planDialogYes, planDialogNo }),
                Style = ListStyle.HeroCard,
            });
        }

        private static async Task<DialogTurnResult> ReturnStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Choise"] = ((FoundChoice)stepContext.Result).Value;
            if (stepContext.Values["Choise"].ToString().ToLower() == "ja")
            {
                await stepContext.EndDialogAsync();
                return await stepContext.BeginDialogAsync(nameof(PlanDialog), null, cancellationToken);
            }
            else
            {
                await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(OverviewDialog), null, cancellationToken);
            }
        }
    }
}

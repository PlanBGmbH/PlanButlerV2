using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PlanB.Butler.Bot.Models;

namespace PlanB.Butler.Bot.Services
{
    /// <summary>
    /// MealService.
    /// </summary>
    /// <seealso cref="PlanB.Butler.Bot.Services.IMealService" />
    public class MealService : IMealService
    {
        /// <summary>
        /// The HTTP client.
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// The configuration.
        /// </summary>
        private readonly BotConfig config;

        /// <summary>
        /// Initializes a new instance of the <see cref="MealService" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="botConfig">The bot configuration.</param>
        public MealService(HttpClient httpClient, BotConfig botConfig)
        {
            this.httpClient = httpClient;
            this.config = botConfig;
        }

        /// <summary>
        /// Gets the meals.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns>
        /// Meals.
        /// </returns>
        public async Task<List<MealModel>> GetMeals(string startDate, string endDate)
        {
            var uri = this.config.ButlerServiceUrl;
            this.httpClient.DefaultRequestHeaders.Add(Constants.FunctionsKeyHeader, "8NL2rP9nV8agFOGWmwTrlpcrEsIyr7rJINX3qpbZb4WEfyWgzTWH0Q==");
            var responseString = this.httpClient.GetStringAsync(uri).Result;

            var meals = JsonConvert.DeserializeObject<List<MealModel>>(responseString);
            return meals;
        }
    }

    /// <summary>
    /// IMealService.
    /// </summary>
    public interface IMealService
    {
        /// <summary>
        /// Gets the meals.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns>List of Meals.</returns>
        Task<List<MealModel>> GetMeals(string startDate, string endDate);
    }
}

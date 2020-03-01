// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PlanB.Butler.Admin.Contracts;
using PlanB.Butler.Admin.Models;

namespace PlanB.Butler.Admin.Services
{
    /// <summary>
    /// MealService.
    /// </summary>
    /// <seealso cref="PlanB.Butler.Admin.Contracts.IMealService" />
    public class MealService : IMealService
    {
        /// <summary>
        /// The HTTP client.
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// The configuration.
        /// </summary>
        private readonly IConfiguration config;

        /// <summary>
        /// Initializes a new instance of the <see cref="MealService" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="configuration">The configuration.</param>
        public MealService(HttpClient httpClient, IConfiguration configuration)
        {
            this.httpClient = httpClient;
            this.config = configuration;
        }

        /// <summary>
        /// Creates the meal.
        /// </summary>
        /// <param name="meal">The meal.</param>
        /// <returns>
        /// True or false.
        /// </returns>
        public async Task<bool> CreateMeal(MealViewModel meal)
        {
            Guid correlationId = Guid.NewGuid();
            meal.CorrelationId = correlationId;
            var json = JsonConvert.SerializeObject(meal);
            StringContent content = Util.CreateStringContent(json, correlationId, null);
            var uri = this.config["MealsUri"];

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = content,
            };
            httpRequestMessage.Headers.Clear();
            Util.AddDefaultEsbHeaders(httpRequestMessage, correlationId, this.config["FunctionsKey"]);
            var result = await this.httpClient.SendAsync(httpRequestMessage);
            result.EnsureSuccessStatusCode();
            var success = result.IsSuccessStatusCode;
            return success;
        }

        /// <summary>
        /// Gets the meal.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        /// Meal by Id.
        /// </returns>
        public async Task<MealViewModel> GetMeal(string id)
        {
            Guid correlationId = Guid.NewGuid();
            var uri = this.config["MealsUri"].TrimEnd('/') + "/" + id;
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            httpRequestMessage.Headers.Clear();
            Util.AddDefaultEsbHeaders(httpRequestMessage, correlationId, this.config["FunctionsKey"]);
            var result = await this.httpClient.SendAsync(httpRequestMessage);
            result.EnsureSuccessStatusCode();

            var body = result.Content.ReadAsStringAsync().Result;

            var meal = JsonConvert.DeserializeObject<MealViewModel>(body);

            return meal;
        }

        /// <summary>
        /// Gets the meals.
        /// </summary>
        /// <returns>
        /// Meals.
        /// </returns>
        public async Task<List<MealViewModel>> GetMeals()
        {
            var uri = this.config["MealsUri"];
            this.httpClient.DefaultRequestHeaders.Add(Constants.FunctionsKeyHeader, this.config["FunctionsKey"]);
            var responseString = await this.httpClient.GetStringAsync(uri);

            var meals = JsonConvert.DeserializeObject<List<MealViewModel>>(responseString);
            return meals;
        }
    }
}

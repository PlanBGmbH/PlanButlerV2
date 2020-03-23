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
    /// RestaurantService.
    /// </summary>
    /// <seealso cref="PlanB.Butler.Admin.Contracts.IRestaurantService" />
    public class RestaurantService : IRestaurantService
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
        /// Initializes a new instance of the <see cref="RestaurantService" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="configuration">The configuration.</param>
        public RestaurantService(HttpClient httpClient, IConfiguration configuration)
        {
            this.httpClient = httpClient;
            this.config = configuration;
        }

        /// <summary>
        /// Creates the meal.
        /// </summary>
        /// <param name="restaurant">The restaurant.</param>
        /// <returns>
        /// True or false.
        /// </returns>
        public async Task<bool> CreateRestaurant(RestaurantViewModel restaurant)
        {
            Guid correlationId = Guid.NewGuid();
            restaurant.CorrelationId = correlationId;
            var json = JsonConvert.SerializeObject(restaurant);
            StringContent content = Util.CreateStringContent(json, correlationId, null);
            var uri = this.config["RestaurantUri"];

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
        /// Gets the restaurant.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        /// Restaurant by Id.
        /// </returns>
        public async Task<RestaurantViewModel> GetRestaurant(string id)
        {
            Guid correlationId = Guid.NewGuid();
            var uri = this.config["RestaurantUri"].TrimEnd('/') + "/" + id;
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            httpRequestMessage.Headers.Clear();
            Util.AddDefaultEsbHeaders(httpRequestMessage, correlationId, this.config["FunctionsKey"]);
            var result = await this.httpClient.SendAsync(httpRequestMessage);
            result.EnsureSuccessStatusCode();

            var body = result.Content.ReadAsStringAsync().Result;

            var restaurant = JsonConvert.DeserializeObject<RestaurantViewModel>(body);
            return restaurant;
        }

        /// <summary>
        /// Gets the restaurant.
        /// </summary>
        /// <returns>
        /// Restaurant.
        /// </returns>
        public async Task<List<RestaurantViewModel>> GetRestaurant()
        {
            var uri = this.config["RestaurantUri"];
            this.httpClient.DefaultRequestHeaders.Add(Constants.FunctionsKeyHeader, this.config["FunctionsKey"]);
            var responseString = await this.httpClient.GetStringAsync(uri);

            var restaurant = JsonConvert.DeserializeObject<List<RestaurantViewModel>>(responseString);
            return restaurant;
        }

        /// <summary>
        /// Updates the restaurant.
        /// </summary>
        /// <param name="restaurant">The restaurant.</param>
        /// <returns>
        /// Restaurant.
        /// </returns>
        public async Task<RestaurantViewModel> UpdateRestaurant(RestaurantViewModel restaurant)
        {
            Guid correlationId = Guid.NewGuid();
            restaurant.CorrelationId = correlationId;
            var json = JsonConvert.SerializeObject(restaurant);
            StringContent content = Util.CreateStringContent(json, correlationId, null);
            var uri = this.config["RestaurantUri"].TrimEnd('/') + "/" + restaurant.Id;

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, uri)
            {
                Content = content,
            };
            httpRequestMessage.Headers.Clear();
            Util.AddDefaultEsbHeaders(httpRequestMessage, correlationId, this.config["FunctionsKey"]);
            var result = await this.httpClient.SendAsync(httpRequestMessage);
            var responseString = await result.Content.ReadAsStringAsync();
            result.EnsureSuccessStatusCode();
            var updatedRestaurant = JsonConvert.DeserializeObject<RestaurantViewModel>(responseString);
            return updatedRestaurant;
        }
    }
}

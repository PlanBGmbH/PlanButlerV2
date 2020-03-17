// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

using PlanB.Butler.Admin.Models;

namespace PlanB.Butler.Admin.Contracts
{
    /// <summary>
    /// IRestaurantService.
    /// </summary>
    public interface IRestaurantService
    {
        /// <summary>
        /// Gets the restaurant.
        /// </summary>
        /// <returns>Restaurant.</returns>
        Task<List<RestaurantViewModel>> GetRestaurant();

        /// <summary>
        /// Creates the meal.
        /// </summary>
        /// <param name="restaurant">The meal.</param>
        /// <returns>True or false.</returns>
        Task<bool> CreateRestaurant(RestaurantViewModel restaurant);

        /// <summary>
        /// Updates the restaurant.
        /// </summary>
        /// <param name="restaurant">The restuarant.</param>
        /// <returns>Restaurant.</returns>
        Task<RestaurantViewModel> UpdateRestaurant(RestaurantViewModel restaurant);

        /// <summary>
        /// Gets the restaurant.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Restaurant by Id.</returns>
        Task<RestaurantViewModel> GetRestaurant(string id);
    }
}

// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using PlanB.Butler.Admin.Contracts;

namespace PlanB.Butler.Admin.Controllers
{
    /// <summary>
    /// RestaurantController.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    public class RestaurantController : Controller
    {
        /// GET: /<controller>
        private IRestaurantService restaurantService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestaurantController"/> class.
        /// </summary>
        /// <param name="restserv">The restaurant SVC.</param>
        public RestaurantController(IRestaurantService restserv) => this.restaurantService = restserv;

        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>Index.</returns>
        public IActionResult Index()
        {
            var restaurant = this.restaurantService.GetRestaurant().Result;
            return this.View(restaurant);
        }

        /// <summary>
        /// Creates this instance.
        /// </summary>
        /// <returns>IActionResult.</returns>
        public IActionResult Create()
        {
            return this.View();
        }

        /// <summary>
        /// Creates the specified meal.
        /// </summary>
        /// <param name="restaurant">The meal.</param>
        /// <returns>Meal.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CorrelationId,Date,Price,Name,Restaurant")] Models.RestaurantViewModel restaurant)
        {
            if (this.ModelState.IsValid)
            {
                var result = await this.restaurantService.CreateRestaurant(restaurant);
                return this.RedirectToAction("Index");
            }

            return this.View(restaurant);
        }

        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="restaurant">The meal.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,CorrelationId,Date,Price,Name,Restaurant")] Models.RestaurantViewModel restaurant)
        {
            if (id != restaurant.Id)
            {
                return this.NotFound();
            }

            if (this.ModelState.IsValid)
            {
                var result = await this.restaurantService.UpdateRestaurant(restaurant);
            }

            return this.View(restaurant);
        }

        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Meal.</returns>
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return this.NotFound();
            }

            var restaurant = await this.restaurantService.GetRestaurant(id);

            if (restaurant == null)
            {
                return this.NotFound();
            }

            return this.View(restaurant);
        }

    }
}

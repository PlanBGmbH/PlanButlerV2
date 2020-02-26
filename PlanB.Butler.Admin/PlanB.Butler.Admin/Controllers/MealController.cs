﻿// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PlanB.Butler.Admin.Contracts;
using PlanB.Butler.Admin.Models;

namespace PlanB.Butler.Admin.Controllers
{
    /// <summary>
    /// MealController.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    public class MealController : Controller
    {
        /// <summary>
        /// The meal service.
        /// </summary>
        private readonly IMealService mealService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MealController"/> class.
        /// </summary>
        /// <param name="mealSvc">The meal SVC.</param>
        public MealController(IMealService mealSvc) => this.mealService = mealSvc;

        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>Meals.</returns>
        public IActionResult Index()
        {
            var meals = this.mealService.GetMeals().Result;
            return this.View(meals);
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
        /// <param name="meal">The meal.</param>
        /// <returns>Meal.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CorrelationId,Date,Price,Name,Restaurant")] MealViewModel meal)
        {
            if (this.ModelState.IsValid)
            {
                var result = await this.mealService.CreateMeal(meal);
            }

            return this.View(meal);
        }
    }
}

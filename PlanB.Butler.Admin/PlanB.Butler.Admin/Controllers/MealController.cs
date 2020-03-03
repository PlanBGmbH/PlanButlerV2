// Copyright (c) PlanB. GmbH. All Rights Reserved.
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
                return this.RedirectToAction("Index");
            }

            return this.View(meal);
        }

        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="meal">The meal.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,CorrelationId,Date,Price,Name,Restaurant")] MealViewModel meal)
        {
            if (id != meal.Id)
            {
                return this.NotFound();
            }

            if (this.ModelState.IsValid)
            {
                var result = await this.mealService.UpdateMeal(meal);
                return this.RedirectToAction(nameof(this.Index));
            }

            return this.View(meal);
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

            var meal = await this.mealService.GetMeal(id);

            if (meal == null)
            {
                return this.NotFound();
            }

            return this.View(meal);
        }

        /// <summary>
        /// Deletes the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return this.NotFound();
            }

            var meal = await this.mealService.GetMeal(id);

            if (meal == null)
            {
                return this.NotFound();
            }

            return this.View(meal);
        }

        /// <summary>
        /// Deletes the confirmed.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await this.mealService.DeleteMeal(id);

            return this.RedirectToAction(nameof(this.Index));
        }
    }
}

// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;

namespace PlanB.Butler.Services.Models
{
    /// <summary>
    /// OrderModel.
    /// </summary>
    public class OrderModel
    {
        /// <summary>
        /// Gets or sets the Relationship.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public OrderRelationship Relationship { get; set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>
        /// The date.
        /// </value>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the company.
        /// </summary>
        /// <value>
        /// The name of the company.
        /// </value>
        public string CompanyName { get; set; }

        /// <summary>
        /// Gets or sets the restaurant.
        /// </summary>
        /// <value>
        /// The restaurant.
        /// </value>
        public string Restaurant { get; set; }

        /// <summary>
        /// Gets or sets the meal.
        /// </summary>
        /// <value>
        /// The meal.
        /// </value>
        public string Meal { get; set; }

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        /// <value>
        /// The price.
        /// </value>
        public double Price { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the benefit.
        /// </summary>
        /// <value>
        /// The benefit.
        /// </value>
        public double Benefit { get; set; }
    }
}

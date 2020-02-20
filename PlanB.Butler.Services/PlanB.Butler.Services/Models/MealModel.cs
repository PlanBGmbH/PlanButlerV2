// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Globalization;

namespace PlanB.Butler.Services.Models
{
    /// <summary>
    /// MealModel.
    /// </summary>
    public class MealModel
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id
        {
            get
            {
                var date = this.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var id = $"{date}-{this.Restaurant}.json";
                return id;
            }
        }

        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        public Guid? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>
        /// The date.
        /// </value>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        /// <value>
        /// The price.
        /// </value>
        public double Price { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the restaurant.
        /// </summary>
        /// <value>
        /// The restaurant.
        /// </value>
        public string Restaurant { get; set; }
    }
}

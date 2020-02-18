// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace PlanB.Butler.Admin.Models
{
    /// <summary>
    /// PlanModel.
    /// </summary>
    public class PlanModel
    {
        // TODO: Refactor to support n restaurants.

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the restaurant1.
        /// </summary>
        /// <value>
        /// The restaurant1.
        /// </value>
        public string Restaurant1 { get; set; }

        /// <summary>
        /// Gets or sets the meal1.
        /// </summary>
        /// <value>
        /// The meal1.
        /// </value>
        public List<FoodModel> Meal1 { get; set; }

        /// <summary>
        /// Gets or sets the restaurant2.
        /// </summary>
        /// <value>
        /// The restaurant2.
        /// </value>
        public string Restaurant2 { get; set; }

        /// <summary>
        /// Gets or sets the meal2.
        /// </summary>
        /// <value>
        /// The meal2.
        /// </value>
        public List<FoodModel> Meal2 { get; set; }
    }
}

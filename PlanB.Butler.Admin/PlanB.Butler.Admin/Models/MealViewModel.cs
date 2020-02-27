// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace PlanB.Butler.Admin.Models
{
    /// <summary>
    /// MealViewModel.
    /// </summary>
    public class MealViewModel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [Key]
        [JsonProperty("id")]
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        [JsonProperty("correlationid")]
        public Guid? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>
        /// The date.
        /// </value>
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
        [DataType(DataType.Date)]
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        /// <value>
        /// The price.
        /// </value>
        [DisplayFormat(DataFormatString = "{0:c}")]
        [JsonProperty("price")]
        public double Price { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the restaurant.
        /// </summary>
        /// <value>
        /// The restaurant.
        /// </value>
        [JsonProperty("restaurant")]
        public string Restaurant { get; set; }
    }
}

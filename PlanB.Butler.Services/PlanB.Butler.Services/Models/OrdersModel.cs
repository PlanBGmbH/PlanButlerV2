// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace PlanB.Butler.Services.Models
{
    /// <summary>
    /// OrdersModel.
    /// </summary>
    public class OrdersModel
    {
        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>
        /// The date.
        /// </value>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the name of the login.
        /// </summary>
        /// <example>john.doe@domain.com.</example>
        /// <value>
        /// The name of the login.
        /// </value>
        public string LoginName { get; set; }

        /// <summary>
        /// Gets or sets the orders.
        /// </summary>
        /// <value>
        /// The orders.
        /// </value>
        public List<OrderModel> Orders { get; set; }
    }
}

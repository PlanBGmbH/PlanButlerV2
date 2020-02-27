// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace PlanB.Butler.Admin.Controllers
{
    /// <summary>
    /// RestaurantController.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    public class RestaurantController : Controller
    {
        // GET: /<controller>/

        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>Index.</returns>
        public IActionResult Index()
        {
            return this.View();
        }
    }
}

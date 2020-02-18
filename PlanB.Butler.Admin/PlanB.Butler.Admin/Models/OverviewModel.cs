// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace PlanB.Butler.Admin.Models
{
    /// <summary>
    /// OverviewModel.
    /// </summary>
    public class OverviewModel
    {
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the plan day.
        /// </summary>
        /// <value>
        /// The plan day.
        /// </value>
        public List<PlanModel> PlanDay { get; set; }
    }
}

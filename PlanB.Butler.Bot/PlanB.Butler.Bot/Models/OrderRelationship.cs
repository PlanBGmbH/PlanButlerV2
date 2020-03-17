// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlanB.Butler.Bot.Models
{
    /// <summary>
    /// OrderRelationship.
    /// </summary>
    public enum OrderRelationship
    {
        /// <summary>
        /// External.
        /// </summary>
        External = 1,

        /// <summary>
        /// Internal.
        /// </summary>
        Internal = 2,

        /// <summary>y
        /// Client.
        /// </summary>
        Client = 3,

        /// <summary>
        /// Intership.
        /// </summary>
        Intership = 4,
    }
}

// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Linq;

using Microsoft.AspNetCore.Http;

namespace PlanB.Butler.Services
{
    /// <summary>
    /// Util.
    /// </summary>
    internal static class Util
    {
        /// <summary>
        /// Reads the correlation identifier.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <returns>Correlation Id.</returns>
        internal static Guid ReadCorrelationId(IHeaderDictionary headers)
        {
            Guid correlationId = Guid.NewGuid();

            if (headers != null && headers.TryGetValue(Constants.ButlerCorrelationTraceName, out var headerValues))
            {
                if (Guid.TryParse(headerValues.FirstOrDefault(), out correlationId))
                {
                    correlationId = new Guid(headerValues.FirstOrDefault());
                }
            }

            return correlationId;
        }
    }
}

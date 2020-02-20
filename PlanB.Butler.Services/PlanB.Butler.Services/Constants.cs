// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PlanB.Butler.Services.Test")]

namespace PlanB.Butler.Services
{
    /// <summary>
    /// Constants.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// The butler correlation trace name.
        /// </summary>
        internal const string ButlerCorrelationTraceName = "Butler-Correlation-Id";

        /// <summary>
        /// The butler correlation trace header.
        /// </summary>
        internal const string ButlerCorrelationTraceHeader = "ButlerCorrelationId";
    }
}

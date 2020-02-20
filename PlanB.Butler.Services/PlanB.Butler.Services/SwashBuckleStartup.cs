// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Reflection;

using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using PlanB.Butler.Services;

[assembly: WebJobsStartup(typeof(SwashBuckleStartup))]

namespace PlanB.Butler.Services
{
    /// <summary>
    /// SwashBuckleStartup.
    /// </summary>
    /// <seealso cref="Microsoft.Azure.WebJobs.Hosting.IWebJobsStartup" />
    internal class SwashBuckleStartup : IWebJobsStartup
    {
        /// <summary>
        /// Configures the specified builder.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddSwashBuckle(Assembly.GetExecutingAssembly());
        }
    }
}

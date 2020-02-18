// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace PlanB.Butler.Admin
{
    /// <summary>
    /// AdminConfig.
    /// </summary>
    public class AdminConfig
    {
        /// <summary>
        /// Gets or sets the storage account URL.
        /// </summary>
        /// <value>
        /// The storage account URL.
        /// </value>
        public string StorageAccountUrl { get; set; }

        /// <summary>
        /// Gets or sets the storage account key.
        /// </summary>
        /// <value>
        /// The storage account key.
        /// </value>
        public string StorageAccountKey { get; set; }

        /// <summary>
        /// Gets or sets the service bus connection string.
        /// </summary>
        /// <value>
        /// The service bus connection string.
        /// </value>
        public string ServiceBusConnectionString { get; set; }
    }
}

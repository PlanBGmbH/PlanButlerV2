// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace PlanB.Butler.Bot
{
    /// <summary>
    /// BotConfig.
    /// </summary>
    public class BotConfig
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
        /// Gets or sets the storage account connection string.
        /// </summary>
        /// <value>
        /// The storage account connection string.
        /// </value>
        public string StorageAccountConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the service bus connection string.
        /// </summary>
        /// <value>
        /// The service bus connection string.
        /// </value>
        public string ServiceBusConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the get daily overview function.
        /// </summary>
        /// <value>
        /// The get daily overview function.
        /// </value>
        public string GetDailyOverviewFunc { get; set; }

        /// <summary>
        /// Gets or sets the get salary deduction.
        /// </summary>
        /// <value>
        /// The get salary deduction.
        /// </value>
        public string GetSalaryDeduction { get; set; }

        /// <summary>
        /// Gets or sets the get daily user overview function.
        /// </summary>
        /// <value>
        /// The get daily user overview function.
        /// </value>
        public string GetDailyUserOverviewFunc { get; set; }
    }
}

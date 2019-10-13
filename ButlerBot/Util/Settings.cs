using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ButlerBot.Util
{
    /// <summary>
    /// Class that provides the Environment settings that are set in the Azure App Services Configuration.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Gets the Storage Account Name of our application.
        /// </summary>
        /// <value>
        /// The URL of the Storage Account.
        /// </value>
        public static string StorageAccountUrl => Environment.GetEnvironmentVariable("butlerStorageAccountUrl");

        /// <summary>
        /// Gets the Storage Account Key of our application.
        /// </summary>
        /// <value>
        /// The Storage Account Key.
        /// </value>
        public static string StorageAccountKey => Environment.GetEnvironmentVariable("storageAccountKey");

        /// <summary>
        /// Gets the Storage Account Name of our application.
        /// </summary>
        /// <value>
        /// The Service Bus Connection String.
        /// </value>
        public static string serviceBusConnectionString => Environment.GetEnvironmentVariable("serviceBusConnectionString");
    }
}

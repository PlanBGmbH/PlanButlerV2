// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

namespace PlanB.Butler.Services.Test
{
    /// <summary>
    /// FunctionTestLogger.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Logging.ILogger" />
    internal class FunctionTestLogger : ILogger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionTestLogger"/> class.
        /// </summary>
        internal FunctionTestLogger()
        {
            this.Logs = new List<string>();
            this.Exceptions = new List<Exception>();
            this.Events = new List<EventId>();
        }

        /// <summary>
        /// Gets or sets the logs.
        /// </summary>
        /// <value>
        /// The logs.
        /// </value>
        internal List<string> Logs { get; set; }

        /// <summary>
        /// Gets or sets the exceptions.
        /// </summary>
        /// <value>
        /// The exceptions.
        /// </value>
        internal List<Exception> Exceptions { get; set; }

        /// <summary>
        /// Gets or sets events.
        /// </summary>
        internal List<EventId> Events { get; set; }

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <typeparam name="TState">The TState.</typeparam>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>
        /// An IDisposable that ends the logical operation scope on dispose.
        /// </returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return Microsoft.Extensions.Logging.Abstractions.Internal.NullScope.Instance;
        }

        /// <summary>
        /// Checks if the given <paramref name="logLevel" /> is enabled.
        /// </summary>
        /// <param name="logLevel">level to be checked.</param>
        /// <returns>
        ///   <c>true</c> if enabled.
        /// </returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <typeparam name="TState">The TState.</typeparam>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a <c>string</c> message of the <paramref name="state" /> and <paramref name="exception" />.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string message = formatter(state, exception);

            if (eventId != null)
            {
                this.Events.Add(eventId);
            }

            if (logLevel == LogLevel.Error)
            {
                this.Exceptions.Add(new Exception(message));
            }
            else
            {
                this.Logs.Add(message);
            }
        }
    }
}

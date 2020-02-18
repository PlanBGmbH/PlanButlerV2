// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

namespace PlanB.Butler.Services.Extensions
{
    /// <summary>
    /// The ILogger Extensions.
    /// </summary>
    public static class LoggerExtension
    {
        /// <summary>
        /// Informations the specified event identifier.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="butlerCorrelationId">The Butler correlation identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="trace">The trace.</param>
        public static void LogInformation(this ILogger log, Guid butlerCorrelationId, string message, IDictionary<string, string> trace)
        {
            if (log == null)
            {
                return;
            }

            EventId eventId = new EventId(butlerCorrelationId.GetHashCode(), Constants.ButlerCorrelationTraceName);

            var state = new Dictionary<string, object>
            {
                { Constants.ButlerCorrelationTraceName, butlerCorrelationId },
                { "Message", message },
            };

            var rator = trace.GetEnumerator();
            while (rator.MoveNext())
            {
                if (!state.ContainsKey(rator.Current.Key))
                {
                    state.Add(rator.Current.Key, rator.Current.Value);
                }
            }

            log.Log(LogLevel.Information, eventId, state, null, Formatter);
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="trace">The trace.</param>
        /// <param name="ex">The exeception.</param>
        public static void LogError(this ILogger log, Guid correlationId, string message, IDictionary<string, string> trace, Exception ex = null)
        {
            if (log == null)
            {
                return;
            }

            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);

            var state = new Dictionary<string, object>
            {
                { Constants.ButlerCorrelationTraceName, correlationId },
                { "Message", message },
            };

            var rator = trace.GetEnumerator();
            while (rator.MoveNext())
            {
                if (!state.ContainsKey(rator.Current.Key))
                {
                    state.Add(rator.Current.Key, rator.Current.Value);
                }
            }

            if (ex == null)
            {
                ex = new Exception(message);
            }

            log.Log(LogLevel.Error, eventId, state, ex, Formatter);
        }

        /// <summary>
        /// Style of logging.
        /// </summary>
        /// <typeparam name="T">State.</typeparam>
        /// <param name="state">State Param.</param>
        /// <param name="ex">Exception.</param>
        /// <returns>Message.</returns>
        internal static string Formatter<T>(T state, Exception ex)
        {
            if (ex != null)
            {
                return ex.ToString();
            }

            Dictionary<string, object> stateDictionary = state as Dictionary<string, object>;
            if (stateDictionary != null && stateDictionary.TryGetValue("Message", out var message))
            {
                return message?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}

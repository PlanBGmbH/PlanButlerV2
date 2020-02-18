// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace PlanB.Butler.Services.Extensions
{
    /// <summary>
    /// NewtonsoftExtension.
    /// </summary>
    public static class NewtonsoftExtension
    {
        /// <summary>
        /// Sanitizes a JObject.
        /// </summary>
        /// <param name="obj">JObject.</param>
        /// <param name="removeMetaTags">Indicates whether 'id' and 'lastUpdate' fields should be removed.</param>
        public static void Sanitize(this JObject obj, bool removeMetaTags = false)
        {
            List<JToken> metaData = new List<JToken>();
            foreach (JToken token in obj.Descendants())
            {
                try
                {
                    JProperty propertyToken = (JProperty)token;
                    if (propertyToken.Name.StartsWith("_"))
                    {
                        metaData.Add(token);
                    }

                    if (removeMetaTags)
                    {
                        if (propertyToken.Name.Equals("id") || propertyToken.Name.Equals("lastUpdate"))
                        {
                            metaData.Add(token);
                        }
                    }
                }

                // Some Properties may not get Serialized, just skip them
                catch
                {
                }
            }

            metaData.ForEach(n => n.Remove());
        }

        /// <summary>
        /// Sanitizes a JArray.
        /// </summary>
        /// <param name="obj">JArray.</param>
        /// <param name="removeDocumentMetaData">Indicates whether 'id' and 'lastUpdate' should be removed.</param>
        public static void Sanitize(this JArray obj, bool removeDocumentMetaData = false)
        {
            List<JToken> metaData = new List<JToken>();
            foreach (JToken token in obj.Descendants())
            {
                try
                {
                    JProperty propertyToken = (JProperty)token;
                    if (propertyToken.Name.StartsWith("_"))
                    {
                        metaData.Add(token);
                    }

                    if (removeDocumentMetaData)
                    {
                        if (propertyToken.Name.Equals("id") || propertyToken.Name.Equals("lastUpdate"))
                        {
                            metaData.Add(token);
                        }
                    }
                }

                // Some Properties may not get Serialized, just skip them
                catch
                {
                }
            }

            metaData.ForEach(n => n.Remove());
        }
    }
}

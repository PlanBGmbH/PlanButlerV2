// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Newtonsoft.Json;
using PlanB.Butler.Services.Models;

namespace PlanB.Butler.Services.Test
{
    /// <summary>
    /// MealServiceTest.
    /// </summary>
    [TestClass]
    public class MealServiceMockTest
    {
        /// <summary>
        /// The context.
        /// </summary>
        private Microsoft.Azure.WebJobs.ExecutionContext context;

        /// <summary>
        /// The log.
        /// </summary>
        private FunctionTestLogger log;

        /// <summary>
        /// The mock BLOB container.
        /// </summary>
        private Mock<CloudBlobContainer> mockBlobContainer;

        /// <summary>
        /// The correlation identifier.
        /// </summary>
        private Guid correlationId;

        /// <summary>
        /// The message header.
        /// </summary>
        private Message messageHeader;

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        [TestInitialize]
        public void Init()
        {
            this.correlationId = Guid.NewGuid();
            this.messageHeader = new Message() { CorrelationId = this.correlationId.ToString() };
            this.context = new Microsoft.Azure.WebJobs.ExecutionContext() { FunctionName = nameof(MealService) };
            this.log = new FunctionTestLogger();

            var mockBlobUri = new Uri("http://localhost/container");
            this.mockBlobContainer = new Mock<CloudBlobContainer>(MockBehavior.Loose, mockBlobUri);
            Mock<CloudBlockBlob> blobMock = new Mock<CloudBlockBlob>(new Uri("http://localhost/blob"));
            blobMock.Setup(n => n.UploadTextAsync(It.IsAny<string>())).Returns(Task.FromResult(true));
            this.mockBlobContainer.Setup(n => n.GetBlockBlobReference(It.IsAny<string>())).Returns(blobMock.Object);
        }

        /// <summary>
        /// Creates the meal test.
        /// </summary>
        [TestMethod]
        public void CreateMealTest()
        {
            MealModel mealModel = new MealModel()
            {
                CorrelationId = Guid.NewGuid(),
                Date = DateTime.Now,
                Name = "Kässpätzle",
                Price = 2.3,
                Restaurant = "Gasthof Adler " + DateTime.Now.Ticks,
            };

            // Setup Mock
            var httpRequest = CreateMockRequest(mealModel);
            var result = MealService.CreateMeal(httpRequest.Object, this.mockBlobContainer.Object, this.log, this.context).Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(typeof(OkObjectResult), result.GetType());
        }

        /// <summary>
        /// Creates the meal test.
        /// </summary>
        [TestMethod]
        public void CreateMealNoNameTest()
        {
            MealModel mealModel = new MealModel()
            {
                CorrelationId = Guid.NewGuid(),
                Date = DateTime.Now,
                Price = 2.3,
                Restaurant = "Gasthof Adler",
            };

            // Setup Mock
            var httpRequest = CreateMockRequest(mealModel);
            var result = MealService.CreateMeal(httpRequest.Object, this.mockBlobContainer.Object, this.log, this.context).Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(typeof(BadRequestObjectResult), result.GetType());
        }

        /// <summary>
        /// Creates the mock request.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <returns>HttpRequest.</returns>
        private static Mock<HttpRequest> CreateMockRequest(object body)
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);

            var json = JsonConvert.SerializeObject(body);

            sw.Write(json);
            sw.Flush();

            ms.Position = 0;
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaderDictionary = new Mock<HeaderDictionary>();

            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockResponse.Setup(c => c.Headers).Returns(mockHeaderDictionary.Object);

            var mockRequest = new Mock<HttpRequest>();

            // mockRequest.Setup(req => req.Query).Returns(new QueryCollection(query));
            Dictionary<string, StringValues> header = new Dictionary<string, StringValues>();
            mockRequest.Setup(req => req.Headers).Returns(new HeaderDictionary(header));
            mockRequest.SetupGet(req => req.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(x => x.Body).Returns(ms);

            return mockRequest;
        }
    }
}

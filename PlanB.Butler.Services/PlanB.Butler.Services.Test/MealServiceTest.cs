// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

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
    public class MealServiceTest
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
        /// Alls the dates empty test ok.
        /// </summary>
        [TestMethod]
        public void AllDatesEmptyTestOk()
        {
            var result = MealService.CreateBlobPrefix(string.Empty, string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        /// <summary>
        /// Starts the date empty test ok.
        /// </summary>
        [TestMethod]
        public void StartDateEmptyTestOK()
        {
            var result = MealService.CreateBlobPrefix(string.Empty, "2020-02-25");
            Assert.AreEqual(string.Empty, result);
        }

        /// <summary>
        /// Ends the date empty test ok.
        /// </summary>
        [TestMethod]
        public void EndDateEmptyTestOK()
        {
            var result = MealService.CreateBlobPrefix("2020-02-25", string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        /// <summary>
        /// Matches the day test ok.
        /// </summary>
        [TestMethod]
        public void MatchDayTestOK()
        {
            var result = MealService.CreateBlobPrefix("2020-02-25", "2020-02-25");
            Assert.AreEqual("2020-02-25", result);
        }

        /// <summary>
        /// Matches the month test ok.
        /// </summary>
        [TestMethod]
        public void MatchMonthTestOK()
        {
            var result = MealService.CreateBlobPrefix("2020-02-25", "2020-02-04");
            Assert.AreEqual("2020-02-", result);
        }

        /// <summary>
        /// Matches the year test ok.
        /// </summary>
        [TestMethod]
        public void MatchYearTestOK()
        {
            var result = MealService.CreateBlobPrefix("2020-02-25", "2020-03-04");
            Assert.AreEqual("2020-0", result);
        }

        /// <summary>
        /// Lengthes the fail test.
        /// </summary>
        [TestMethod]
        public void LengthFailTest()
        {
            var result = MealService.CreateBlobPrefix("2020-02-25", "2020-03-0");
            Assert.AreEqual(string.Empty, result);
        }

        /// <summary>
        /// Determines whether date is in range ok.
        /// </summary>
        [TestMethod]
        public void IsDateInRangeOK()
        {
            var start = "2020-02-01";
            var end = "2020-02-12";
            var toCheck = "2020-02-10";

            var startDate = DateTime.Parse(start);
            var endDate = DateTime.Parse(end);
            var toCheckDate = DateTime.Parse(toCheck);

            var result = MealService.IsDateInRange(startDate, endDate, toCheckDate);
            Assert.AreEqual(true, result);
        }

        /// <summary>
        /// Determines whether date is too early.
        /// </summary>
        [TestMethod]
        public void IsDateInRangeCheckDateEarlierOK()
        {
            var start = "2020-02-01";
            var end = "2020-02-12";
            var toCheck = "2020-01-10";

            var startDate = DateTime.Parse(start);
            var endDate = DateTime.Parse(end);
            var toCheckDate = DateTime.Parse(toCheck);

            var result = MealService.IsDateInRange(startDate, endDate, toCheckDate);
            Assert.AreEqual(false, result);
        }

        /// <summary>
        /// Determines whether date is too late.
        /// </summary>
        [TestMethod]
        public void IsDateInRangeCheckDateLaterOK()
        {
            var start = "2020-02-01";
            var end = "2020-02-12";
            var toCheck = "2021-01-10";

            var startDate = DateTime.Parse(start);
            var endDate = DateTime.Parse(end);
            var toCheckDate = DateTime.Parse(toCheck);

            var result = MealService.IsDateInRange(startDate, endDate, toCheckDate);
            Assert.AreEqual(false, result);
        }

        /// <summary>
        /// Determines whether all dates are equal.
        /// </summary>
        [TestMethod]
        public void IsDateInRangeCheckDateAllEqual()
        {
            var start = "2020-02-01";
            var end = "2020-02-01";
            var toCheck = "2020-02-01";

            var startDate = DateTime.Parse(start);
            var endDate = DateTime.Parse(end);
            var toCheckDate = DateTime.Parse(toCheck);

            var result = MealService.IsDateInRange(startDate, endDate, toCheckDate);
            Assert.AreEqual(true, result);
        }

        /// <summary>
        /// Determines whether Startdate is equal to CheckDate.
        /// </summary>
        [TestMethod]
        public void IsDateInRangeCheckStartEqualCheck()
        {
            var start = "2020-02-01";
            var end = "2020-12-01";
            var toCheck = "2020-02-01";

            var startDate = DateTime.Parse(start);
            var endDate = DateTime.Parse(end);
            var toCheckDate = DateTime.Parse(toCheck);

            var result = MealService.IsDateInRange(startDate, endDate, toCheckDate);
            Assert.AreEqual(true, result);
        }

        /// <summary>
        /// Determines whether Enddate is equal to CheckDate.
        /// </summary>
        [TestMethod]
        public void IsDateInRangeCheckEndEqualCheck()
        {
            var start = "2020-01-01";
            var end = "2020-02-01";
            var toCheck = "2020-02-01";

            var startDate = DateTime.Parse(start);
            var endDate = DateTime.Parse(end);
            var toCheckDate = DateTime.Parse(toCheck);

            var result = MealService.IsDateInRange(startDate, endDate, toCheckDate);
            Assert.AreEqual(true, result);
        }

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

            var mockBlobUri = new Uri("http://bogus/myaccount/blob");
            this.mockBlobContainer = new Mock<CloudBlobContainer>(MockBehavior.Loose, mockBlobUri);
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
                Restaurant = "Gasthof Adler",
            };

            // Setup Mock
            var httpRequest = CreateMockRequest(mealModel);
            var result = MealService.CreateMeal(httpRequest.Object, this.mockBlobContainer.Object, this.log, this.context).Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(typeof(OkResult), result.GetType());
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

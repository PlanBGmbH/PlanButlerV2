// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PlanB.Butler.Services.Test
{
    /// <summary>
    /// MealServiceTest.
    /// </summary>
    [TestClass]
    public class MealServiceTest
    {
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
    }
}

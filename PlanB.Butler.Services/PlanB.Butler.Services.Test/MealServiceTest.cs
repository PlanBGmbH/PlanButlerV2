// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

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
    }
}

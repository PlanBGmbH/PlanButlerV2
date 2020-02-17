using BotLibraryV2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace BotLibraryTest
{
    /// <summary>
    /// CalenderTests.
    /// </summary>
    [TestClass]
    public class CalenderTests
    {
        /// <summary>
        /// Called when defined workdays is wrong.
        /// </summary>
        [TestMethod]
        public void OnlyDefinedWorkdaysFail()
        {

            var result =BotMethods.CalculateNextDay("test");
            Assert.AreNotEqual(DateTime.MinValue, result);
        }
    }
}

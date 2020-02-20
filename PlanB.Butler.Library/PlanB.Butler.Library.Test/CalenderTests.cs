using BotLibraryV2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Bot.Builder;
using System;
using System.Resources;
using System.Threading;
using System.Globalization;
using System.Reflection;

namespace BotLibraryTest
{
    /// <summary>
    /// CalenderTests.
    /// </summary>
    [TestClass]


    public class CalenderTests
    {
        private static ResourceManager rm = new ResourceManager("PlanB.Butler.Library.Test.DictionaryTest.resourceTest", Assembly.GetExecutingAssembly());
        private static string thanks = rm.GetString("thanks");
        private static string hello = rm.GetString("hello");
        /// <summary>
        /// Called when defined workdays is wrong.
        /// </summary>
        [TestMethod]
        public void OnlyDefinedWorkdaysFail()
        {
            var message = MessageFactory.Text(thanks, hello);

            Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-EN");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-EN");

            var message1 = MessageFactory.Text(thanks, hello);


            //Console.WriteLine("CurrentUICulture is now {0}." + bestellen, CultureInfo.CurrentCulture);
            //string abfrage = rm.GetString("abfrage", CultureInfo.CurrentUICulture);
            //string[] tage = new string[] { bestellen, abfrage};

            var culture = new CultureInfo("de-DE");
            var day = culture.DateTimeFormat.GetDayName(DateTime.Today.DayOfWeek);
            var result = BotMethods.CalculateNextDay("test");
            Assert.AreNotEqual(DateTime.MinValue, result);
        }
    }
}

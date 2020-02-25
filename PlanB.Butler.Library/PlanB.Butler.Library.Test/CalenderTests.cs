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
        
        
        private static string sumMe = rm.GetString("sumMe");
        string[] abc = new string[] { sumMe, calculated };
        private static string calculated = rm.GetString("calculated");
        /// <summary>
        /// Called when defined workdays is wrong.
        /// </summary>
        [TestMethod]
        public void OnlyDefinedWorkdaysFail()
        {
            int sum = 5;
            string sumMeCALC = sumMe + sum + calculated;


            string[] restaurant = new string[] { "ali", "delphi" };

            var message = MessageFactory.Text(string.Format(sumMe,22.ToString(),"test"));

            //string.Format("hallo {0}, thanks{1}", hallo2, hallo3);

            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-EN");
            //Thread.CurrentThread.CurrentCulture = new CultureInfo("de-EN");

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

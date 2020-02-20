using BotLibraryV2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        /// <summary>
        /// Called when defined workdays is wrong.
        /// </summary>
        [TestMethod]
        public void OnlyDefinedWorkdaysFail()
        {
            ResourceManager rm = new ResourceManager("PlanB.Butler.Library.Test.Dictionary.main", Assembly.GetExecutingAssembly());
            string bestellen = rm.GetString("bestellen", CultureInfo.CurrentUICulture);
            
            Console.WriteLine(bestellen);


            // Ausgabe der main file 

            Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-IT");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-IT");
          
            bestellen = rm.GetString("hallo", CultureInfo.CurrentUICulture);
            Console.WriteLine("CurrentUICulture is now {0}." + bestellen, CultureInfo.CurrentCulture);
            string abfrage = rm.GetString("abfrage", CultureInfo.CurrentUICulture);
            string[] tage = new string[] { bestellen, abfrage};
            


            var culture = new CultureInfo("de-DE");
            var day = culture.DateTimeFormat.GetDayName(DateTime.Today.DayOfWeek);
            var result = BotMethods.CalculateNextDay("test");
            Assert.AreNotEqual(DateTime.MinValue, result);
        }
    }
}

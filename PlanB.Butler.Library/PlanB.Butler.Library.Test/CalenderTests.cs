using System;
using System.Globalization;
using System.Resources;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BotLibraryTest
{
    /// <summary>
    /// CalenderTests.
    /// </summary>
    [TestClass]


    public class CalenderTests
    {
        private static ResourceManager rm = new ResourceManager("PlanB.Butler.Library.Test.DictionaryTest.resourceTest", Assembly.GetExecutingAssembly());
        private static string hello = rm.GetString("hello");
      
        /// <summary>
        /// Called when defined workdays is wrong.
        /// </summary>
        [TestMethod]
        public void OnlyDefinedWorkdaysFail()
        {
            var hello1 = hello;

            

        }
    }
}

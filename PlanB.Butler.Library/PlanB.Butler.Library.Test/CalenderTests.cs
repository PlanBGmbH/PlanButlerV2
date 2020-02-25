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
              
        /// <summary>
        /// Called when defined workdays is wrong.
        /// </summary>
        [TestMethod]
        public void OnlyDefinedWorkdaysFail()
        {
           
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using DocumentFormat.OpenXml.Office.CustomUI;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

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

        }

        
        private class Server
        {
        }
    }
}

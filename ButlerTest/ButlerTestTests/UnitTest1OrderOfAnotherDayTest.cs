using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace ButlerTest.Tests
{
    [TestClass()]
    public class UnitTest1OrderOfAnotherDayTest
    {
        [TestMethod()]
        public void Test1CalculateTheDateForOrderOfAnotherDay()
        {
            string day = "wednesday";
            string[] weekDaysList = { "monday", "tuesday", "wednesday", "thursday", "friday" };
            int indexDay = 0;
            int indexCurentDay = 0;
            string currentDay = "wednesday";
            DateTime date = DateTime.Now;
            var stringDate = string.Empty;
            for (int i = 0; i < weekDaysList.Length; i++)
            {
                if (currentDay == weekDaysList[i])
                {
                    indexCurentDay = i;
                }
                if (day.ToLower() == weekDaysList[i])
                {
                    indexDay = i;
                }
            }
            if (indexDay == indexCurentDay)
            {

                indexCurentDay = indexDay - indexCurentDay;
                stringDate = date.ToString("yyyy-MM-dd");
            }
            else
            {
                indexCurentDay = indexDay - indexCurentDay;
                date = DateTime.Now.AddDays(indexCurentDay);
                stringDate = date.ToString("yyyy-MM-dd");
            }
            Assert.AreEqual(0, indexCurentDay);
        }

        //[TestMethod()]
        //public void Test1CalculateTheDateForOrderOfAnotherDay()
        //{
        //    string day = "wednesday";
        //    string[] weekDaysList = { "monday", "tuesday", "wednesday", "thursday", "friday" };
        //    int indexDay = 0;
        //    int indexCurentDay = 0;
        //    string currentDay = "wednesday";
        //    DateTime date = DateTime.Now;
        //    var stringDate = string.Empty;
        //    for (int i = 0; i < weekDaysList.Length; i++)
        //    {
        //        if (currentDay == weekDaysList[i])
        //        {
        //            indexCurentDay = i;
        //        }
        //        if (day.ToLower() == weekDaysList[i])
        //        {
        //            indexDay = i;
        //        }
        //    }
        //    if (indexDay == indexCurentDay)
        //    {

        //        indexCurentDay = indexDay - indexCurentDay;
        //        stringDate = date.ToString("yyyy-MM-dd");
        //    }
        //    else
        //    {
        //        indexCurentDay = indexDay - indexCurentDay;
        //        date = DateTime.Now.AddDays(indexCurentDay);
        //        stringDate = date.ToString("yyyy-MM-dd");
        //    }
        //    Assert.AreEqual(0, indexCurentDay);
        //}
    }
}
using BotLibraryV2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanB.Butler.Bot.Dialogs;
using System;

namespace PlanB.Butler.Bot.Test
{
    /// <summary>
    /// NextOrderTest.
    /// </summary>
    [TestClass]
    public class NextOrderTest
    {
        /// <summary>
        /// The plan
        /// </summary>
        private Plan plan;
        private PlanDay planDay;
        private PlanDayMeal planDayMealLinsen;


        /// <summary>
        /// Initialize each test.
        /// </summary>
        [TestInitialize]
        public void Init()
        {
            this.plan = new Plan();
            this.planDay = new PlanDay() { Name = "Day1" };
            this.planDayMealLinsen = new PlanDayMeal()
            {
                Name = "Linsen",
                Price = 2.3,
                Restaurant = "LinsenWirt",
            };
            this.planDay.Meal1 = new System.Collections.Generic.List<PlanDayMeal>();
            this.planDay.Meal1.Add(this.planDayMealLinsen);
            this.plan.Planday = new System.Collections.Generic.List<PlanDay>
            {
                this.planDay,
            };
        }

        /// <summary>
        /// Plans the null identifier empty.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void PlanNullIdentifierEmpty()
        {
            var result = NextOrder.GetChoice(string.Empty, this.plan);
            Assert.IsNotNull(result);
        }

        /// <summary>
        /// Plans the empty identifier empty.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void PlanEmptyIdentifierEmpty()
        {
            this.plan = new Plan();
            var result = NextOrder.GetChoice(string.Empty, this.plan);
            Assert.IsNotNull(result);
        }

        /// <summary>
        /// Plans the only name identifier empty.
        /// </summary>
        [TestMethod]
        public void PlanOnlyNameIdentifierEmpty()
        {
            this.plan = new Plan();
            var planDay = new PlanDay() { Name = "Day1" };
            this.plan.Planday = new System.Collections.Generic.List<PlanDay>
            {
                planDay,
            };

            var result = NextOrder.GetChoice(string.Empty, this.plan);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }


        /// <summary>
        /// Plans the one meal identifier empty.
        /// </summary>
        [TestMethod]
        public void PlanOneMealIdentifierEmpty()
        {
            var result = NextOrder.GetChoice(string.Empty, this.plan);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }
    }
}

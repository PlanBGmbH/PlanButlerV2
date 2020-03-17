namespace BotLibraryV2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class PlanDayMeal
    {
        public string Restaurant { get; set; }

        public string Name { get; set; }

        public double Price { get; set; }
    }

    public class PlanDay
    {
        public string Name { get; set; }

        [Obsolete("Replace Restaurant1 with List<Restaurant>")]
        public string Restaurant1 { get; set; }

        public List<PlanDayMeal> Meal1 { get; set; }

        [Obsolete("Replace Restaurant2 with List<Restaurant>")]
        public string Restaurant2 { get; set; }

        public List<PlanDayMeal> Meal2 { get; set; }
    }

    public class Plan
    {
        public string Title { get; set; }

        public List<PlanDay> Planday { get; set; }
    }
}

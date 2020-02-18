namespace BotLibraryV2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class Meal1
    {
        public string Restaurant { get; set; }

        public string Name { get; set; }

        public double Price { get; set; }
    }

    public class Meal2
    {
        public string Restaurant { get; set; }

        public string Name { get; set; }

        public double Price { get; set; }
    }

    public class PlanDay
    {
        public string Name { get; set; }

        public string Restaurant1 { get; set; }

        public List<Meal1> Meal1 { get; set; }

        public string Restaurant2 { get; set; }

        public List<Meal2> Meal2 { get; set; }
    }

    public class Plan
    {
        public string Title { get; set; }

        public List<PlanDay> Planday { get; set; }
    }
}

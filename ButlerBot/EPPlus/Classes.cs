namespace ButlerBot.EPPlus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class Order
    {
        public DateTime Date { get; set; }

        public string CompanyStatus { get; set; }

        public string Name { get; set; }

        public string Restaurant { get; set; }

        public string Meal { get; set; }

        public double Price { get; set; }

        public int Quantaty { get; set; }

        public double Grand { get; set; }
    }

    public class Day
    {
        public int Daynumber { get; set; }

        public string Name { get; set; }

        public List<Order> Order { get; set; }
    }

    public class Person
    {
        public string Name { get; set; }

        public List<Order> Orders { get; set; }
    }

    public class Restaurant
    {
        public string RestaurantName { get; set; }

        public List<Order> Orders { get; set; }
    }
}

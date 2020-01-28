namespace ButlerBot.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class SalaryDeduction
    {
        public int Daynumber { get; set; }

        public string Name { get; set; }

        public List<Order> Order { get; set; }
    }
}

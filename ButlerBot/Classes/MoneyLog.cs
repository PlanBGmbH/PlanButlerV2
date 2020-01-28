namespace ButlerBot.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class User
    {
        public string Name { get; set; }

        public double Owe { get; set; }
    }

    public class MoneyLog
    {
        public string Title { get; set; }

        public int Monthnumber { get; set; }

        public List<User> User { get; set; }
    }
}

namespace BotLibraryV2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class Day
    {
        public int Weeknumber { get; set; }

        public string Name { get; set; }

        public List<Order> Order { get; set; }
    }

    public class OrderBlob
    {
        public string Title { get; set; }

        public List<Day> Day { get; set; }
    }
}

namespace BotLibraryV2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;



    public class OrderBlob
    {
        public OrderBlob()
        {
        }

        public OrderBlob(Order order)
        {
            OrderList.Add(order);
        }

        public List<Order> OrderList { get; set; }

        
        //public OrderBlob(Order order)
        //{
        //    Order.Add(order);
        //}
    }
}

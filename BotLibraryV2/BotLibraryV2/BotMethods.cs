using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BotLibraryV2
{
    public class BotMethods
    {
        private static string[] weekDays = { "Montag", "Dienstag", "Mitwoch", "Donnerstag", "Freitag" };
        private static string[] weekDaysEN = { "monday", "tuesday", "wednesday", "thursday", "friday" };
        private static int indexer = 0;
        private static string dayName;
        /// <summary>
        /// Gets a document from our StorageAccount
        /// </summary>
        /// <param name="container">Describes the needed container</param>
        /// <param name="resourceName">Describes the needed resource</param>
        /// <returns>Returns a JSON you specified with container and resourceName</returns>
        public static string GetDocument(string container, string resourceName)
        {
            BackendCommunication backendcom = new BackendCommunication();
            string taskUrl = backendcom.GetDocument(container, resourceName);
            return taskUrl;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="resourceName"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static HttpStatusCode PutDocument(string container, string resourceName, string body, string que)
        {
            BackendCommunication backendcom = new BackendCommunication();
            HttpStatusCode taskUrl = backendcom.PutDocument(container, resourceName, body, que);
            return taskUrl;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        public static HttpStatusCode UploadMoney(Order order)
        {
            try
            {
                MoneyLog money = JsonConvert.DeserializeObject<MoneyLog>(GetDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json"));
                var _money = money;
                var userId = _money.User.FindIndex(x => x.Name == order.Name);
                if (userId == -1) // enters if the current user is not in the list 
                {
                    User user = new User() { Name = order.Name, Owe = order.Price };
                    _money.User.Add(user);

                    HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money),"q.planbutlerupdatemoney");
                    return status;
                }
                else // enters if everything is normal
                {
                    var newOwe = money.User[userId].Owe;
                    newOwe += order.Price;
                    _money.User[userId].Owe = newOwe;

                    HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money), "q.planbutlerupdatemoney");
                    return status;
                }
            }
            catch // enters if blob dont exist
            {
                List<User> users = new List<User>();
                User user = new User() { Name = order.Name, Owe = order.Price };
                users.Add(user);
                MoneyLog money = new MoneyLog() { Monthnumber = DateTime.Now.Month, Title = "moneylog", User = users };

                HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money), "q.planbutlerupdatemoney");
                return status;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        public static HttpStatusCode UploadOrder(Order order)
        {

            DateTime date = DateTime.Now;
            var stringDate = date.ToString("yyyy-MM-dd");
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json"));

                List<Order> orders = new List<Order>();
                orders.Add(order);

                HttpStatusCode status = PutDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder");
                return status;

            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                orders.Add(order);


                HttpStatusCode status = PutDocument("orders",  "orders_" + stringDate + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder");
                return status;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        public static HttpStatusCode UpdateOrder(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            DateTime date = DateTime.Now;
            var stringDate = date.ToString("yyyy-MM-dd");
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json"));

                List<Order> orders = new List<Order>();
                orders.Add(order);

                HttpStatusCode status = PutDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder");
                return status;

            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                orders.Add(order);

                HttpStatusCode status = PutDocument("orders",  "orders_" + stringDate + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder");
                return status;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        public static HttpStatusCode UploadOrderforSalaryDeduction(Order order)
        {
            SalaryDeduction salaryDeduction = new SalaryDeduction();
            int dayNumber = DateTime.Now.DayOfYear;
            try
            {
                salaryDeduction = JsonConvert.DeserializeObject<SalaryDeduction>(GetDocument("salarydeduction", "orders_" + dayNumber.ToString() + "_" + DateTime.Now.Year + ".json"));
                salaryDeduction.Order.Add(order);
                HttpStatusCode status = PutDocument("salarydeduction", "orders_" + dayNumber.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction), "q.planbutlerupdatesalary");
                return status;
            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                salaryDeduction.Daynumber = dayNumber;
                salaryDeduction.Name = "SalaryDeduction";

                orders.Add(order);
                salaryDeduction.Order = orders;

                HttpStatusCode status = PutDocument("salarydeduction", "orders_" + dayNumber.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction), "q.planbutlerupdatesalary");
                return status;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        public static void DeleteMoney(Order order, string day)
        {
            try
            {
                MoneyLog money = JsonConvert.DeserializeObject<MoneyLog>(GetDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json"));
                var _money = money;
                var userId = _money.User.FindIndex(x => x.Name == order.Name);
                _money.User[userId].Owe -= order.Price;
                PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money), "q.planbutlerupdatemoney");
            }
            catch // enters if blob dont exist
            {
                List<User> users = new List<User>();
                User user = new User() { Name = order.Name, Owe = order.Price };
                users.Add(user);
                MoneyLog money = new MoneyLog() { Monthnumber = DateTime.Now.Month, Title = "moneylog", User = users };

                PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money), "q.planbutlerupdatemoney");
            }
        }
        /// <summary>
        /// gets the order entry.
        /// </summary>
        /// <param name="order"></param>
        //public static Order GetOrder(Order order)
        //{
        //    OrderBlob orderBlob = new OrderBlob();
        //    int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
        //    orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json"));
        //    var valueDay = orderBlob.Day.FindIndex(x => x.Name == dayName);
        //    var bufferOrder = orderBlob.Day[valueDay].Order;
        //    var orderValue = bufferOrder.Find(x => x.Name == order.Name);
        //    return orderValue;
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param companyName="order"></param>
        public static HttpStatusCode UploadMoneyCompany(Order order)
        {
            try
            {
                MoneyLog money = JsonConvert.DeserializeObject<MoneyLog>(GetDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json"));
                var _money = money;
                if (order.CompanyStatus.ToString().ToLower() == "für mich" || order.CompanyStatus.ToString().ToLower() == "privat")
                {
                    var userId = _money.User.FindIndex(x => x.Name == order.Name);

                    // enters if the current user is not in the list. 
                    if (userId == -1)
                    {
                        User user = new User() { Name = order.Name, Owe = order.Price };
                        _money.User.Add(user);

                        HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money), "q.planbutlerupdatemoney");
                        return status;
                    }
                    else // enters if everything is normal
                    {
                        var newOwe = money.User[userId].Owe;
                        newOwe += order.Price;
                        _money.User[userId].Owe = newOwe;

                        HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money), "q.planbutlerupdatemoney");
                        return status;
                    }
                }
                else
                {
                    var userId = _money.User.FindIndex(x => x.Name == order.CompanyName);

                    // enters if the current user is not in the list. 
                    if (userId == -1)
                    {
                        User user = new User() { Name = order.CompanyName, Owe = order.Price };
                        _money.User.Add(user);

                        HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money), "q.planbutlerupdatemoney");
                        return status;
                    }
                    else // enters if everything is normal
                    {
                        var newOwe = money.User[userId].Owe;
                        newOwe += order.Price;
                        _money.User[userId].Owe = newOwe;

                        HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money), "q.planbutlerupdatemoney");
                        return status;
                    }
                }

            }
            catch // enters if blob dont exist
            {
                List<User> users = new List<User>();
                User user = new User() { Name = order.CompanyName, Owe = order.Price };
                users.Add(user);
                MoneyLog money = new MoneyLog() { Monthnumber = DateTime.Now.Month, Title = "moneylog", User = users };

                HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money), "q.planbutlerupdatemoney");
                return status;
            }
        }
        /// <summary>
        /// delets the entry of your order.
        /// </summary>
        /// <param companyName="order">.</param>
        public static void DeleteOrderCompany(Order order)
        {

            DateTime date = DateTime.Now;
            var stringDate = date.ToString("yyyy-MM-dd");
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json"));
                PutDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder");
            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                orders.Add(order);
                PutDocument("orders",  "orders_" + stringDate + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder");
            }
        }
        /// <summary>
        /// delets the entry of your order.
        /// </summary>
        /// <param companyName="order"></param>
        public static void DeleteOrderforSalaryDeductionCompany(Order order)
        {
            SalaryDeduction salaryDeduction = new SalaryDeduction();
            var dayId = order.Date.Date.DayOfYear;
            salaryDeduction = JsonConvert.DeserializeObject<SalaryDeduction>(GetDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year + ".json"));
            var nameId = salaryDeduction.Order.FindIndex(x => x.CompanyName == order.CompanyName);
            salaryDeduction.Order.RemoveAt(nameId);
            try
            {
                PutDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction), "q.planbutlerupdatesalary");
            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                salaryDeduction.Daynumber = dayId;
                salaryDeduction.Name = "SalaryDeduction";

                orders.Add(order);
                salaryDeduction.Order = orders;

                PutDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction), "q.planbutlerupdatesalary");
            }
        }
        /// <summary>
        /// .
        /// </summary>
        /// <param name="order">.</param>
        public static HttpStatusCode UploadOrderforAnotherDay(Order order)
        {

            DateTime date = DateTime.Now;
            var stringDate = date.ToString("yyyy-MM-dd");
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json"));


                List<Order> orders = new List<Order>();
                orders.Add(order);

                HttpStatusCode status = PutDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder");
                return status;

            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                orders.Add(order);



                HttpStatusCode status = PutDocument("orders",  "orders_" + stringDate + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder");
                return status;
            }
        }
        ///<summary>
        /// delets the entry of your order. Equivalent zu DeleteOrder().
        /// </summary>
        /// <param name="order"></param>
        public static void MoneyDeduction(Order order)
        {
            try
            {
                MoneyLog money = JsonConvert.DeserializeObject<MoneyLog>(GetDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json"));
                var userId = money.User.FindIndex(x => x.Name == order.Name);
                money.User[userId].Owe = Math.Round(money.User[userId].Owe - order.Price, 2);
                PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money), "q.planbutlerupdatemoney");
            }
            catch // enters if blob dont exist
            {
                List<User> users = new List<User>();
                User user = new User() { Name = order.Name, Owe = order.Price };
                users.Add(user);
                MoneyLog money = new MoneyLog() { Monthnumber = DateTime.Now.Month, Title = "moneylog", User = users };

                PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money), "q.planbutlerupdatemoney");
            }
        }
        /// <summary>
        /// delets the entry of your order. Equivalent zu DeleteOrder()
        /// </summary>
        /// <param name="order"></param>
        public static void DeleteOrder(Order order, string daySelection)
        {

            DateTime date = DateTime.Now;
            var stringDate = date.ToString("yyyy-MM-dd");
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json"));
                PutDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder");
            }
            catch (Exception ex) // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                orders.Add(order);
                PutDocument("orders",  "orders_" + stringDate + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder");
            }
        }
        /// <summary>
        /// delets the entry of your order.Equivalent to DeleteOrderForSalaryDeduction();
        /// </summary>
        /// <param name="order"></param>
        public static void DeleteOrderforSalaryDeduction(Order order)
        {
            SalaryDeduction salaryDeduction = new SalaryDeduction();
            var dayId = order.Date.Date.DayOfYear;
            salaryDeduction = JsonConvert.DeserializeObject<SalaryDeduction>(GetDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year + ".json"));
            var collection = salaryDeduction.Order.FindAll(x => x.Name == order.Name);
            var temp = collection.FindAll(x => x.CompanyStatus == order.CompanyStatus);
            salaryDeduction.Order.Remove(temp[temp.Count - 1]);

            try
            {
                PutDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction), "q.planbutlerupdateorder");
            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                salaryDeduction.Daynumber = dayId;
                salaryDeduction.Name = "SalaryDeduction";

                orders.Add(order);
                salaryDeduction.Order = orders;

                PutDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction), "q.planbutlerupdateorder");
            }
        }
        /// <summary>
        /// Equivalent to Upload Order.
        /// </summary>
        /// <param companyName="order"></param>
        public static HttpStatusCode NextOrderUpload(Order order)
        {
            DateTime date = DateTime.Now;
            var stringDate = date.ToString("yyyy-MM-dd");
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json"));
                List<Order> orders = new List<Order>();
                orders.Add(order);

                HttpStatusCode status = PutDocument("orders", "orders_" + stringDate + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder");
                return status;
            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                orders.Add(order);

                HttpStatusCode status = PutDocument("orders",  "orders_" + stringDate + "_" + order.Name + ".json", JsonConvert.SerializeObject(orderBlob), "q.planbutlerupdateorder");
                return status;
            }
        }
    }
}
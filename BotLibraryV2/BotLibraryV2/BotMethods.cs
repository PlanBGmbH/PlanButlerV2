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
        public static HttpStatusCode PutDocument(string container, string resourceName, string body)
        {
            BackendCommunication backendcom = new BackendCommunication();
            HttpStatusCode taskUrl = backendcom.PutDocument(container, resourceName, body, "q.planbutler");
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

                    HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money));
                    return status;
                }
                else // enters if everything is normal
                {
                    var newOwe = money.User[userId].Owe;
                    newOwe += order.Price;
                    _money.User[userId].Owe = newOwe;

                    HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money));
                    return status;
                }
            }
            catch // enters if blob dont exist
            {
                List<User> users = new List<User>();
                User user = new User() { Name = order.Name, Owe = order.Price };
                users.Add(user);
                MoneyLog money = new MoneyLog() { Monthnumber = DateTime.Now.Month, Title = "moneylog", User = users };

                HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money));
                return status;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        public static HttpStatusCode UploadOrder(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                var dayId = orderBlob.Day.FindIndex(x => x.Name == DateTime.Now.DayOfWeek.ToString().ToLower());
                if (dayId == -1) // enters if the current day is not in the list 
                {
                    List<Order> orders = new List<Order>();
                    orders.Add(order);
                    orderBlob.Day.Add(new Day() { Name = DateTime.Now.DayOfWeek.ToString().ToLower(), Order = orders, Weeknumber = weeknumber });
                    orderBlob.Title = "orders/" + DateTime.Now.Month + "/" + DateTime.Now.Year;

                    HttpStatusCode status = PutDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(orderBlob));
                    return status;
                }
                else // enters if everything is normal
                {
                    orderBlob.Day[dayId].Order.Add(order);


                    HttpStatusCode status = PutDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(orderBlob));
                    return status;
                }
            }
            catch // enters if blob dont exist
            {
                List<Day> day = new List<Day>();
                List<Order> orders = new List<Order>();

                orders.Add(order);
                day.Add(new Day() { Name = DateTime.Now.DayOfWeek.ToString().ToLower(), Order = orders, Weeknumber = weeknumber });

                orderBlob.Title = "orders/" + DateTime.Now.Month + "/" + DateTime.Now.Year;
                orderBlob.Day = day;

                HttpStatusCode status = PutDocument("orders", "orders_" + weeknumber.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(orderBlob));
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
                HttpStatusCode status = PutDocument("salarydeduction", "orders_" + dayNumber.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction));
                return status;
            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                salaryDeduction.Daynumber = dayNumber;
                salaryDeduction.Name = "SalaryDeduction";

                orders.Add(order);
                salaryDeduction.Order = orders;

                HttpStatusCode status = PutDocument("salarydeduction", "orders_" + dayNumber.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction));
                return status;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        public static void DeleteMoney(Order order)
        {
            try
            {
                MoneyLog money = JsonConvert.DeserializeObject<MoneyLog>(GetDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json"));
                var _money = money;
                var userId = _money.User.FindIndex(x => x.Name == order.Name);
                _money.User.RemoveAt(userId);
                PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money));
            }
            catch // enters if blob dont exist
            {
                List<User> users = new List<User>();
                User user = new User() { Name = order.Name, Owe = order.Price };
                users.Add(user);
                MoneyLog money = new MoneyLog() { Monthnumber = DateTime.Now.Month, Title = "moneylog", User = users };

                PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money));
            }
        }
        /// <summary>
        /// gets the order entry.
        /// </summary>
        /// <param name="order"></param>
        public static Order GetOrder(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
            var valueDay = orderBlob.Day.FindIndex(x => x.Name == dayName);
            var bufferOrder = orderBlob.Day[valueDay].Order;
            var nameID = bufferOrder.Find(x => x.Name == order.Name);
            return nameID;
        }
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

                        HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money));
                        return status;
                    }
                    else // enters if everything is normal
                    {
                        var newOwe = money.User[userId].Owe;
                        newOwe += order.Price;
                        _money.User[userId].Owe = newOwe;

                        HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money));
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

                        HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money));
                        return status;
                    }
                    else // enters if everything is normal
                    {
                        var newOwe = money.User[userId].Owe;
                        newOwe += order.Price;
                        _money.User[userId].Owe = newOwe;

                        HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(_money));
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

                HttpStatusCode status = PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money));
                return status;
            }
        }
        /// <summary>
        /// delets the entry of your order.
        /// </summary>
        /// <param companyName="order">.</param>
        public static void DeleteOrderCompany(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                var valueDay = orderBlob.Day.FindIndex(x => x.Name == dayName);
                var nameId = orderBlob.Day[valueDay].Order.FindIndex(x => x.CompanyName == order.CompanyName);
                orderBlob.Day[valueDay].Order.RemoveAt(nameId);
                PutDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(orderBlob));
            }
            catch // enters if blob dont exist
            {
                List<Day> day = new List<Day>();
                List<Order> orders = new List<Order>();

                orders.Add(order);
                day.Add(new Day() { Name = DateTime.Now.DayOfWeek.ToString().ToLower(), Order = orders, Weeknumber = weeknumber });

                orderBlob.Title = "orders/" + DateTime.Now.Month + "/" + DateTime.Now.Year;
                orderBlob.Day = day;

                PutDocument("orders", "orders_" + weeknumber.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(orderBlob));
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
                PutDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction));
            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                salaryDeduction.Daynumber = dayId;
                salaryDeduction.Name = "SalaryDeduction";

                orders.Add(order);
                salaryDeduction.Order = orders;

                PutDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction));
            }
        }
        /// <summary>
        /// .
        /// </summary>
        /// <param name="order">.</param>
        public static HttpStatusCode UploadOrderforAnotherDay(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                var dayId = orderBlob.Day.FindIndex(x => x.Name == weekDaysEN[indexer].ToString().ToLower());

                if (dayId == -1) // enters if the current day is not in the list 
                {
                    List<Order> orders = new List<Order>();
                    orders.Add(order);
                    orderBlob.Day.Add(new Day() { Name = weekDaysEN[indexer].ToString().ToLower(), Order = orders, Weeknumber = weeknumber });

                    orderBlob.Title = "orders/" + DateTime.Now.Month + "/" + DateTime.Now.Year;
                    orderBlob.Day[dayId].Order.Add(order);
                    HttpStatusCode status = PutDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(orderBlob));
                    return status;
                }
                else // enters if everything is normal
                {
                    var nameId = orderBlob.Day[dayId].Order.FindIndex(x => x.Name == order.Name);
                    orderBlob.Day[dayId].Order.Add(order);
                    HttpStatusCode status = PutDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(orderBlob));
                    return status;
                }
            }
            catch // enters if blob dont exist
            {
                List<Day> day = new List<Day>();
                List<Order> orders = new List<Order>();

                orders.Add(order);
                day.Add(new Day() { Name = weekDaysEN[indexer], Order = orders, Weeknumber = weeknumber });

                orderBlob.Title = "orders/" + DateTime.Now.Month + "/" + DateTime.Now.Year;


                HttpStatusCode status = PutDocument("orders", "orders_" + weeknumber.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(orderBlob));
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
                PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money));
            }
            catch // enters if blob dont exist
            {
                List<User> users = new List<User>();
                User user = new User() { Name = order.Name, Owe = order.Price };
                users.Add(user);
                MoneyLog money = new MoneyLog() { Monthnumber = DateTime.Now.Month, Title = "moneylog", User = users };

                PutDocument("moneylog", "money_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(money));
            }
        }
        /// <summary>
        /// delets the entry of your order. Equivalent zu DeleteOrder()
        /// </summary>
        /// <param name="order"></param>
        public static void DeleteOrder(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                var valueDay = orderBlob.Day.FindIndex(x => x.Name == dayName);
                var collection = orderBlob.Day[valueDay].Order.FindAll(x => x.Name == order.Name);
                foreach (var item in collection)
                {
                    if (item.Meal == order.Meal)
                    {
                        orderBlob.Day[valueDay].Order.Remove(item);
                    }
                }

                PutDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(orderBlob));
            }
            catch // enters if blob dont exist
            {
                List<Day> day = new List<Day>();
                List<Order> orders = new List<Order>();

                orders.Add(order);
                day.Add(new Day() { Name = DateTime.Now.DayOfWeek.ToString().ToLower(), Order = orders, Weeknumber = weeknumber });

                orderBlob.Title = "orders/" + DateTime.Now.Month + "/" + DateTime.Now.Year;
                orderBlob.Day = day;

                PutDocument("orders", "orders_" + weeknumber.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(orderBlob));
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
            foreach (var item in collection)
            {
                if (item.Meal == order.Meal)
                {
                    salaryDeduction.Order.Remove(item);
                }
            }

            try
            {
                PutDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction));
            }
            catch // enters if blob dont exist
            {
                List<Order> orders = new List<Order>();

                salaryDeduction.Daynumber = dayId;
                salaryDeduction.Name = "SalaryDeduction";

                orders.Add(order);
                salaryDeduction.Order = orders;

                PutDocument("salarydeduction", "orders_" + dayId.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(salaryDeduction));
            }
        }
        /// <summary>
        /// Equivalent to Upload Order.
        /// </summary>
        /// <param companyName="order"></param>
        public static HttpStatusCode NextOrderUpload(Order order)
        {
            OrderBlob orderBlob = new OrderBlob();
            int weeknumber = (DateTime.Now.DayOfYear / 7) + 1;
            try
            {
                orderBlob = JsonConvert.DeserializeObject<OrderBlob>(GetDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json"));
                var dayId = orderBlob.Day.FindIndex(x => x.Name == weekDaysEN[indexer]);
                if (dayId == -1) // enters if the current day is not in the list 
                {
                    List<Order> orders = new List<Order>();
                    orders.Add(order);
                    orderBlob.Day.Add(new Day() { Name = weekDaysEN[indexer], Order = orders, Weeknumber = weeknumber });
                    orderBlob.Title = "orders/" + DateTime.Now.Month + "/" + DateTime.Now.Year;

                    HttpStatusCode status = PutDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(orderBlob));
                    return status;
                }
                else // enters if everything is normal
                {
                    orderBlob.Day[dayId].Order.Add(order);
                    HttpStatusCode status = PutDocument("orders", "orders_" + weeknumber + "_" + DateTime.Now.Year + ".json", JsonConvert.SerializeObject(orderBlob));
                    return status;
                }
            }
            catch // enters if blob dont exist
            {
                List<Day> day = new List<Day>();
                List<Order> orders = new List<Order>();

                orders.Add(order);
                day.Add(new Day() { Name = DateTime.Now.DayOfWeek.ToString().ToLower(), Order = orders, Weeknumber = weeknumber });

                orderBlob.Title = "orders/" + DateTime.Now.Month + "/" + DateTime.Now.Year;
                orderBlob.Day = day;

                HttpStatusCode status = PutDocument("orders", "orders_" + weeknumber.ToString() + "_" + DateTime.Now.Year.ToString() + ".json", JsonConvert.SerializeObject(orderBlob));
                return status;
            }
        }
    }
}
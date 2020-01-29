using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using System.Net;
using System.Net.Http;
using BotLibraryV2;
using ButlerBot.Util;

namespace ButlerBot
{
    public static class getExcel
    {
        public static bool Run()
        {
            //string name = req.Query["name"];

            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;

            //return name != null
            //    ? (ActionResult)new OkObjectResult($"Hello, {name}")
            //    : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
            MemoryStream stream = new MemoryStream();
            using (var excelPackage = new ExcelPackage(stream))
            {
                // Create the Worksheet
                var internAndSecondMeal = excelPackage.Workbook.Worksheets.Add("Intern + Zweites Essen");
                var externMeal = excelPackage.Workbook.Worksheets.Add("Extern");
                var restaurantBillingCheck = excelPackage.Workbook.Worksheets.Add("Restaurant Rechnungsprüfung");

                List<Day> days = GetAndPrepareMeals();
                List<Person> persons = GetAndPreparePersons(days);
                List<Restaurant> restaurants = GetAndPrepareRestaurants(days);

                // Prepare Tables basically.
                CreateInternAndSecondMeal(internAndSecondMeal, persons);
                CreateExternMeals(externMeal, persons);
                CreateRestaurantBillingCheck(restaurantBillingCheck, restaurants);

                //FileInfo file = new FileInfo("C:\\Users\\SamuelS\\Desktop\\test.xlsx");
                //excelPackage.SaveAs(file);
                //byte[] arr = excelPackage.GetAsByteArray();
                excelPackage.Save();
                // Return Excel File under the given Path.
                PutDocument("excel", "45.xlsx", stream);

            }
            //FileStream fs = new FileStream("C:\\Users\\SamuelS\\Desktop\\test.xlsx", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            //stream.WriteTo(fs);
            //stream.Dispose();
            //fs.Dispose();
            return true;
        }
        public static void CreateInternAndSecondMeal(ExcelWorksheet worksheetReference, List<Person> persons)
        {

            // Create and Fill date Collum
            worksheetReference.Cells[1, 1].Value = "Datum";
            CreateDateCollum(worksheetReference, 3);

            // Create and Fill employees Collum
            int counter = 2;
            foreach (var person in persons)
            {
                if (person.Orders[0].CompanyStatus != "Kunde")
                {
                    worksheetReference.Cells[1, counter].Value = "Betrag";
                    worksheetReference.Cells[1, counter + 1].Value = "Zuschuss";
                    worksheetReference.Cells[1, counter + 2].Value = "Endbetrag";
                    worksheetReference.Cells[2, counter, 2, counter + 2].Merge = true;
                    worksheetReference.Cells[2, counter].Value = person.Name;
                    int row = 0;
                    double total = 0;
                    double grand = 3.3;
                    foreach (var order in person.Orders)
                    {
                        row = order.Date.Day;
                        total += order.Price + order.Grand;
                    }
                    worksheetReference.Cells[row + 2, counter].Value = total;
                    worksheetReference.Cells[row + 2, counter + 1].Value = grand;
                    worksheetReference.Cells[row + 2, counter + 2].Value = total - grand;

                    counter += 3;
                }
            }
            if (counter > 2)
            {
                // Format Collums
                worksheetReference.Cells[2, 2, 2, counter - 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheetReference.Cells[1, 1, 1, counter - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetReference.Cells[1, 1, 1, counter - 1].Style.Fill.BackgroundColor.SetColor(100, 255, 255, 0);
                //worksheetReference.Cells[worksheetReference.Dimension.Address].AutoFitColumns();
            }
        }

        public static void CreateExternMeals(ExcelWorksheet worksheetReference, List<Person> persons)
        {
            // Create and Fill date Collum
            worksheetReference.Cells[1, 1].Value = "Datum";
            CreateDateCollum(worksheetReference, 3);

            // Create and Fill extern Collum
            int counter = 2;
            foreach (var person in persons)
            {
                if (person.Orders[0].CompanyStatus == "Kunde")
                {
                    worksheetReference.Cells[1, counter].Value = "Zweck";
                    worksheetReference.Cells[1, counter + 1].Value = "Bewirtete Personen";
                    worksheetReference.Cells[1, counter + 2].Value = "Gesamtsumme";
                    worksheetReference.Cells[2, counter, 2, counter + 2].Merge = true;
                    worksheetReference.Cells[2, counter].Value = person.Name;
                    int row = 0;
                    double total = 0;
                    foreach (var order in person.Orders)
                    {
                        row = order.Date.Day;
                        total += order.Price + order.Grand;
                    }
                    // worksheetReference.Cells[row + 2, counter].Value = ; Zweck
                    // worksheetReference.Cells[row + 2, counter + 1].Value = ; Bewirtete Personen
                    worksheetReference.Cells[row + 2, counter + 2].Value = total;

                    counter += 3;
                }
            }
            if (counter > 2)
            {
                // Format Collums
                worksheetReference.Cells[2, 2, 2, counter - 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheetReference.Cells[1, 1, 1, counter - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetReference.Cells[1, 1, 1, counter - 1].Style.Fill.BackgroundColor.SetColor(100, 255, 255, 0);
                //worksheetReference.Cells[worksheetReference.Dimension.Address].AutoFitColumns();
            }
        }

        public static void CreateRestaurantBillingCheck(ExcelWorksheet worksheetReference, List<Restaurant> restaurant)
        {
            // Create and Fill date Collum
            worksheetReference.Cells[1, 1].Value = "Datum";
            CreateDateCollum(worksheetReference, 3);

            // Create and Fill employees Collum
            int counter = 2;
            foreach (var order in restaurant)
            {
                worksheetReference.Cells[1, counter].Value = "Betrag";
                worksheetReference.Cells[1, counter + 1].Value = "Anzahl Essen";
                worksheetReference.Cells[1, counter + 2].Value = "Davon Kundenessen";
                worksheetReference.Cells[2, counter, 2, counter + 2].Merge = true;
                worksheetReference.Cells[2, counter].Value = order.RestaurantName;
                int row = 0;
                double total = 0;
                int count = 0;
                int costumerCount = 0;
                foreach (var test in order.Orders)
                {
                    row = test.Date.Day;
                    count += test.Quantaty;
                    total += test.Price + test.Grand;
                    if (test.CompanyStatus == "Kunde")
                    {
                        costumerCount += test.Quantaty;
                    }
                }
                worksheetReference.Cells[row + 2, counter].Value = total;
                worksheetReference.Cells[row + 2, counter + 1].Value = count;
                worksheetReference.Cells[row + 2, counter + 2].Value = costumerCount;

                counter += 3;
            }
            if (counter > 2)
            {
                // Format Collums
                worksheetReference.Cells[2, 2, 2, counter - 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheetReference.Cells[1, 1, 1, counter - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetReference.Cells[1, 1, 1, counter - 1].Style.Fill.BackgroundColor.SetColor(100, 255, 255, 0);
                //worksheetReference.Cells[worksheetReference.Dimension.Address].AutoFitColumns();
            }
        }

        public static List<Day> GetAndPrepareMeals()
        {
            List<Day> days = new List<Day>();
            DateTime date = DateTime.Today;
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            for (int i = firstDayOfMonth.DayOfYear; i < lastDayOfMonth.DayOfYear; i++)
            {
                try
                {
                    Day day = JsonConvert.DeserializeObject<Day>(BotMethods.GetDocument("salarydeduction", $"orders_{i.ToString()}_{DateTime.Now.Year}.json"));
                    days.Add(day);
                }
                catch
                {
                    // NoThInG
                }
            }
            return days;
        }

        public static List<Person> GetAndPreparePersons(List<Day> days)
        {
            List<Person> persons = new List<Person>();
            foreach (var order in days)
            {
                foreach (var person in order.Order)
                {
                    var pIndex = persons.FindIndex(x => x.Name == person.Name);
                    if (pIndex == -1)
                    {
                        Person person1 = new Person();
                        List<Order> order1 = new List<Order>();
                        order1.Add(person);

                        person1.Name = person.Name;
                        person1.Orders = order1;
                        persons.Add(person1);
                    }
                    else
                    {
                        persons[pIndex].Orders.Add(person);
                    }
                }
            }

            return persons;
        }

        public static List<Restaurant> GetAndPrepareRestaurants(List<Day> days)
        {
            List<Restaurant> restaurants = new List<Restaurant>();
            foreach (var order in days)
            {
                foreach (var person in order.Order)
                {
                    var rIndex = restaurants.FindIndex(x => x.RestaurantName == person.Restaurant);
                    if (rIndex == -1)
                    {
                        Restaurant restaurant = new Restaurant();
                        List<Order> order1 = new List<Order>();
                        order1.Add(person);

                        restaurant.RestaurantName = person.Restaurant;
                        restaurant.Orders = order1;
                        restaurants.Add(restaurant);
                    }
                    else
                    {
                        restaurants[rIndex].Orders.Add(person);
                    }
                }
            }
            return restaurants;
        }

        public static void CreateDateCollum(ExcelWorksheet worksheetReference, int startCollum)
        {
            var dayCount = 1;
            var month = DateTime.Now.Month;
            var year = DateTime.Now.Year;
            for (int i = startCollum; i < DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month) + 3; i++)
            {
                var date = $"{dayCount}/{month}/{year}";
                worksheetReference.Cells[i, 1].Value = date;
                dayCount++;
            }
            worksheetReference.Cells[startCollum, 1, dayCount + 1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        }

        private static bool PutDocument(string container, string resourceName, MemoryStream body)
        {
            try
            {
                BackendCommunication backendcom = new BackendCommunication();
                //HttpStatusCode taskUrl = backendcom.PutDocumentStream(container, resourceName, body, "q.planbutler");
                var sas = backendcom.GenerateStorageSasToken($"{container}/{resourceName}", Settings.StorageAccountUrl, Settings.StorageAccountKey);
                HttpClient client = new HttpClient();
                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Put, sas);
                msg.Headers.Add("x-ms-blob-type", "BlockBlob");
                msg.Headers.Add("x-ms-date", DateTime.UtcNow.ToString());
                msg.Headers.Add("x-ms-version", "2018-03-28");
                msg.Headers.Add("x-ms-date", "application/octet-stream");
                body.Position = 0;
                msg.Content = new StreamContent(body);
                var resp = client.SendAsync(msg).Result;
                if (resp.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
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
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;

namespace ButlerBot
{
    /// <summary>
    /// ExcelGenerator.
    /// </summary>
    internal static class ExcelGenerator
    {
        private static List<SalaryDeduction> salaryDeductionList = new List<SalaryDeduction>();
        internal static bool Run(List<SalaryDeduction> sal)
        {
            salaryDeductionList = sal;
            MemoryStream stream = new MemoryStream();
            using (var excelPackage = new ExcelPackage(stream))
            {
                // Create the Worksheets
                var internAndSecondMeal = excelPackage.Workbook.Worksheets.Add("Intern + Zweites Essen");
                var externMeal = excelPackage.Workbook.Worksheets.Add("Extern");
                var restaurantBillingCheck = excelPackage.Workbook.Worksheets.Add("Restaurant Rechnungsprüfung");

                List<Day> days = GetAndPrepareMeals(sal);
                List<Person> persons = GetAndPreparePersons(sal);
                List<Restaurant> restaurants = GetAndPrepareRestaurants(sal);

                //  Prepare Tables basically.
                CreateInternAndSecondMeal(internAndSecondMeal, persons);
                CreateExternMeals(externMeal, persons);
                CreateRestaurantBillingCheck(restaurantBillingCheck, restaurants);

                //FileInfo file = new FileInfo("C:\\Users\\PeterS\\Desktop\\test.xlsx");
                //excelPackage.SaveAs(file);
                //byte[] arr = excelPackage.GetAsByteArray();
                excelPackage.Save();
                byte[] test = excelPackage.GetAsByteArray();
                //   Return Excel File under the given Path.
                string monthid = "";
                if (DateTime.Now.Month < 10)
                {
                    monthid = "0" + DateTime.Now.Month.ToString();
                }
                else
                {
                    monthid = DateTime.Now.Month.ToString();
                }
                PutDocument("excel", "Monatsuebersicht_" + monthid + "_" + DateTime.Now.Year + ".xlsx", test);
            }

            //FileStream fs = new FileStream("C:\\Users\\PeterS\\Desktop\\test.xlsx", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            //stream.WriteTo(fs);
            //session.send(stream);
            //stream.Dispose();
            //fs.Dispose();
            return true;
        }
        internal static void CreateInternAndSecondMeal(ExcelWorksheet worksheetReference, List<Person> persons)
        {
            //Create and Fill date Collum
            worksheetReference.Cells[1, 1].Value = "Datum";
            CreateDateCollum(worksheetReference, 3);
            //Create and Fill employees Collum
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
                    DateTime buffer = DateTime.Now;
                    string nameBuffer = "";
                    bool valid = false;
                    foreach (var order in person.Orders)
                    {
                        if (order.Date.ToString("yyyy-MM-dd") == buffer.ToString("yyyy-MM-dd"))
                        {
                            row = order.Date.Day;
                            total += order.Price + order.Grand;
                            buffer = order.Date;
                            valid = true;
                        }
                        else
                        {
                            if (valid)
                            {
                                worksheetReference.Cells[row + 2, counter].Value = total;
                                worksheetReference.Cells[row + 2, counter + 1].Value = grand;
                                worksheetReference.Cells[row + 2, counter + 2].Value = total - grand;
                                valid = false;
                            }
                            row = 0;
                            total = 0;
                            row += order.Date.Day;
                            total += order.Price + order.Grand;
                            buffer = order.Date;


                        }

                    }
                    worksheetReference.Cells[row + 2, counter].Value = total;
                    worksheetReference.Cells[row + 2, counter + 1].Value = grand;
                    worksheetReference.Cells[row + 2, counter + 2].Value = total - grand;
                    counter += 3;
                }
            }
            if (counter > 2)
            {
                //Format Collums
                worksheetReference.Cells[2, 2, 2, counter - 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheetReference.Cells[1, 1, 1, counter - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetReference.Cells[1, 1, 1, counter - 1].Style.Fill.BackgroundColor.SetColor(100, 255, 255, 0);
                worksheetReference.Cells[worksheetReference.Dimension.Address].AutoFitColumns();
            }
        }

        internal static void CreateExternMeals(ExcelWorksheet worksheetReference, List<Person> persons)
        {
            //Create and Fill date Collum
            worksheetReference.Cells[1, 1].Value = "Datum";
            CreateDateCollum(worksheetReference, 3);

            //Create and Fill extern Collum
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
                    //  worksheetReference.Cells[row + 2, counter].Value = ; //Zweck
                    //worksheetReference.Cells[row + 2, counter + 1].Value = ; //Bewirtete Personen
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
                worksheetReference.Cells[worksheetReference.Dimension.Address].AutoFitColumns();
            }
        }

        internal static void CreateRestaurantBillingCheck(ExcelWorksheet worksheetReference, List<Restaurant> restaurant)
        {
            //Create and Fill date Collum
            worksheetReference.Cells[1, 1].Value = "Datum";
            CreateDateCollum(worksheetReference, 3);
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
                DateTime buffer = DateTime.Now;
                string nameBuffer = "";
                bool valid = false;
                foreach (var item in order.Orders)
                {
                    if (item.Date.ToString("yyyy-MM-dd") == buffer.ToString("yyyy-MM-dd"))
                    {
                        row = item.Date.Day;
                        count += item.Quantaty;
                        total += item.Price;
                        if (item.CompanyStatus == "Kunde")
                        {
                            costumerCount += item.Quantaty;
                        }
                    }
                    else
                    {
                        if (valid)
                        {
                            worksheetReference.Cells[row + 2, counter].Value = total;
                            worksheetReference.Cells[row + 2, counter + 1].Value = count;
                            worksheetReference.Cells[row + 2, counter + 2].Value = costumerCount;
                            valid = false;
                        }
                        row = 0;
                        total = 0;
                        row += item.Date.Day;
                        total += item.Price;
                        buffer = item.Date;


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
                worksheetReference.Cells[worksheetReference.Dimension.Address].AutoFitColumns();
            }
        }

        internal static List<Day> GetAndPrepareMeals(List<SalaryDeduction> salaryList)
        {
            List<Day> order = new List<Day>();
            DateTime date = DateTime.Today;
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            return order;
        }

        internal static List<Person> GetAndPreparePersons(List<SalaryDeduction> sal)
        {
            List<Person> persons = new List<Person>();

            foreach (var order in sal)
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

        public static List<Restaurant> GetAndPrepareRestaurants(List<SalaryDeduction> sal)
        {
            List<Restaurant> restaurants = new List<Restaurant>();
            foreach (var order in sal)
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

        private static bool PutDocument(string container, string resourceName, byte[] body)
        {
            try
            {
                BackendCommunication backendcom = new BackendCommunication();
                HttpStatusCode taskUrl = backendcom.PutDocumentByteArray(container, resourceName, body, "q.planbutlerupdateexcel");
                var sas = string.Empty;//TODO backendcom.GenerateStorageSasTokenWrite($"{container}/{resourceName}", Settings.StorageAccountUrl, Settings.StorageAccountKey);
                HttpClient client = new HttpClient();
                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Put, sas);
                msg.Headers.Add("x-ms-blob-type", "BlockBlob");
                msg.Headers.Add("x-ms-date", DateTime.UtcNow.ToString());
                msg.Headers.Add("x-ms-version", "2018-03-28");
                msg.Headers.Add("x-ms-date", "application/octet-stream");
                //body.Position = 0;
                msg.Content = new ByteArrayContent(body);
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
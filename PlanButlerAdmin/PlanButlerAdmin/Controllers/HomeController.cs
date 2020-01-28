using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDataReader;
using Microsoft.AspNetCore.Mvc;
using PlanButlerAdmin.Models;
using Newtonsoft.Json;
using System.Drawing;
using System.Net;

namespace PlanButlerAdmin.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Plan()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Plan(string[] location)
        {
            ViewData["Message"] = "fail";
            bool checkPaths = true;

            foreach (var checkpath in location)
            {
                if (checkpath != null && checkpath != "")
                {
                    checkPaths = false;
                }
            }

            if (checkPaths)
            {
                ViewData["Message"] = "fail";
                return View();
            }
            List<PlanModel> planList = new List<PlanModel>();
            var json = GetDocument("eatingplan", "tempOverview" + ".json");

            List<Food> items = JsonConvert.DeserializeObject<List<Food>>(json);
            for (int i = 0; i < location.Length; i += 2)
            {
                PlanModel plan = new PlanModel();
                switch (i)
                {
                    case 0:
                        plan.name = "monday";
                        break;
                    case 2:
                        plan.name = "tuesday";
                        break;
                    case 4:
                        plan.name = "wednesday";
                        break;
                    case 6:
                        plan.name = "thursday";
                        break;
                    case 8:
                        plan.name = "friday";
                        break;
                }
                plan.meal1 = new List<Food>();
                plan.meal2 = new List<Food>();
                foreach (var item in items)
                {
                    if (location[i] == item.restaurant)
                    {

                        plan.restaurant1 = item.restaurant;
                        var temp = item;

                        plan.meal1.Add(temp);

                    }
                    if (location[i + 1] == item.restaurant)
                    {
                        plan.restaurant2 = item.restaurant;

                        plan.meal2.Add(item);
                    }
                }
                planList.Add(plan);
            }

            postPlan(planList);
            ViewData["Message"] = "Success";
            return View();
        }

        public void postPlan(List<PlanModel> plans)
        {
            OverviewModel temp = new OverviewModel();
            temp.title = "Overview";
            temp.planday = plans;
            PutDocument("eatingplan", "ButlerOverview" + ".json", JsonConvert.SerializeObject(temp));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param companyName="container"></param>
        /// <param companyName="resourceName"></param>
        /// <param companyName="body"></param>
        /// <returns></returns>
        private static HttpStatusCode PutDocument(string container, string resourceName, string body)
        {
            ButlerBot.Util.BackendCommunication backendcom = new ButlerBot.Util.BackendCommunication();
            HttpStatusCode taskUrl = backendcom.PutDocument(container, resourceName, body, "q.planbutler");
            return taskUrl;
        }

        [HttpPost]
        public IActionResult Index(string[] filePath, string[] picPath)
        {
            ViewData["Message"] = "fail";
            bool checkPaths = true;
            bool checkPics = true;
            foreach (var checkpath in filePath)
            {
                if (checkpath != null && checkpath != "")
                {
                    checkPaths = false;
                }
            }
            foreach (var checkpic in picPath)
            {
                if (checkpic != null && checkpic != "")
                {
                    checkPics = false;
                }
            }
            if (checkPics == true && checkPaths == true)
            {
                ViewData["Message"] = "fail";
                return View();
            }

            var rootPath = @"C:\Users\Public\";
            int index = 0;
            List<Food> foods = new List<Food>();
            List<Food> temp = new List<Food>();
            foreach (var path in filePath)
            {
                if (path != "" && path != null)
                {
                    using (var stream = System.IO.File.Open(rootPath + path, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = ExcelReaderFactory.CreateReader(stream))
                        {

                            reader.Read();
                            var restname = reader.GetValue(0).ToString();
                            do
                            {
                                while (reader.Read())
                                {

                                    var name = reader.GetValue(0);
                                    var price = reader.GetValue(1);

                                    Food food = new Food();
                                    if (name != null)
                                    {
                                        food.name = name.ToString();
                                    }
                                    else
                                    {
                                        food.name = string.Empty;
                                    }
                                    if (price != null)
                                    {
                                        food.price = Convert.ToDouble(price);
                                    }
                                    else
                                    {
                                        food.price = 0;
                                    }
                                    if (food.price != 0 && food.name != "")
                                    {
                                        food.restaurant = restname;
                                        temp.Add(food);
                                    }
                                }
                            } while (reader.NextResult());

                        }
                    }

                    PutDocument("eatingplan", "ButlerOverview" + ".json", JsonConvert.SerializeObject(temp));
                }
                index++;
            }
            UpdateFood(temp);
            ViewData["Message"] = "Success";
            return View();
        }

        [HttpPost]
        public void UpdateFood(List<Food> foods)
        {
            PutDocument("eatingplan", "tempOverview" + ".json", JsonConvert.SerializeObject(foods));
        }

        private static string GetDocument(string container, string resourceName)
        {
            ButlerBot.Util.BackendCommunication backendcom = new ButlerBot.Util.BackendCommunication();
            string taskUrl = backendcom.GetDocument(container, resourceName);
            return taskUrl;
        }

        public IActionResult Permission()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

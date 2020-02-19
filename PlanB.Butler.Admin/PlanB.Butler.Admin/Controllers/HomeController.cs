// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;

using ExcelDataReader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PlanB.Butler.Admin.Models;

namespace PlanB.Butler.Admin.Controllers
{
    /// <summary>
    /// HomeController.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    [Authorize]
    public class HomeController : Controller
    {
        /// <summary>
        /// The admin configuration.
        /// </summary>
        private readonly IOptions<AdminConfig> adminConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public HomeController(IOptions<AdminConfig> config)
        {
            this.adminConfig = config;
        }

        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>Index.</returns>
        public IActionResult Index()
        {
            return this.View();
        }

        /// <summary>
        /// Plans this instance.
        /// </summary>
        /// <returns>Plan.</returns>
        public IActionResult Plan()
        {
            return this.View();
        }

        /// <summary>
        /// Plans the specified location.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>Plan.</returns>
        [HttpPost]
        public IActionResult Plan(string[] location)
        {
            this.ViewData["Message"] = "fail";
            bool checkPaths = true;

            foreach (var checkpath in location)
            {
                if (checkpath != null && !string.IsNullOrEmpty(checkpath))
                {
                    checkPaths = false;
                }
            }

            if (checkPaths)
            {
                this.ViewData["Message"] = "fail";
                return this.View();
            }

            List<PlanModel> planList = new List<PlanModel>();
            var json = GetDocument("eatingplan", "tempOverview" + ".json", this.adminConfig.Value.StorageAccountUrl, this.adminConfig.Value.StorageAccountKey);

            List<FoodModel> items = JsonConvert.DeserializeObject<List<FoodModel>>(json);
            for (int i = 0; i < location.Length; i += 2)
            {
                PlanModel plan = new PlanModel();
                switch (i)
                {
                    case 0:
                        plan.Name = "monday";
                        break;
                    case 2:
                        plan.Name = "tuesday";
                        break;
                    case 4:
                        plan.Name = "wednesday";
                        break;
                    case 6:
                        plan.Name = "thursday";
                        break;
                    case 8:
                        plan.Name = "friday";
                        break;
                }

                plan.Meal1 = new List<FoodModel>();
                plan.Meal2 = new List<FoodModel>();
                foreach (var item in items)
                {
                    if (location[i] == item.Restaurant)
                    {
                        plan.Restaurant1 = item.Restaurant;
                        var temp = item;

                        plan.Meal1.Add(temp);
                    }

                    if (location[i + 1] == item.Restaurant)
                    {
                        plan.Restaurant2 = item.Restaurant;

                        plan.Meal2.Add(item);
                    }
                }

                planList.Add(plan);
            }

            this.PostPlan(planList);
            this.ViewData["Message"] = "Success";
            return this.View();
        }

        /// <summary>
        /// Posts the plan.
        /// </summary>
        /// <param name="plans">The plans.</param>
        public void PostPlan(List<PlanModel> plans)
        {
            OverviewModel overviewModel = new OverviewModel
            {
                Title = "Overview",
                PlanDay = plans,
            };
            PutDocument("eatingplan", "ButlerOverview" + ".json", JsonConvert.SerializeObject(overviewModel), this.adminConfig.Value.ServiceBusConnectionString);
        }

        /// <summary>
        /// Indexes the specified file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="picPath">The pic path.</param>
        /// <returns>Index.</returns>
        [HttpPost]
        public IActionResult Index(string[] filePath, string[] picPath)
        {
            this.ViewData["Message"] = "fail";
            bool checkPaths = true;
            bool checkPics = true;
            foreach (var checkpath in filePath)
            {
                if (!string.IsNullOrEmpty(checkpath))
                {
                    checkPaths = false;
                }
            }

            foreach (var checkpic in picPath)
            {
                if (!string.IsNullOrEmpty(checkpic))
                {
                    checkPics = false;
                }
            }

            if (checkPics == true && checkPaths == true)
            {
                this.ViewData["Message"] = "fail";
                return this.View();
            }

            // TODO: What is this?
            var rootPath = @"C:\Users\Public\";
            int index = 0;
            List<FoodModel> foods = new List<FoodModel>();
            List<FoodModel> temp = new List<FoodModel>();
            foreach (var path in filePath)
            {
                if (!string.IsNullOrEmpty(path))
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

                                    FoodModel food = new FoodModel();
                                    if (name != null)
                                    {
                                        food.Name = name.ToString();
                                    }
                                    else
                                    {
                                        food.Name = string.Empty;
                                    }

                                    if (price != null)
                                    {
                                        food.Price = Convert.ToDouble(price);
                                    }
                                    else
                                    {
                                        food.Price = 0;
                                    }

                                    if (food.Price != 0 && !string.IsNullOrEmpty(food.Name))
                                    {
                                        food.Restaurant = restname;
                                        temp.Add(food);
                                    }
                                }
                            }
                            while (reader.NextResult());
                        }
                    }

                    PutDocument("eatingplan", "ButlerOverview" + ".json", JsonConvert.SerializeObject(temp), this.adminConfig.Value.ServiceBusConnectionString);
                }

                index++;
            }

            this.UpdateFood(temp);
            this.ViewData["Message"] = "Success";
            return this.View();
        }

        /// <summary>
        /// Updates the food.
        /// </summary>
        /// <param name="foods">The foods.</param>
        [HttpPost]
        public void UpdateFood(List<FoodModel> foods)
        {
            PutDocument("eatingplan", "tempOverview" + ".json", JsonConvert.SerializeObject(foods), this.adminConfig.Value.ServiceBusConnectionString);
        }

        /// <summary>
        /// Permissions this instance.
        /// </summary>
        /// <returns>Permissions.</returns>
        public IActionResult Permission()
        {
            return this.View();
        }

        /// <summary>
        /// Privacies this instance.
        /// </summary>
        /// <returns>Privacy.</returns>
        public IActionResult Privacy()
        {
            return this.View();
        }

        /// <summary>
        /// Errors this instance.
        /// </summary>
        /// <returns>Error.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return this.View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Gets the document.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="storageAccountUrl">The storage account URL.</param>
        /// <param name="storageAccountKey">The storage account key.</param>
        /// <returns>Document.</returns>
        private static string GetDocument(string container, string resourceName, string storageAccountUrl, string storageAccountKey)
        {
            PlanB.Butler.Admin.Util.BackendCommunication backendcom = new PlanB.Butler.Admin.Util.BackendCommunication();
            string taskUrl = backendcom.GetDocument(container, resourceName, storageAccountUrl, storageAccountKey);
            return taskUrl;
        }

        /// <summary>
        /// Puts the document.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="body">The body.</param>
        /// <param name="serviceBusConnectionString">The service bus connection string.</param>
        /// <returns>
        /// HttpStatusCode.
        /// </returns>
        private static HttpStatusCode PutDocument(string container, string resourceName, string body, string serviceBusConnectionString)
        {
            PlanB.Butler.Admin.Util.BackendCommunication backendcom = new PlanB.Butler.Admin.Util.BackendCommunication();
            HttpStatusCode taskUrl = backendcom.PutDocument(container, resourceName, body, "q.planbutler", serviceBusConnectionString);
            return taskUrl;
        }
    }
}

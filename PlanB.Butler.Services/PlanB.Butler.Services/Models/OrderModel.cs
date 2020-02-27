// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace PlanB.Butler.Services.Models
{
    /// <summary>
    /// OrderModel.
    /// </summary>
    public class OrderModel
    {
        /// <summary>
        /// Gets or sets the company status Enum.
        /// </summary>
        public enum OrderRelationship
        {
            /// <summary>
            /// External.
            /// </summary>
            External = 1,

            /// <summary>
            /// Internal.
            /// </summary>
            Internal = 2,

            /// <summary>
            /// Client.
            /// </summary>
            Client = 3,

            /// <summary>
            /// Intership.
            /// </summary>
            Intership = 4,
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public OrderRelationship Relationship { get; set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>
        /// The date.
        /// </value>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the company.
        /// </summary>
        /// <value>
        /// The name of the company.
        /// </value>
        public string CompanyName { get; set; }

        /// <summary>
        /// Gets or sets the restaurant.
        /// </summary>
        /// <value>
        /// The restaurant.
        /// </value>
        public string Restaurant { get; set; }

        /// <summary>
        /// Gets or sets the meal.
        /// </summary>
        /// <value>
        /// The meal.
        /// </value>
        public string Meal { get; set; }

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        /// <value>
        /// The price.
        /// </value>
        public double Price { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the benefit.
        /// </summary>
        /// <value>
        /// The benefit.
        /// </value>
        public double Benefit { get; set; }

        //public OrderModel(string companyStatus, DateTime date, string name, string companyName, string restaurant, string meal, double price, int quantity, double benefit)
        //{
        //    Dictionary<string, OrderRelationship> lookUpCompanyStatus = new Dictionary<string, OrderRelationship>();
        //    lookUpCompanyStatus.Add("Extern", OrderRelationship.External);
        //    lookUpCompanyStatus.Add("Intern", OrderRelationship.Internal);
        //    lookUpCompanyStatus.Add("Kunde", OrderRelationship.Client);
        //    lookUpCompanyStatus.Add("Praktikant", OrderRelationship.Intership);

        //    OrderRelationship selected = lookUpCompanyStatus[companyStatus];
        //    this.Relationship = selected;
        //    this.Date = date;
        //    this.Name = name;
        //    this.CompanyName = companyName;
        //    this.Restaurant = restaurant;
        //    this.Meal = meal;
        //    this.Price = price;
        //    this.Quantity = quantity;
        //    this.Benefit = benefit;
        //}
    }
}

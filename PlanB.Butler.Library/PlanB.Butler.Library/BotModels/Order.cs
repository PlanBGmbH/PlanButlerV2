namespace BotLibraryV2
{
    using System;

    /// <summary>
    /// This is our application state. Just a regular serializable .NET class.
    /// </summary>
    public class Order
    {
        public DateTime Date { get; set; }

        public string CompanyStatus { get; set; }

        public string Name { get; set; }

        public string CompanyName { get; set; }

        public string Restaurant { get; set; }

        public string Meal { get; set; }

        public double Price { get; set; }

        [Obsolete("Please use 'Quantity'")]
        public int Quantaty { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        public int Quantity { get; set; }

        public double Grand { get; set; }
    }
}

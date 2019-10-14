namespace ButlerBot
{
    /// <summary>
    /// This is our application state. Just a regular serializable .NET class.
    /// </summary>
    public class Order
    {
        public string Name { get; set; }

        public string Restaurant { get; set; }

        public string Food { get; set; }

        public int Price { get; set; }
    }
}

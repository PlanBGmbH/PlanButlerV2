namespace BotLibraryV2
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This is our application state. Just a regular serializable .NET class.
    /// </summary>
    public class Person
    {
        public string Name { get; set; }
        public List<Order> Orders { get; set; }
    }
}

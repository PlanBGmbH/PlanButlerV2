using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlanB.Butler.Admin.Models
{
    public class PlanModel
    {
        public string name { get; set; }
        public string restaurant1 { get; set; }
        public List<Food> meal1 { get; set; }
        public string restaurant2 { get; set; }
        public List<Food> meal2 { get; set; }
    }
}

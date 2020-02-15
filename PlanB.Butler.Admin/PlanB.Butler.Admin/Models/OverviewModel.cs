using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlanButlerAdmin.Models
{
    public class OverviewModel
    {
        public string title { get; set; }
        public List<PlanModel> planday { get; set; }
    }
}

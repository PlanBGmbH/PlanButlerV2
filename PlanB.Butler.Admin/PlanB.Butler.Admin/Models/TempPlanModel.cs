using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlanB.Butler.Admin.Models
{
    public class TempPlanModel
    {

        public string restaurant { get; set; }
        //public string pictureB64 { get; set; }
        MealModel meals { get; set; }
    }
}

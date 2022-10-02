using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace tabseetCourse.Models
{
    public class StoreProductModel
    {
        public int ID { get; set; }
        public string name { get; set; }
        public int Category_ID { get; set; }
        public string categoryName { get; set; }

        [AllowHtml]
        public string Description { get; set; }
        public float Price { get; set; }
        public List<string> Images { get; set; }
        public List<HttpPostedFileBase> LPFB { get; set; }
    }
}
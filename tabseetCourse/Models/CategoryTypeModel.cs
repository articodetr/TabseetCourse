using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models
{
    public class CategoryTypeModel
    {
        public int ID { get; set; }
        public string NameAR { get; set; }
        public string NameEN { get; set; }
        public int ParentID { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models_UI
{
    public class Lang_Model
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Direction { get; set; }
        public string DirectionName { get; set; }
        public int? DefultLang { get; set; }
    }
}
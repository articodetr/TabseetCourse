using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models_UI
{
    public class GridpageContentModel
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public HttpPostedFileBase ImageFile { get; set; }
        public string DocLinke { get; set; }
        public int GridPageID { get; set; }

        public int PayValue { get; set; }
        public int Is_reg { get; set; }
        
    }
}
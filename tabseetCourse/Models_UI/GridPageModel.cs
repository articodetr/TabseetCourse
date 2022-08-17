using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models_UI
{
    public class GridPageModel
    {
        public int ID { get; set; }
        public int Lang_ID { get; set; }
        public string Lang_Name { get; set; }
        public int Pages_ID { get; set; }
        public string Title { get; set; }
        public string Image_Cover { get; set; }

        public int ContentBtnReg { get; set; }
        public HttpPostedFileBase Image_CoverFile { get; set; }

        public List<GridpageContentModel> GirdContent { get; set; }
    }
}
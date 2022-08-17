using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models
{
    public class ChatTableModel
    {
        public string Message { get; set; }
        public int Category_ID { get; set; }
        public string User_ID { get; set; }
        public string username { get; set; }
        public string date_Time { get; set; }
    }
}
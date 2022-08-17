using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models
{
    public class Q_TrueOrFolseModel
    {
        public int ID { get; set; }
        public int Question_ID { get; set; }
        public int TrueValue { get; set; }
        public double Degry { get; set; }

        public QuestionsModel Question { get; set; }
    }
}
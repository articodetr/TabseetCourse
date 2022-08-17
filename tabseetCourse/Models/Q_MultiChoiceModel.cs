using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models
{
    public class Q_MultiChoiceModel
    {
        public int ID { get; set; }
        public int Question_ID { get; set; }
        public string Choose { get; set; }
        public double Degry { get; set; }

        public  QuestionsModel Question { get; set; }
    }
}
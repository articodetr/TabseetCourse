using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models
{
    public class Exam_Q_Model
    {
        public int ID { get; set; }
        public int Q_ID { get; set; }
        public int Exam_ID { get; set; }
        public QuestionsModel QM { get; set; }
    }
}
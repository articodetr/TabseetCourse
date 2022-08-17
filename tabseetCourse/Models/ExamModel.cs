using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models
{
    public class ExamModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Day_Time { get; set; }
        public string Start_Time { get; set; }
        public float Duration { get; set; }
        public int Category_ID { get; set; } 
        public float Exam_Degry { get; set; }
        public float Exam_win_Degry { get; set; }
        public int CanStartExam { get; set; }
        public virtual List<Exam_Q_Model> Exam_Q { get; set; }
         
    }
}
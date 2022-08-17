using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models
{
    public class AssignmentModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Lecture_ID { get; set; }
        public float Assignmet_Degry { get; set; }
        public int Assignmet_degry_Win { get; set; }
        public  LectureModel Lecture { get; set; }
        
        public List<Assignment_Q_model> L_Assignment_Q { get; set; }
    }
}
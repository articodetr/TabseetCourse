using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models
{
    public class QuestionsModel
    {
        public int ID { get; set; }
        public int Type { get; set; }
        public string Q_Name { get; set; }
        public string Q_Text { get; set; }
        public string Q_Description { get; set; }

        public int Category_ID { get; set; }
        public string Category_Name { get; set; }
        //public virtual List<Assignment_Q> Assignment_Q { get; set; }

        //public virtual List<Exam_Q> Exam_Q { get; set; }

        //public virtual List<Q_FillVoid> Q_FillVoid { get; set; }
        
        public int Q_Degry { get; set; }
        //public Q_MatchingModel Q_Matching { get; set; }

        public List<Q_MultiChoiceModel> Q_MultiChoice { get; set; }

        public  Q_TrueOrFolseModel Q_TrueOrFalse { get; set; }
        public List<Q_MatchingModel> Q_MatchingChoise { get; set; }
    }
}
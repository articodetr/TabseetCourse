using System;
using System.Collections.Generic;

namespace tabseetCourse.Models
{
    public class StudentSubjectItemModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string TeacherName { get; set; }
        public int LectureCount { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public List<LectureModel> Lectures { get; set; }

        public StudentSubjectItemModel()
        {
            Lectures = new List<LectureModel>();
        }
    }
}


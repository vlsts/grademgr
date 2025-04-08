using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class CreateCourseRequest
{
    [Required(ErrorMessage = "Course name is required")]
    public string CourseName { get; set; }

    [Required(ErrorMessage = "Course code is required")]
    public string CourseCode { get; set; }
    public string Description { get; set; }
}

public class CourseDetailResponse
{
    public string Id { get; set; }
    public string CourseName { get; set; }
    public string CourseCode { get; set; }
    public string Description { get; set; }
    public string TeacherEmail { get; set; }
    public string TeacherName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<StudentInfo> Students { get; set; } = new List<StudentInfo>();
}

public class StudentInfo
{
    public string Username { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
}

public class AddStudentRequest
{
    [Required(ErrorMessage = "Student email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    public string StudentEmail { get; set; }
}
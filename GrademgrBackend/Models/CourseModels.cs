using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Models related to course management functionality.
/// </summary>

/// <summary>
/// Represents a request to create a new course.
/// </summary>
public class CreateCourseRequest
{
    /// <summary>
    /// Gets or sets the name of the course.
    /// </summary>
    [Required(ErrorMessage = "Course name is required")]
    public string CourseName { get; set; }

    /// <summary>
    /// Gets or sets the course code.
    /// </summary>
    [Required(ErrorMessage = "Course code is required")]
    public string CourseCode { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the course.
    /// </summary>
    public string Description { get; set; }
}

/// <summary>
/// Represents a detailed course response returned to clients.
/// </summary>
public class CourseDetailResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the course.
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the course.
    /// </summary>
    public string CourseName { get; set; }
    
    /// <summary>
    /// Gets or sets the course code.
    /// </summary>
    public string CourseCode { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the course.
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the email address of the teacher responsible for the course.
    /// </summary>
    public string TeacherEmail { get; set; }
    
    /// <summary>
    /// Gets or sets the full name of the teacher responsible for the course.
    /// </summary>
    public string TeacherName { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the course was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the list of students enrolled in the course.
    /// </summary>
    public List<StudentInfo> Students { get; set; } = new List<StudentInfo>();
}

/// <summary>
/// Represents information about a student enrolled in a course.
/// </summary>
public class StudentInfo
{
    /// <summary>
    /// Gets or sets the username of the student.
    /// </summary>
    public string Username { get; set; }
    
    /// <summary>
    /// Gets or sets the full name of the student.
    /// </summary>
    public string FullName { get; set; }
    
    /// <summary>
    /// Gets or sets the email address of the student.
    /// </summary>
    public string Email { get; set; }
}

/// <summary>
/// Represents a request to add a student to a course.
/// </summary>
public class AddStudentRequest
{
    /// <summary>
    /// Gets or sets the email address of the student to add to the course.
    /// </summary>
    [Required(ErrorMessage = "Student email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    public string StudentEmail { get; set; }
}

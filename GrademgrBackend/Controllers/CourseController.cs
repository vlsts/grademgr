using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CourseController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CourseController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    // Helper method to get current user email from token
    private string GetUserEmail() => User.FindFirst(ClaimTypes.Email)?.Value;

    // Helper method to get current user role from token
    private UserRole GetUserRole()
    {
        var roleString = User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.Parse<UserRole>(roleString);
    }

    // Helper method to validate and return model state errors
    private Dictionary<string, string[]> GetModelStateErrors()
    {
        return ModelState
            .Where(x => x.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );
    }

    // Check if model state is valid, return BadRequest if not
    private IActionResult ValidateModel()
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { 
                message = "Invalid input", 
                errors = GetModelStateErrors() 
            });
        }
        return null;
    }

    // Teacher Endpoints
    [HttpGet("teacher")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> GetTeacherCourses()
    {
        var teacherEmail = GetUserEmail();
        var courses = await _courseService.GetTeacherCoursesAsync(teacherEmail);
        return Ok(courses);
    }

    [HttpPost("create")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
    {
        // Validate model
        var validationResult = ValidateModel();
        if (validationResult != null)
            return validationResult;
            
        // Additional validations
        if (string.IsNullOrWhiteSpace(request.CourseName))
            return BadRequest(new { message = "Course name is required" });
            
        if (string.IsNullOrWhiteSpace(request.CourseCode))
            return BadRequest(new { message = "Course code is required" });

        var teacherEmail = GetUserEmail();
        try
        {
            var course = await _courseService.CreateCourseAsync(request, teacherEmail);
            return CreatedAtAction(nameof(GetCourseDetails), new { id = course.Id }, course);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> DeleteCourse(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { message = "Course ID is required" });

        var teacherEmail = GetUserEmail();
        var result = await _courseService.DeleteCourseAsync(id, teacherEmail);
        
        if (!result)
            return NotFound(new { message = "Course not found or you don't have permission to delete it" });
            
        return NoContent();
    }

    [HttpPost("{courseId}/students")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> AddStudentToCourse(string courseId, [FromBody] AddStudentRequest request)
    {
        // Validate model
        var validationResult = ValidateModel();
        if (validationResult != null)
            return validationResult;
            
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Course ID is required" });
            
        if (string.IsNullOrWhiteSpace(request.StudentEmail) || !IsValidEmail(request.StudentEmail))
            return BadRequest(new { message = "Valid student email is required" });

        var teacherEmail = GetUserEmail();
        var result = await _courseService.AddStudentToCourseAsync(courseId, request.StudentEmail, teacherEmail);
        
        if (!result)
            return BadRequest(new { message = "Failed to add student to course" });
            
        return Ok(new { message = "Student added to course" });
    }

    [HttpDelete("{courseId}/students/{studentEmail}")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> RemoveStudentFromCourse(string courseId, string studentEmail)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Course ID is required" });
            
        if (string.IsNullOrWhiteSpace(studentEmail) || !IsValidEmail(studentEmail))
            return BadRequest(new { message = "Valid student email is required" });

        var teacherEmail = GetUserEmail();
        var result = await _courseService.RemoveStudentFromCourseAsync(courseId, studentEmail, teacherEmail);
        
        if (!result)
            return BadRequest(new { message = "Failed to remove student from course" });
            
        return Ok(new { message = "Student removed from course" });
    }

    // Student Endpoints
    [HttpGet("student")]
    [Authorize(Policy = "RequireStudentRole")]
    public async Task<IActionResult> GetStudentCourses()
    {
        var studentEmail = GetUserEmail();
        var courses = await _courseService.GetStudentCoursesAsync(studentEmail);
        return Ok(courses);
    }

    // Common Endpoints
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetCourseDetails(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { message = "Course ID is required" });

        var userEmail = GetUserEmail();
        var userRole = GetUserRole();
        
        var courseDetails = await _courseService.GetCourseDetailsAsync(id, userEmail, userRole);
        
        if (courseDetails == null)
            return NotFound(new { message = "Course not found or you don't have access" });
            
        return Ok(courseDetails);
    }

    // teacher: add student grade to course
    [HttpPost("{courseId}/grades")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> AddGradeToCourse(string courseId, [FromBody] AddGradeRequest request)
    {
        // Validate model
        var validationResult = ValidateModel();
        if (validationResult != null)
            return validationResult;
            
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Course ID is required" });
            
        if (string.IsNullOrWhiteSpace(request.StudentMail) || !IsValidEmail(request.StudentMail))
            return BadRequest(new { message = "Valid student email is required" });
            
        if (string.IsNullOrWhiteSpace(request.AssignmentName))
            return BadRequest(new { message = "Assignment name is required" });
            
        if (request.GradeValue < 0 || request.GradeValue > 100)
            return BadRequest(new { message = "Grade must be between 0 and 100" });

        var teacherEmail = GetUserEmail();
        var studentEmail = request.StudentMail;

        var result = await _courseService.AddGradeToCourseAsync(courseId, studentEmail, request, teacherEmail);
        
        if (!result)
            return BadRequest(new { message = "Failed to add grade to course" });
            
        return Ok(new { message = "Grade added to course" });
    }

    // teacher: add multiple student grades to course at once
    [HttpPost("{courseId}/grades/bulk")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> AddBulkGradesToCourse(string courseId, [FromBody] List<AddGradeRequest> requests)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Course ID is required" });
            
        if (requests == null || !requests.Any())
            return BadRequest(new { message = "No grades provided" });
        
        // Validate each grade request
        var invalidGrades = new List<(int index, string message)>();
        for (int i = 0; i < requests.Count; i++)
        {
            var request = requests[i];
            if (string.IsNullOrWhiteSpace(request.StudentMail) || !IsValidEmail(request.StudentMail))
                invalidGrades.Add((i, "Valid student email is required"));
                
            if (string.IsNullOrWhiteSpace(request.AssignmentName))
                invalidGrades.Add((i, "Assignment name is required"));
                
            if (request.GradeValue < 0 || request.GradeValue > 100)
                invalidGrades.Add((i, "Grade must be between 0 and 100"));
        }
        
        if (invalidGrades.Any())
            return BadRequest(new { 
                message = "Invalid grades found", 
                invalidEntries = invalidGrades.Select(g => new { 
                    index = g.index, 
                    error = g.message 
                })
            });
        
        var teacherEmail = GetUserEmail();
        var results = await _courseService.AddBulkGradesToCourseAsync(courseId, requests, teacherEmail);
        
        if (results.All(r => !r))
            return BadRequest(new { message = "Failed to add any grades to course" });
        
        int successCount = results.Count(r => r);
        int failCount = results.Count(r => !r);
        
        if (failCount == 0)
        {
            return Ok(new { 
                message = $"Added {successCount} grades successfully", 
                success = successCount
            });
        }
        else
        {
            return Ok(new { 
                message = $"Added {successCount} grades successfully, {failCount} failed", 
                success = successCount, 
                failed = failCount 
            });
        }
    }

    // teacher: get all student grades for course
    [HttpGet("{courseId}/grades")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> GetGradesForCourse(string courseId)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Course ID is required" });

        var teacherEmail = GetUserEmail();
        var grades = await _courseService.GetGradesForCourseAsync(courseId, teacherEmail);
        
        if (grades == null || !grades.Any())
            return NotFound(new { message = "No grades found for this course" });
            
        return Ok(grades);
    }

    // teacher: delete student grade from course
    [HttpDelete("{courseId}/grades/{gradeId}")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> DeleteGradeFromCourse(string courseId, string gradeId)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Course ID is required" });
            
        if (string.IsNullOrWhiteSpace(gradeId))
            return BadRequest(new { message = "Grade ID is required" });

        var teacherEmail = GetUserEmail();
        var result = await _courseService.DeleteGradeFromCourseAsync(courseId, gradeId, teacherEmail);
        
        if (!result)
            return NotFound(new { message = "Grade not found or you don't have permission to delete it" });
            
        return NoContent();
    }

    // student: get all grades for course
    [HttpGet("{courseId}/grades/student")]
    [Authorize(Policy = "RequireStudentRole")]
    public async Task<IActionResult> GetGradesForCourseAsStudent(string courseId)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Course ID is required" });

        var studentEmail = GetUserEmail();
        var grades = await _courseService.GetGradesForCourseAsStudentAsync(courseId, studentEmail);
        
        if (grades == null || !grades.Any())
            return NotFound(new { message = "No grades found for this course" });
            
        return Ok(grades);
    }

    // student: get all grades for all courses
    [HttpGet("grades")]
    [Authorize(Policy = "RequireStudentRole")]
    public async Task<IActionResult> GetGradesForStudent()
    {
        var studentEmail = GetUserEmail();
        var grades = await _courseService.GetGradesForStudentAsync(studentEmail);
        
        if (grades == null || !grades.Any())
            return NotFound(new { message = "No grades found for this student" });
            
        return Ok(grades);
    }

    // student: get all courses - renamed to avoid confusion with the route parameter
    [HttpGet("student/courses")]
    [Authorize(Policy = "RequireStudentRole")]
    public async Task<IActionResult> GetCoursesForStudent()
    {
        var studentEmail = GetUserEmail();
        var courses = await _courseService.GetStudentCoursesAsync(studentEmail);
        
        if (courses == null || !courses.Any())
            return NotFound(new { message = "No courses found for this student" });
            
        return Ok(courses);
    }

    // Helper method for email validation
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

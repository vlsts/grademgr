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
    private readonly ISecurityService _securityService;

    public CourseController(ICourseService courseService, ISecurityService securityService)
    {
        _courseService = courseService;
        _securityService = securityService;
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
        var teacherEmail = _securityService.SanitizeEmail(GetUserEmail());
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
            
        // Check for scripting patterns
        if (_securityService.ContainsScriptingPattern(request.CourseName) || 
            _securityService.ContainsScriptingPattern(request.CourseCode) ||
            _securityService.ContainsScriptingPattern(request.Description))
        {
            return BadRequest(new { message = "Invalid input detected" });
        }
            
        // Sanitize inputs
        request.CourseName = _securityService.SanitizeString(request.CourseName);
        request.CourseCode = _securityService.SanitizeString(request.CourseCode);
        request.Description = _securityService.SanitizeString(request.Description);
            
        // Additional validations
        if (string.IsNullOrWhiteSpace(request.CourseName))
            return BadRequest(new { message = "Course name is required" });
            
        if (string.IsNullOrWhiteSpace(request.CourseCode))
            return BadRequest(new { message = "Course code is required" });

        var teacherEmail = _securityService.SanitizeEmail(GetUserEmail());
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
        id = _securityService.SanitizeMongoId(id);
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { message = "Invalid course ID format" });

        var teacherEmail = _securityService.SanitizeEmail(GetUserEmail());
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
            
        // Sanitize inputs
        courseId = _securityService.SanitizeMongoId(courseId);
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Invalid course ID format" });
            
        // Check for scripting patterns
        if (_securityService.ContainsScriptingPattern(request.StudentEmail))
            return BadRequest(new { message = "Invalid input detected" });
            
        request.StudentEmail = _securityService.SanitizeEmail(request.StudentEmail);
        if (string.IsNullOrWhiteSpace(request.StudentEmail) || !_securityService.IsValidEmail(request.StudentEmail))
            return BadRequest(new { message = "Valid student email is required" });

        var teacherEmail = _securityService.SanitizeEmail(GetUserEmail());
        var result = await _courseService.AddStudentToCourseAsync(courseId, request.StudentEmail, teacherEmail);
        
        if (!result)
            return BadRequest(new { message = "Failed to add student to course" });
            
        return Ok(new { message = "Student added to course" });
    }

    [HttpDelete("{courseId}/students/{studentEmail}")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> RemoveStudentFromCourse(string courseId, string studentEmail)
    {
        // Sanitize inputs
        courseId = _securityService.SanitizeMongoId(courseId);
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Invalid course ID format" });
            
        // Check for scripting patterns
        if (_securityService.ContainsScriptingPattern(studentEmail))
            return BadRequest(new { message = "Invalid input detected" });
            
        studentEmail = _securityService.SanitizeEmail(studentEmail);
        if (string.IsNullOrWhiteSpace(studentEmail) || !_securityService.IsValidEmail(studentEmail))
            return BadRequest(new { message = "Valid student email is required" });

        var teacherEmail = _securityService.SanitizeEmail(GetUserEmail());
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
        var studentEmail = _securityService.SanitizeEmail(GetUserEmail());
        var courses = await _courseService.GetStudentCoursesAsync(studentEmail);
        return Ok(courses);
    }

    // Common Endpoints
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetCourseDetails(string id)
    {
        id = _securityService.SanitizeMongoId(id);
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { message = "Invalid course ID format" });

        var userEmail = _securityService.SanitizeEmail(GetUserEmail());
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
            
        // Sanitize inputs
        courseId = _securityService.SanitizeMongoId(courseId);
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Invalid course ID format" });
            
        // Check for scripting patterns
        if (_securityService.ContainsScriptingPattern(request.StudentMail) || 
            _securityService.ContainsScriptingPattern(request.AssignmentName))
            return BadRequest(new { message = "Invalid input detected" });
            
        request.StudentMail = _securityService.SanitizeEmail(request.StudentMail);
        if (string.IsNullOrWhiteSpace(request.StudentMail) || !_securityService.IsValidEmail(request.StudentMail))
            return BadRequest(new { message = "Valid student email is required" });
            
        request.AssignmentName = _securityService.SanitizeString(request.AssignmentName);
        if (string.IsNullOrWhiteSpace(request.AssignmentName))
            return BadRequest(new { message = "Assignment name is required" });
            
        if (request.GradeValue < 0 || request.GradeValue > 100)
            return BadRequest(new { message = "Grade must be between 0 and 100" });

        var teacherEmail = _securityService.SanitizeEmail(GetUserEmail());
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
        // Sanitize inputs
        courseId = _securityService.SanitizeMongoId(courseId);
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Invalid course ID format" });
            
        if (requests == null || !requests.Any())
            return BadRequest(new { message = "No grades provided" });
        
        // Validate and sanitize each grade request
        var invalidGrades = new List<(int index, string message)>();
        for (int i = 0; i < requests.Count; i++)
        {
            var request = requests[i];
            
            // Check for scripting patterns
            if (_securityService.ContainsScriptingPattern(request.StudentMail) || 
                _securityService.ContainsScriptingPattern(request.AssignmentName))
            {
                invalidGrades.Add((i, "Invalid input detected"));
                continue;
            }
            
            // Sanitize and validate inputs
            request.StudentMail = _securityService.SanitizeEmail(request.StudentMail);
            if (string.IsNullOrWhiteSpace(request.StudentMail) || !_securityService.IsValidEmail(request.StudentMail))
                invalidGrades.Add((i, "Valid student email is required"));
                
            request.AssignmentName = _securityService.SanitizeString(request.AssignmentName);
            if (string.IsNullOrWhiteSpace(request.AssignmentName))
                invalidGrades.Add((i, "Assignment name is required"));
                
            if (request.GradeValue < 0 || request.GradeValue > 100)
                invalidGrades.Add((i, "Grade must be between 0 and 100"));
            
            // Update the sanitized request in the list
            requests[i] = request;
        }
        
        if (invalidGrades.Any())
            return BadRequest(new { 
                message = "Invalid grades found", 
                invalidEntries = invalidGrades.Select(g => new { 
                    index = g.index, 
                    error = g.message 
                })
            });
        
        var teacherEmail = _securityService.SanitizeEmail(GetUserEmail());
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
        courseId = _securityService.SanitizeMongoId(courseId);
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Invalid course ID format" });

        var teacherEmail = _securityService.SanitizeEmail(GetUserEmail());
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
        courseId = _securityService.SanitizeMongoId(courseId);
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Invalid course ID format" });
            
        gradeId = _securityService.SanitizeMongoId(gradeId);
        if (string.IsNullOrWhiteSpace(gradeId))
            return BadRequest(new { message = "Invalid grade ID format" });

        var teacherEmail = _securityService.SanitizeEmail(GetUserEmail());
        var result = await _courseService.DeleteGradeFromCourseAsync(courseId, gradeId, teacherEmail);
        
        if (!result)
            return NotFound(new { message = "Grade not found or you don't have permission to delete it" });
            
        return NoContent();
    }

    // teacher: update a student grade for a course
    [HttpPut("{courseId}/grades/{gradeId}")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> UpdateGradeInCourse(string courseId, string gradeId, [FromBody] UpdateGradeRequest request)
    {
        // Validate model
        var validationResult = ValidateModel();
        if (validationResult != null)
            return validationResult;
            
        // Sanitize inputs
        courseId = _securityService.SanitizeMongoId(courseId);
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Invalid course ID format" });
            
        gradeId = _securityService.SanitizeMongoId(gradeId);
        if (string.IsNullOrWhiteSpace(gradeId))
            return BadRequest(new { message = "Invalid grade ID format" });
            
        // Check for scripting patterns
        if (_securityService.ContainsScriptingPattern(request.AssignmentName) ||
            _securityService.ContainsScriptingPattern(request.ChangeReason))
            return BadRequest(new { message = "Invalid input detected" });
            
        // Sanitize and validate inputs
        request.AssignmentName = _securityService.SanitizeString(request.AssignmentName);
        if (string.IsNullOrWhiteSpace(request.AssignmentName))
            return BadRequest(new { message = "Assignment name is required" });
            
        request.ChangeReason = _securityService.SanitizeString(request.ChangeReason);
        
        if (request.GradeValue < 0 || request.GradeValue > 100)
            return BadRequest(new { message = "Grade must be between 0 and 100" });

        var teacherEmail = _securityService.SanitizeEmail(GetUserEmail());
        var result = await _courseService.UpdateGradeInCourseAsync(courseId, gradeId, request, teacherEmail);
        
        if (!result)
            return BadRequest(new { message = "Failed to update grade. Grade not found or you don't have permission." });
            
        return Ok(new { message = "Grade updated successfully" });
    }

    // student: get all grades for course
    [HttpGet("{courseId}/grades/student")]
    [Authorize(Policy = "RequireStudentRole")]
    public async Task<IActionResult> GetGradesForCourseAsStudent(string courseId)
    {
        courseId = _securityService.SanitizeMongoId(courseId);
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "Invalid course ID format" });

        var studentEmail = _securityService.SanitizeEmail(GetUserEmail());
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
        var studentEmail = _securityService.SanitizeEmail(GetUserEmail());
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
        var studentEmail = _securityService.SanitizeEmail(GetUserEmail());
        var courses = await _courseService.GetStudentCoursesAsync(studentEmail);
        
        if (courses == null || !courses.Any())
            return NotFound(new { message = "No courses found for this student" });
            
        return Ok(courses);
    }
}

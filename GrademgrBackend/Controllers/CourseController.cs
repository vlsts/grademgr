using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Controller responsible for handling course-related operations for teachers and students.
/// Provides endpoints for course management, student enrollment, and grade management.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CourseController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ISecurityService _securityService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CourseController"/> class.
    /// </summary>
    /// <param name="courseService">The service for handling course operations.</param>
    /// <param name="securityService">The service for handling security operations.</param>
    public CourseController(ICourseService courseService, ISecurityService securityService)
    {
        _courseService = courseService;
        _securityService = securityService;
    }

    /// <summary>
    /// Extracts the current user's email from their JWT token claims.
    /// </summary>
    /// <returns>The email address of the authenticated user.</returns>
    private string GetUserEmail() => User.FindFirst(ClaimTypes.Email)?.Value;

    /// <summary>
    /// Extracts the current user's role from their JWT token claims.
    /// </summary>
    /// <returns>The role of the authenticated user as a UserRole enum.</returns>
    private UserRole GetUserRole()
    {
        var roleString = User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.Parse<UserRole>(roleString);
    }

    /// <summary>
    /// Extracts validation errors from the ModelState.
    /// </summary>
    /// <returns>A dictionary of field names and their corresponding error messages.</returns>
    private Dictionary<string, string[]> GetModelStateErrors()
    {
        return ModelState
            .Where(x => x.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );
    }

    /// <summary>
    /// Validates the model state and returns an appropriate response if invalid.
    /// </summary>
    /// <returns>A BadRequest result with validation errors if the model is invalid; otherwise, null.</returns>
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

    /// <summary>
    /// Retrieves all courses taught by the authenticated teacher.
    /// </summary>
    /// <returns>A list of courses taught by the teacher.</returns>
    /// <response code="200">Returns the list of courses.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a teacher.</response>
    [HttpGet("teacher")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> GetTeacherCourses()
    {
        var teacherEmail = _securityService.SanitizeEmail(GetUserEmail());
        var courses = await _courseService.GetTeacherCoursesAsync(teacherEmail);
        return Ok(courses);
    }

    /// <summary>
    /// Creates a new course with the authenticated teacher as the instructor.
    /// </summary>
    /// <param name="request">The course creation request containing course details.</param>
    /// <returns>The newly created course.</returns>
    /// <response code="201">Returns the newly created course.</response>
    /// <response code="400">If the request is invalid or contains malicious content.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a teacher.</response>
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

    /// <summary>
    /// Deletes a course and all related grades from the system.
    /// </summary>
    /// <param name="id">The identifier of the course to delete.</param>
    /// <returns>A response indicating success or failure.</returns>
    /// <response code="204">If the course was successfully deleted.</response>
    /// <response code="400">If the course ID format is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a teacher.</response>
    /// <response code="404">If the course was not found or the user doesn't have permission.</response>
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

    /// <summary>
    /// Adds a student to a course's enrollment list.
    /// </summary>
    /// <param name="courseId">The identifier of the course.</param>
    /// <param name="request">The request containing the student's email to add.</param>
    /// <returns>A response indicating success or failure.</returns>
    /// <response code="200">If the student was successfully added to the course.</response>
    /// <response code="400">If the request is invalid, the course ID format is invalid, or the operation failed.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a teacher.</response>
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

    /// <summary>
    /// Removes a student from a course's enrollment list and deletes their associated grades.
    /// </summary>
    /// <param name="courseId">The identifier of the course.</param>
    /// <param name="studentEmail">The email address of the student to remove.</param>
    /// <returns>A response indicating success or failure.</returns>
    /// <response code="200">If the student was successfully removed from the course.</response>
    /// <response code="400">If the course ID or email format is invalid, or the operation failed.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a teacher.</response>
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

    /// <summary>
    /// Retrieves all courses in which the authenticated student is enrolled.
    /// </summary>
    /// <returns>A list of courses the student is enrolled in.</returns>
    /// <response code="200">Returns the list of courses.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a student.</response>
    [HttpGet("student")]
    [Authorize(Policy = "RequireStudentRole")]
    public async Task<IActionResult> GetStudentCourses()
    {
        var studentEmail = _securityService.SanitizeEmail(GetUserEmail());
        var courses = await _courseService.GetStudentCoursesAsync(studentEmail);
        return Ok(courses);
    }

    /// <summary>
    /// Retrieves detailed information about a course. Teachers get full details including student list,
    /// while students only get information for courses they're enrolled in.
    /// </summary>
    /// <param name="id">The identifier of the course.</param>
    /// <returns>Detailed information about the course.</returns>
    /// <response code="200">Returns the course details.</response>
    /// <response code="400">If the course ID format is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the course was not found or the user doesn't have access.</response>
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

    /// <summary>
    /// Adds a grade for a student in a specific course.
    /// </summary>
    /// <param name="courseId">The identifier of the course.</param>
    /// <param name="request">The grade information containing the student email, grade value, and assignment name.</param>
    /// <returns>A response indicating success or failure.</returns>
    /// <response code="200">If the grade was successfully added.</response>
    /// <response code="400">If the request is invalid, the course ID format is invalid, or the operation failed.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a teacher.</response>
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

    /// <summary>
    /// Adds multiple grades for students in a specific course in a single request.
    /// </summary>
    /// <param name="courseId">The identifier of the course.</param>
    /// <param name="requests">A list of grade requests containing student emails, grade values, and assignment names.</param>
    /// <returns>A response indicating success or failure for each grade request.</returns>
    /// <response code="200">If at least some grades were successfully added, with details on success/failure counts.</response>
    /// <response code="400">If the course ID format is invalid, no grades were provided, or all grade additions failed.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a teacher.</response>
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

    /// <summary>
    /// Retrieves all grades for a specific course with student information for the teacher's view.
    /// </summary>
    /// <param name="courseId">The identifier of the course.</param>
    /// <returns>A list of grades with student information for the specified course.</returns>
    /// <response code="200">Returns the list of grades.</response>
    /// <response code="400">If the course ID format is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a teacher.</response>
    /// <response code="404">If no grades were found for the course.</response>
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

    /// <summary>
    /// Deletes a grade from a course.
    /// </summary>
    /// <param name="courseId">The identifier of the course.</param>
    /// <param name="gradeId">The identifier of the grade to delete.</param>
    /// <returns>A response indicating success or failure.</returns>
    /// <response code="204">If the grade was successfully deleted.</response>
    /// <response code="400">If the course ID or grade ID format is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a teacher.</response>
    /// <response code="404">If the grade was not found or the user doesn't have permission.</response>
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

    /// <summary>
    /// Updates an existing grade in a course and creates a history record of the change.
    /// </summary>
    /// <param name="courseId">The identifier of the course.</param>
    /// <param name="gradeId">The identifier of the grade to update.</param>
    /// <param name="request">Updated grade information and reason for change.</param>
    /// <returns>A response indicating success or failure.</returns>
    /// <response code="200">If the grade was successfully updated.</response>
    /// <response code="400">If the request is invalid, the course ID or grade ID format is invalid, or the operation failed.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a teacher.</response>
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

    /// <summary>
    /// Retrieves all grades for a student in a specific course.
    /// </summary>
    /// <param name="courseId">The identifier of the course.</param>
    /// <returns>A list of grades for the authenticated student in the specified course.</returns>
    /// <response code="200">Returns the list of grades.</response>
    /// <response code="400">If the course ID format is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a student.</response>
    /// <response code="404">If no grades were found for this student in this course.</response>
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

    /// <summary>
    /// Retrieves all grades for the authenticated student across all enrolled courses.
    /// </summary>
    /// <returns>A list of detailed grade information for the student.</returns>
    /// <response code="200">Returns the list of grades.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a student.</response>
    /// <response code="404">If no grades were found for this student.</response>
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

    /// <summary>
    /// Retrieves all courses in which the authenticated student is enrolled.
    /// </summary>
    /// <returns>A list of courses the student is enrolled in.</returns>
    /// <response code="200">Returns the list of courses.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a student.</response>
    /// <response code="404">If no courses were found for this student.</response>
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

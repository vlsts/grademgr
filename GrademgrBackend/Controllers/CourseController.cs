using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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
        var userEmail = GetUserEmail();
        var userRole = GetUserRole();
        
        var courseDetails = await _courseService.GetCourseDetailsAsync(id, userEmail, userRole);
        
        if (courseDetails == null)
            return NotFound(new { message = "Course not found or you don't have access" });
            
        return Ok(courseDetails);
    }
}
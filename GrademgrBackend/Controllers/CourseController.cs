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

    // teacher: add student grade to course
    [HttpPost("{courseId}/grades")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> AddGradeToCourse(string courseId, [FromBody] AddGradeRequest request)
    {
        var teacherEmail = GetUserEmail();
        var studentEmail = request.StudentMail;

        var result = await _courseService.AddGradeToCourseAsync(courseId, studentEmail, request, teacherEmail);
        
        if (!result)
            return BadRequest(new { message = "Failed to add grade to course" });
            
        return Ok(new { message = "Grade added to course" });
    }

    // teacher: get all student grades for course
    [HttpGet("{courseId}/grades")]
    [Authorize(Policy = "RequireTeacherRole")]
    public async Task<IActionResult> GetGradesForCourse(string courseId)
    {
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
}

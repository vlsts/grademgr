using System.Collections.Generic;
using System.Threading.Tasks;

public interface ICourseService
{
    // Teacher endpoints
    Task<List<Course>> GetTeacherCoursesAsync(string teacherEmail);
    Task<Course> CreateCourseAsync(CreateCourseRequest request, string teacherEmail);
    Task<bool> DeleteCourseAsync(string courseId, string teacherEmail);
    Task<bool> AddStudentToCourseAsync(string courseId, string studentEmail, string teacherEmail);
    Task<bool> RemoveStudentFromCourseAsync(string courseId, string studentEmail, string teacherEmail);
    Task<CourseDetailResponse> GetCourseDetailsAsync(string courseId, string userEmail, UserRole role);
    
    // Student endpoints
    Task<List<Course>> GetStudentCoursesAsync(string studentEmail);
}
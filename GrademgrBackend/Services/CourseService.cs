using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

public class CourseService : ICourseService
{
    private readonly DatabaseContext _context;

    public CourseService(DatabaseContext context)
    {
        _context = context;
    }

    // Teacher methods
    public async Task<List<Course>> GetTeacherCoursesAsync(string teacherEmail)
    {
        var teacher = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == teacherEmail && u.Role == UserRole.Teacher);
        
        if (teacher == null)
            return new List<Course>();

        return await _context.Courses
            .Where(c => c.TeacherId == teacher.Id)
            .ToListAsync();
    }

    public async Task<Course> CreateCourseAsync(CreateCourseRequest request, string teacherEmail)
    {
        var teacher = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == teacherEmail && u.Role == UserRole.Teacher);
        
        if (teacher == null)
            throw new Exception("Teacher not found");

        var course = new Course
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CourseName = request.CourseName,
            CourseCode = request.CourseCode,
            Description = request.Description,
            TeacherId = teacher.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
        return course;
    }

    public async Task<bool> DeleteCourseAsync(string courseId, string teacherEmail)
    {
        var teacher = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == teacherEmail && u.Role == UserRole.Teacher);
        
        if (teacher == null)
            return false;

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacher.Id);

        if (course == null)
            return false;

        _context.Courses.Remove(course);
        
        // Also delete related grades
        var relatedGrades = await _context.Grades
            .Where(g => g.CourseId == courseId)
            .ToListAsync();
            
        _context.Grades.RemoveRange(relatedGrades);
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddStudentToCourseAsync(string courseId, string studentEmail, string teacherEmail)
    {
        var teacher = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == teacherEmail && u.Role == UserRole.Teacher);
        
        if (teacher == null)
            return false;

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacher.Id);

        if (course == null)
            return false;

        // Check if student exists
        var student = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == UserRole.Student);

        if (student == null)
            return false;

        // Check if student is already in course
        if (course.StudentIds.Contains(student.Id))
            return true;  // Already added

        course.StudentIds.Add(student.Id);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveStudentFromCourseAsync(string courseId, string studentEmail, string teacherEmail)
    {
        var teacher = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == teacherEmail && u.Role == UserRole.Teacher);
        
        if (teacher == null)
            return false;

        var student = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == UserRole.Student);
        
        if (student == null)
            return false;

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacher.Id);

        if (course == null)
            return false;

        if (!course.StudentIds.Contains(student.Id))
            return false;  // Student not in course

        course.StudentIds.Remove(student.Id);
        
        // Also remove related grades
        var relatedGrades = await _context.Grades
            .Where(g => g.CourseId == courseId && g.StudentId == student.Id)
            .ToListAsync();
            
        _context.Grades.RemoveRange(relatedGrades);
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CourseDetailResponse> GetCourseDetailsAsync(string courseId, string userEmail, UserRole role)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == userEmail);
        
        if (user == null)
            return null;

        Course course;
        
        if (role == UserRole.Teacher)
        {
            course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == user.Id);
        }
        else
        {
            course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId && c.StudentIds.Contains(user.Id));
        }

        if (course == null)
            return null;

        var teacher = await _context.Users.FirstOrDefaultAsync(u => u.Id == course.TeacherId);
        
        var response = new CourseDetailResponse
        {
            Id = course.Id,
            CourseName = course.CourseName,
            CourseCode = course.CourseCode,
            Description = course.Description,
            TeacherEmail = teacher?.Email ?? "Unknown",
            TeacherName = teacher?.FullName ?? "Unknown",
            CreatedAt = course.CreatedAt,
            Students = new List<StudentInfo>()
        };

        // Only include student details for teachers
        if (role == UserRole.Teacher && course.StudentIds.Any())
        {
            var studentIds = course.StudentIds;
            var students = await _context.Users
                .Where(u => studentIds.Contains(u.Id))
                .ToListAsync();

            response.Students = students.Select(s => new StudentInfo
            {
                Username = s.Username,
                FullName = s.FullName,
                Email = s.Email
            }).ToList();
        }

        return response;
    }

    // Student methods
    public async Task<List<Course>> GetStudentCoursesAsync(string studentEmail)
    {
        var student = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == UserRole.Student);
        
        if (student == null)
            return new List<Course>();

        return await _context.Courses
            .Where(c => c.StudentIds.Contains(student.Id))
            .ToListAsync();
    }
}
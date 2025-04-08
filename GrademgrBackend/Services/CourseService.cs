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
        // Find the student by email
        var student = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == UserRole.Student);
        
        if (student == null)
            return new List<Course>();
    
        // Find courses where the student's ID is in the StudentIds collection
        var courses = await _context.Courses
            .Where(c => c.StudentIds.Contains(student.Id))
            .ToListAsync();
        
        return courses;
    }

    // teacher: add student grade to course
    public async Task<bool> AddGradeToCourseAsync(string courseId, string studentEmail, GradeRequest request, string teacherEmail)
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

        // Check if student is in course
        if (!course.StudentIds.Contains(student.Id))
            return false;  // Student not in course

        var grade = new Grade
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CourseId = courseId,
            StudentId = student.Id,
            GradeValue = request.GradeValue,
            AssignmentName = request.AssignmentName,
            EnteredAt = DateTime.UtcNow,
            EnteredBy = teacher.Id
        };

        _context.Grades.Add(grade);
        await _context.SaveChangesAsync();
        return true;
    }

    // teacher: add bulk grades to course
    public async Task<List<bool>> AddBulkGradesToCourseAsync(string courseId, List<AddGradeRequest> requests, string teacherEmail)
    {
        var teacher = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == teacherEmail && u.Role == UserRole.Teacher);
        
        if (teacher == null)
            return requests.Select(_ => false).ToList();

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacher.Id);

        if (course == null)
            return requests.Select(_ => false).ToList();
        
        var results = new List<bool>();
        
        foreach (var request in requests)
        {
            try
            {
                var studentEmail = request.StudentMail;
                var student = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == UserRole.Student);
                
                if (student == null || !course.StudentIds.Contains(student.Id))
                {
                    results.Add(false);
                    continue;
                }
                
                var grade = new Grade
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    StudentId = student.Id,
                    CourseId = courseId,
                    GradeValue = request.GradeValue,
                    AssignmentName = request.AssignmentName,
                    EnteredAt = DateTime.UtcNow,
                    EnteredBy = teacher.Id
                };
                
                _context.Grades.Add(grade);
                results.Add(true);
            }
            catch
            {
                results.Add(false);
            }
        }
        
        await _context.SaveChangesAsync();
        return results;
    }

    // teacher: get all student grades for course

    public async Task<List<GradeWithStudentInfo>> GetGradesForCourseAsync(string courseId, string teacherEmail)
    {
        var teacher = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == teacherEmail && u.Role == UserRole.Teacher);
        
        if (teacher == null)
            return new List<GradeWithStudentInfo>();

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacher.Id);

        if (course == null)
            return new List<GradeWithStudentInfo>();

        var grades = await _context.Grades
            .Where(g => g.CourseId == courseId)
            .ToListAsync();
            
        var studentIds = grades.Select(g => g.StudentId).Distinct().ToList();
        var students = await _context.Users
            .Where(u => studentIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);
            
        return grades.Select(g => new GradeWithStudentInfo
        {
            Id = g.Id,
            CourseId = g.CourseId,
            StudentId = g.StudentId,
            StudentName = students.ContainsKey(g.StudentId) ? students[g.StudentId].FullName : "Unknown",
            StudentEmail = students.ContainsKey(g.StudentId) ? students[g.StudentId].Email : "Unknown",
            GradeValue = g.GradeValue,
            AssignmentName = g.AssignmentName,
            EnteredAt = g.EnteredAt,
            EnteredBy = g.EnteredBy
        }).ToList();
    }

    // teacher: delete grade from course

    public async Task<bool> DeleteGradeFromCourseAsync(string courseId, string gradeId, string teacherEmail)
    {
        var teacher = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == teacherEmail && u.Role == UserRole.Teacher);
        
        if (teacher == null)
            return false;

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacher.Id);

        if (course == null)
            return false;

        var grade = await _context.Grades
            .FirstOrDefaultAsync(g => g.Id == gradeId && g.CourseId == courseId);

        if (grade == null)
            return false;

        _context.Grades.Remove(grade);
        await _context.SaveChangesAsync();
        return true;
    }

    // student: get all grades for all courses

    public async Task<List<GradeDetailsDto>> GetGradesForStudentAsync(string studentEmail)
    {
        try
        {
            // Create a new list to hold the results
            var result = new List<GradeDetailsDto>();
            
            // Step 1: Get the student by email (wait for this to complete)
            var student = await _context.Users
                .AsNoTracking() // Use no tracking for read-only operations
                .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == UserRole.Student);
                
            if (student == null)
                return result; // Return empty list if student not found
                
            // Step 2: Get all courses for this student (wait for this to complete)
            var studentCourses = await _context.Courses
                .AsNoTracking()
                .Where(c => c.StudentIds.Contains(student.Id))
                .ToListAsync();
                
            if (!studentCourses.Any())
                return result; // Return empty list if no courses
                
            // Step 3: Get all courseIds (in memory operation, no DB query)
            var courseIds = studentCourses.Select(c => c.Id).ToList();
            
            // Step 4: Get all grades for this student across all their courses (wait for this to complete)
            var studentGrades = await _context.Grades
                .AsNoTracking()
                .Where(g => g.StudentId == student.Id && courseIds.Contains(g.CourseId))
                .ToListAsync();
                
            if (!studentGrades.Any())
                return result; // Return empty list if no grades
                
            // Step 5: Get teacher IDs (in memory operation, no DB query)
            var teacherIds = studentGrades
                .Select(g => g.EnteredBy)
                .Distinct()
                .ToList();
                
            // Step 6: Get all teachers in one query (wait for this to complete)
            var teachers = await _context.Users
                .AsNoTracking()
                .Where(u => teacherIds.Contains(u.Id) && u.Role == UserRole.Teacher)
                .ToDictionaryAsync(t => t.Id, t => t.FullName);
                
            // Step 7: Build the DTOs (in memory operation, no DB query)
            foreach (var grade in studentGrades)
            {
                var course = studentCourses.FirstOrDefault(c => c.Id == grade.CourseId);
                
                var dto = new GradeDetailsDto
                {
                    Id = grade.Id,
                    StudentId = student.Id,
                    StudentName = student.FullName,
                    StudentEmail = student.Email,
                    CourseId = grade.CourseId,
                    CourseCode = course?.CourseCode ?? "Unknown Course",
                    GradeValue = (double) grade.GradeValue,
                    AssignmentName = grade.AssignmentName,
                    EnteredAt = grade.EnteredAt,
                    EnteredBy = grade.EnteredBy,
                    // Look up teacher name from our dictionary, or use "Unknown" if not found
                    TeacherName = teachers.TryGetValue(grade.EnteredBy, out var teacherName) ? teacherName : "Unknown Teacher",
                    // TODO: comment
                    Comment = grade.AssignmentName
                };
                
                result.Add(dto);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            // Log the exception details for debugging
            System.Diagnostics.Debug.WriteLine($"Exception in GetGradesForStudentAsync: {ex}");
            throw; // Re-throw the exception after logging
        }
    }

    // student: get all grades for course

    public async Task<List<Grade>> GetGradesForCourseAsStudentAsync(string courseId, string studentEmail)
    {
        var student = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == UserRole.Student);
        
        if (student == null)
            return new List<Grade>();

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && c.StudentIds.Contains(student.Id));

        if (course == null)
            return new List<Grade>();

        return await _context.Grades
            .Where(g => g.CourseId == courseId && g.StudentId == student.Id)
            .ToListAsync();
    }

    public async Task<bool> UpdateGradeInCourseAsync(string courseId, string gradeId, UpdateGradeRequest request, string teacherEmail)
    {
        var teacher = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == teacherEmail && u.Role == UserRole.Teacher);
        
        if (teacher == null)
            return false;

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacher.Id);

        if (course == null)
            return false;
            
        var grade = await _context.Grades
            .FirstOrDefaultAsync(g => g.Id == gradeId && g.CourseId == courseId);
            
        if (grade == null)
            return false;
            
        // Create a grade history record before updating
        var gradeHistory = new GradeHistory
        {
            Id = ObjectId.GenerateNewId().ToString(),
            GradeId = grade.Id,
            PreviousGrade = grade.GradeValue,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = teacher.Id,
            ChangeReason = request.ChangeReason
        };
        
        // Update the grade
        grade.GradeValue = request.GradeValue;
        grade.AssignmentName = request.AssignmentName;
        
        // Add the history record
        await _context.Set<GradeHistory>().AddAsync(gradeHistory);
        await _context.SaveChangesAsync();
        
        return true;
    }
}

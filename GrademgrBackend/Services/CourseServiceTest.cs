using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using Xunit;

namespace GrademgrBackend.Tests
{
    public class CourseServiceTests
    {
        private DbContextOptions<DatabaseContext> GetDbContextOptions()
        {
            return new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        #region CreateCourseAsync Tests

        [Fact]
        public async Task CreateCourseAsync_WithValidTeacher_CreatesNewCourse()
        {
            // Arrange
            var options = GetDbContextOptions();

            // Setup the database with a teacher
            using (var context = new DatabaseContext(options))
            {
                var teacher = new User
                {
                    Id = "teacher1",
                    Email = "teacher@example.com",
                    Username = "teacher",
                    FullName = "Test Teacher",
                    Password = "Password123!",
                    Role = UserRole.Teacher
                };

                context.Users.Add(teacher);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DatabaseContext(options))
            {
                var courseService = new CourseService(context);
                var request = new CreateCourseRequest
                {
                    CourseName = "Test Course",
                    CourseCode = "TST101",
                    Description = "A test course"
                };

                var result = await courseService.CreateCourseAsync(request, "teacher@example.com");

                // Assert
                Assert.NotNull(result);
                Assert.Equal("Test Course", result.CourseName);
                Assert.Equal("TST101", result.CourseCode);
                Assert.Equal("A test course", result.Description);
                Assert.Equal("teacher1", result.TeacherId);

                // Verify course was saved to database
                var savedCourse = await context.Courses.FirstOrDefaultAsync(c => c.CourseCode == "TST101");
                Assert.NotNull(savedCourse);
            }
        }

        #endregion

        #region DeleteCourseAsync Tests

        [Fact]
        public async Task DeleteCourseAsync_DeletesCourseAndRelatedGrades()
        {
            // Arrange
            var options = GetDbContextOptions();

            string courseId = ObjectId.GenerateNewId().ToString();

            // Setup database with teacher, course, and grades
            using (var context = new DatabaseContext(options))
            {
                var teacher = new User
                {
                    Id = "teacher1",
                    Email = "teacher@example.com",
                    Username = "teacher",
                    FullName = "Test Teacher",
                    Role = UserRole.Teacher,
                    Password = "TestPassword123"  // Added missing password
                };

                var student = new User
                {
                    Id = "student1",
                    Email = "student@example.com",
                    Username = "student",
                    FullName = "Test Student",
                    Password = "Password123!",
                    Role = UserRole.Student
                };

                var course = new Course
                {
                    Id = courseId,
                    CourseName = "Test Course",
                    CourseCode = "TST101",
                    TeacherId = "teacher1",
                    StudentIds = new List<string> { "student1" },
                    Description = "Test course description" // Add missing required property
                };
                var grade = new Grade
                {
                    Id = "grade1",
                    CourseId = courseId,
                    StudentId = "student1",
                    GradeValue = 85,
                    AssignmentName = "Test Assignment",
                    EnteredAt = DateTime.UtcNow,
                    EnteredBy = "teacher1"
                };

                context.Users.AddRange(teacher, student);
                context.Courses.Add(course);
                context.Grades.Add(grade);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DatabaseContext(options))
            {
                var courseService = new CourseService(context);
                var result = await courseService.DeleteCourseAsync(courseId, "teacher@example.com");

                // Assert
                Assert.True(result);

                // Verify course was deleted
                var course = await context.Courses.FindAsync(courseId);
                Assert.Null(course);

                // Verify grades were deleted
                var grades = await context.Grades.Where(g => g.CourseId == courseId).ToListAsync();
                Assert.Empty(grades);
            }
        }

        #endregion

        #region AddStudentToCourseAsync Tests

        [Fact]
        public async Task AddStudentToCourseAsync_AddsStudentToCourse()
        {
            // Arrange
            var options = GetDbContextOptions();

            string courseId = ObjectId.GenerateNewId().ToString();

            // Setup database with teacher, course, and student
            using (var context = new DatabaseContext(options))
            {
                var teacher = new User
                {
                    Id = "teacher1",
                    Email = "teacher@example.com",
                    Username = "teacher",
                    FullName = "Test Teacher",
                    Role = UserRole.Teacher,
                    Password = "TeacherPass123"  // Added missing password
                };

                var student = new User
                {
                    Id = "student1",
                    Email = "student@example.com",
                    Username = "student",
                    FullName = "Test Student",
                    Password = "password123",
                    Role = UserRole.Student
                };

                var course = new Course
                {
                    Id = courseId,
                    CourseName = "Test Course",
                    CourseCode = "TST101",
                    TeacherId = "teacher1",
                    StudentIds = new List<string>(),
                    Description = "Test course description" // Add missing required property
                };

                context.Users.AddRange(teacher, student);
                context.Courses.Add(course);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DatabaseContext(options))
            {
                var courseService = new CourseService(context);
                var result = await courseService.AddStudentToCourseAsync(
                    courseId, "student@example.com", "teacher@example.com");

                // Assert
                Assert.True(result);

                // Verify student was added to course
                var course = await context.Courses.FindAsync(courseId);
                Assert.Contains("student1", course.StudentIds);
            }
        }

        #endregion

        #region GetCourseDetailsAsync Tests

        [Fact]
        public async Task GetCourseDetailsAsync_AsTeacher_ReturnsFullDetails()
        {
            // Arrange
            var options = GetDbContextOptions();

            string courseId = ObjectId.GenerateNewId().ToString();

            // Setup database with teacher, students, and course
            using (var context = new DatabaseContext(options))
            {
                var teacher = new User
                {
                    Id = "teacher1",
                    Email = "teacher@example.com",
                    Username = "teacher",
                    FullName = "Test Teacher",
                    Role = UserRole.Teacher,
                    Password = "TeacherPass123"  // Added missing password
                };

                var student1 = new User
                {
                    Id = "student1",
                    Email = "student1@example.com",
                    Username = "student1",
                    FullName = "Test Student 1",
                    Role = UserRole.Student,
                    Password = "StudentPass123"  // Added missing password
                };

                var student2 = new User
                {
                    Id = "student2",
                    Email = "student2@example.com",
                    Username = "student2",
                    FullName = "Test Student 2",
                    Password = "Password123!",
                    Role = UserRole.Student
                };

                var course = new Course
                {
                    Id = courseId,
                    CourseName = "Test Course",
                    CourseCode = "TST101",
                    Description = "A test course",
                    TeacherId = "teacher1",
                    StudentIds = new List<string> { "student1", "student2" }
                };

                context.Users.AddRange(teacher, student1, student2);
                context.Courses.Add(course);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DatabaseContext(options))
            {
                var courseService = new CourseService(context);
                var result = await courseService.GetCourseDetailsAsync(
                    courseId, "teacher@example.com", UserRole.Teacher);

                // Assert
                Assert.NotNull(result);
                Assert.Equal("Test Course", result.CourseName);
                Assert.Equal("TST101", result.CourseCode);
                Assert.Equal("A test course", result.Description);
                Assert.Equal("Test Teacher", result.TeacherName);
                Assert.Equal("teacher@example.com", result.TeacherEmail);
                Assert.Equal(2, result.Students.Count);

                // Verify student details
                Assert.Contains(result.Students, s => s.Email == "student1@example.com");
                Assert.Contains(result.Students, s => s.Email == "student2@example.com");
            }
        }

        #endregion

        #region AddGradeToCourseAsync Tests

        [Fact]
        public async Task AddGradeToCourseAsync_AddsGradeToStudent()
        {
            // Arrange
            var options = GetDbContextOptions();

            string courseId = ObjectId.GenerateNewId().ToString();

            // Setup database with teacher, student, and course
            using (var context = new DatabaseContext(options))
            {
                var teacher = new User
                {
                    Id = "teacher1",
                    Email = "teacher@example.com",
                    Username = "teacher",
                    FullName = "Test Teacher",
                    Role = UserRole.Teacher,
                    Password = "TeacherPass123"  // Added missing password
                };

                var student = new User
                {
                    Id = "student1",
                    Email = "student@example.com",
                    Username = "student",
                    FullName = "Test Student",
                    Password = "Password123!",
                    Role = UserRole.Student
                };

                var course = new Course
                {
                    Id = courseId,
                    CourseName = "Test Course",
                    CourseCode = "TST101",
                    TeacherId = "teacher1",
                    StudentIds = new List<string> { "student1" },
                    Description = "Test course description" // Add missing required property
                };
                context.Users.AddRange(teacher, student);
                context.Courses.Add(course);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DatabaseContext(options))
            {
                var courseService = new CourseService(context);
                var gradeRequest = new GradeRequest
                {
                    GradeValue = 95,
                    AssignmentName = "Final Exam"
                };

                var result = await courseService.AddGradeToCourseAsync(
                    courseId, "student@example.com", gradeRequest, "teacher@example.com");

                // Assert
                Assert.True(result);

                // Verify grade was added
                var grade = await context.Grades.FirstOrDefaultAsync(
                    g => g.CourseId == courseId && g.StudentId == "student1");

                Assert.NotNull(grade);
                Assert.Equal(95, grade.GradeValue);
                Assert.Equal("Final Exam", grade.AssignmentName);
                Assert.Equal("teacher1", grade.EnteredBy);
            }
        }

        #endregion
    }
}

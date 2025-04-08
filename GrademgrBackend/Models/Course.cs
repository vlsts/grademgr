using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

/// <summary>
/// Represents a course in the educational system.
/// A course has one teacher, multiple students, and associated information.
/// </summary>
public class Course
{
    /// <summary>
    /// Gets or sets the unique identifier for the course.
    /// Stored as a MongoDB ObjectId.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the full name of the course.
    /// For example, "Introduction to Computer Science".
    /// </summary>
    [BsonElement("courseName")]
    public string CourseName { get; set; }

    /// <summary>
    /// Gets or sets the course code.
    /// Usually a department abbreviation followed by a number (e.g., "CS101").
    /// </summary>
    [BsonElement("courseCode")]
    public string CourseCode { get; set; }

    /// <summary>
    /// Gets or sets the detailed description of the course.
    /// Should include information about course content and objectives.
    /// </summary>
    [BsonElement("description")]
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the teacher assigned to this course.
    /// References the Id property of a User with Teacher role.
    /// </summary>
    [BsonElement("teacherId")]
    public string TeacherId { get; set; }

    /// <summary>
    /// Gets or sets the list of student identifiers enrolled in this course.
    /// Each identifier references the Id property of a User with Student role.
    /// </summary>
    [BsonElement("studentIds")]
    public List<string> StudentIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the date and time when the course was created.
    /// Stored in UTC format.
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

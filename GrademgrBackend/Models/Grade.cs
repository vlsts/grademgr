using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Models related to grade management functionality.
/// </summary>

/// <summary>
/// Represents a grade assigned to a student for a specific course assignment.
/// </summary>
public class Grade
{
    /// <summary>
    /// Gets or sets the unique identifier for the grade.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the student.
    /// </summary>
    [BsonElement("studentId")]
    public string StudentId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the course.
    /// </summary>
    [BsonElement("courseId")]
    public string CourseId { get; set; }

    /// <summary>
    /// Gets or sets the numeric value of the grade (0-100).
    /// </summary>
    [BsonRepresentation(BsonType.Decimal128)]
    [Range(0, 100)]
    [BsonElement("gradeValue")]
    public decimal GradeValue { get; set; }

    /// <summary>
    /// Gets or sets the name of the assignment for which the grade was given.
    /// </summary>
    [BsonElement("assignmentName")]
    public string AssignmentName { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the grade was entered into the system.
    /// </summary>
    [BsonElement("enteredAt")]
    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the user who entered the grade.
    /// </summary>
    [BsonElement("enteredBy")]
    public string EnteredBy { get; set; }
}

/// <summary>
/// Represents a historical record of changes made to a grade.
/// </summary>
public class GradeHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for the grade history entry.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the grade that was changed.
    /// </summary>
    [BsonElement("gradeId")]
    public string GradeId { get; set; }

    /// <summary>
    /// Gets or sets the previous grade value before the change.
    /// </summary>
    [BsonElement("previousGrade")]
    public decimal PreviousGrade { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the grade was changed.
    /// </summary>
    [BsonElement("changedAt")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the user who changed the grade.
    /// </summary>
    [BsonElement("changedBy")]
    public string ChangedBy { get; set; }

    /// <summary>
    /// Gets or sets the reason for changing the grade.
    /// </summary>
    [BsonElement("changeReason")]
    public string ChangeReason { get; set; }
}

/// <summary>
/// Base class for grade-related requests.
/// </summary>
public class GradeRequest
{
    /// <summary>
    /// Gets or sets the numeric value of the grade (between 0 and 100).
    /// </summary>
    [Required(ErrorMessage = "Grade value is required")]
    [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100")]
    public decimal GradeValue { get; set; }

    /// <summary>
    /// Gets or sets the name of the assignment for which the grade is being entered.
    /// </summary>
    [Required(ErrorMessage = "Assignment name is required")]
    public string AssignmentName { get; set; }
}

/// <summary>
/// Represents a request to add a new grade for a student.
/// </summary>
public class AddGradeRequest : GradeRequest
{
    /// <summary>
    /// Gets or sets the email address of the student receiving the grade.
    /// </summary>
    [Required(ErrorMessage = "Student email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    public string StudentMail { get; set; }
}

/// <summary>
/// Represents a request to update an existing grade.
/// </summary>
public class UpdateGradeRequest : GradeRequest
{
    /// <summary>
    /// Gets or sets the reason for updating the grade.
    /// </summary>
    public string ChangeReason { get; set; }
}

/// <summary>
/// Extends the Grade class with additional student information.
/// </summary>
public class GradeWithStudentInfo : Grade
{
    /// <summary>
    /// Gets or sets the full name of the student.
    /// </summary>
    public string StudentName { get; set; }
    
    /// <summary>
    /// Gets or sets the email address of the student.
    /// </summary>
    public string StudentEmail { get; set; }
}

/// <summary>
/// Data transfer object containing detailed information about a grade.
/// </summary>
public class GradeDetailsDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the grade.
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the student.
    /// </summary>
    public string StudentId { get; set; }
    
    /// <summary>
    /// Gets or sets the full name of the student.
    /// </summary>
    public string StudentName { get; set; }
    
    /// <summary>
    /// Gets or sets the email address of the student.
    /// </summary>
    public string StudentEmail { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the course.
    /// </summary>
    public string CourseId { get; set; }
    
    /// <summary>
    /// Gets or sets the course code.
    /// </summary>
    public string CourseCode { get; set; }
    
    /// <summary>
    /// Gets or sets the numeric value of the grade.
    /// </summary>
    public double GradeValue { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the assignment.
    /// </summary>
    public string AssignmentName { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the grade was entered.
    /// </summary>
    public DateTime EnteredAt { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the user who entered the grade.
    /// </summary>
    public string EnteredBy { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the teacher who entered the grade.
    /// </summary>
    public string TeacherName { get; set; }
    
    /// <summary>
    /// Gets or sets any additional comments about the grade.
    /// </summary>
    public string Comment { get; set; }
}

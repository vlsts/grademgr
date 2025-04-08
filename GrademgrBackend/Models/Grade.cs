using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

public class Grade
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("studentId")]
    public string StudentId { get; set; }

    [BsonElement("courseId")]
    public string CourseId { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    [Range(0, 100)]
    [BsonElement("gradeValue")]
    public decimal GradeValue { get; set; }

    [BsonElement("assignmentName")]
    public string AssignmentName { get; set; }

    [BsonElement("enteredAt")]
    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;

    [BsonElement("enteredBy")]
    public string EnteredBy { get; set; }
}

public class GradeHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("gradeId")]
    public string GradeId { get; set; }

    [BsonElement("previousGrade")]
    public decimal PreviousGrade { get; set; }

    [BsonElement("changedAt")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("changedBy")]
    public string ChangedBy { get; set; }

    [BsonElement("changeReason")]
    public string ChangeReason { get; set; }
}

public class GradeRequest
{
    [Required(ErrorMessage = "Grade value is required")]
    [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100")]
    public decimal GradeValue { get; set; }

    [Required(ErrorMessage = "Assignment name is required")]
    public string AssignmentName { get; set; }
}

public class AddGradeRequest : GradeRequest
{
    [Required(ErrorMessage = "Student email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    public string StudentMail { get; set; }
}

public class UpdateGradeRequest : GradeRequest
{
    public string ChangeReason { get; set; }
}

public class GradeWithStudentInfo : Grade
{
    public string StudentName { get; set; }
    public string StudentEmail { get; set; }
}

public class GradeDetailsDto
{
    public string Id { get; set; }
    public string StudentId { get; set; }
    public string StudentName { get; set; }
    public string StudentEmail { get; set; }
    public string CourseId { get; set; }
    public string CourseCode { get; set; }
    public double GradeValue { get; set; }
    public string AssignmentName { get; set; }
    public DateTime EnteredAt { get; set; }
    public string EnteredBy { get; set; }
    public string TeacherName { get; set; }
    public string Comment { get; set; }
}

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
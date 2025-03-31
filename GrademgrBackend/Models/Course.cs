using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Course
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("courseName")]
    public string CourseName { get; set; }

    [BsonElement("courseCode")]
    public string CourseCode { get; set; }

    [BsonElement("description")]
    public string Description { get; set; }

    [BsonElement("teacherId")]
    public string TeacherId { get; set; }

    [BsonElement("studentIds")]
    public List<string> StudentIds { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
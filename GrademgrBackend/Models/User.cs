using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a user in the grade management system.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the username for the user.
    /// </summary>
    [Required]
    [BsonElement("username")]
    public string Username { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    [Required]
    [EmailAddress]
    [BsonElement("email")]
    public string Email { get; set; }

    /// <summary>
    /// Gets or sets the hashed password for the user.
    /// </summary>
    [BsonElement("password")]
    public string Password { get; set; }

    /// <summary>
    /// Gets or sets the role of the user in the system.
    /// </summary>
    [BsonElement("role")]
    public UserRole Role { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user was created.
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the full name of the user.
    /// </summary>
    [BsonElement("fullName")]
    public string FullName { get; set; }
}

/// <summary>
/// Defines possible roles for users in the system.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Student role with limited permissions.
    /// </summary>
    Student = 1,
    
    /// <summary>
    /// Teacher role with elevated permissions.
    /// </summary>
    Teacher = 2
}

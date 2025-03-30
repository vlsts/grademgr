using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    public static DatabaseContext Create(IMongoDatabase mongoDatabase) =>
        new(new DbContextOptionsBuilder<DatabaseContext>()
            .UseMongoDB(mongoDatabase.Client, mongoDatabase.DatabaseNamespace.DatabaseName)
            .Options);

    // define dbsets
    // public DbSet<Student> Students { get; set; }
    // public DbSet<Course> Courses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // configure entity properties and relationships
        // modelBuilder.Entity<Grade>().ToCollection("grades");
        // modelBuilder.Entity<Student>().ToCollection("Students");
        // modelBuilder.Entity<Course>().ToCollection("Courses");
    }
}

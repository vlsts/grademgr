using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) {
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
     }

    public static DatabaseContext Create(IMongoDatabase database) =>
        new(new DbContextOptionsBuilder<DatabaseContext>()
            .UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName)
            .Options);

    public DbSet<User> Users { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<GradeHistory> GradeHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToCollection("users");
        modelBuilder.Entity<Course>().ToCollection("courses");
        modelBuilder.Entity<Grade>().ToCollection("grades");
        modelBuilder.Entity<GradeHistory>().ToCollection("gradehistories");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }

}
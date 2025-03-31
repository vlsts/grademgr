using MongoDB.Driver;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var mongoDbConnectionString = Environment.GetEnvironmentVariable("MONGODB_URI") ?? "mongodb://localhost:27017";

var mongoClient = new MongoClient(mongoDbConnectionString);
var mongoDatabase = mongoClient.GetDatabase("grade_manager_db");

InitializeDatabase(mongoDatabase);

var db = DatabaseContext.Create(mongoDatabase);
builder.Services.AddSingleton(db);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => {
        options.WithTitle("Grade Manager API");
        options.WithSidebar(true);
    });
}

void InitializeDatabase(IMongoDatabase database)
{
    var collections = database.ListCollectionNames().ToList();
    
    var requiredCollections = new[] { "users", "courses", "grades", "grade_history" };
    foreach (var collection in requiredCollections)
    {
        if (!collections.Contains(collection))
        {
            database.CreateCollection(collection);
        }
    }
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

using MongoDB.Driver;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var mongoDbConnectionString = Environment.GetEnvironmentVariable("MONGODB_URI") ?? "mongodb://localhost:27017";

// Register MongoDB client as a singleton (connection is thread-safe)
builder.Services.AddSingleton<IMongoClient>(sp => 
    new MongoClient(mongoDbConnectionString));

// Register MongoDB database as a singleton
builder.Services.AddSingleton(sp => 
    sp.GetRequiredService<IMongoClient>().GetDatabase("grade_manager_db"));

// Register DatabaseContext as scoped, but using the singleton database
builder.Services.AddScoped(sp => 
    DatabaseContext.Create(sp.GetRequiredService<IMongoDatabase>()));

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };
});

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireStudentRole", policy => policy.RequireRole("Student"));
    options.AddPolicy("RequireTeacherRole", policy => policy.RequireRole("Teacher"));
});

// Register auth service
builder.Services.AddScoped<IAuthService, AuthService>();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Allow requests from Angular app
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register course service
builder.Services.AddScoped<ICourseService, CourseService>();

// Register security service
builder.Services.AddScoped<ISecurityService, SecurityService>();

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

// Use CORS policy
app.UseCors("AllowFrontend");

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
// Removed UseHttpsRedirection since Render handles HTTPS at the edge
app.UseAuthorization();
app.MapControllers();

// Use the PORT environment variable provided by Render, default to 5121 if not set
var port = Environment.GetEnvironmentVariable("PORT") ?? "5121";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();

public class CodeRequest
{
    public required string Language { get; set; }
    public required string SourceCode { get; set; }
}

public class CodeResponse
{
    public required string Output { get; set; }
    public bool Error { get; set; }
}
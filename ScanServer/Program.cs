using Microsoft.Extensions.Hosting.WindowsServices;
using ScanServer.Controllers;
using System.Diagnostics;

ILogger<WebApplication> _logger;

var options = new WebApplicationOptions
{
    Args = args,
    // Setting to allow the service to run both in IDE and as service
    ContentRootPath = WindowsServiceHelpers.IsWindowsService()
        ? AppContext.BaseDirectory
        : default
};
var builder = WebApplication.CreateBuilder(options);
var isService = !(Debugger.IsAttached || args.Contains("--console"));
if (isService)
{
    builder.Host.UseWindowsService();
}

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = args.SkipWhile(arg => arg != "--origins")
                                 .Skip(1) // Skip the "--origins" argument itself
                                 .ToList();
if (allowedOrigins.Count == 0)
{
    allowedOrigins.Add("*"); // Set to wildcard if no origins specified
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins(allowedOrigins.ToArray())
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () =>
{
    return "Scan Server is Running with:" + "\nargs: " + String.Join(' ', args) + "\nContentRootPath: "+ options.ContentRootPath + "\nService: " + isService;
});

app.MapControllers();
app.UseCors();

if (isService)
{
    await app.RunAsync();
} else
{
    app.Run();
}

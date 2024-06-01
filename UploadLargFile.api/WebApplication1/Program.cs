using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MimeDetective.Engine;
using System.Runtime.CompilerServices;
using tusdotnet;
using tusdotnet.Models;
using WebApplication1.Data;
using WebApplication1.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(op =>
{
    op.UseSqlServer(builder.Configuration.GetConnectionString("ConStr"));
});
// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                 .WithExposedHeaders("Location", "Upload-Offset", "Upload-Length"); // Ensure Location header is exposed
        });
});

var host = builder.Services.BuildServiceProvider().GetRequiredService<IWebHostEnvironment>();
var dbContext = builder.Services.BuildServiceProvider().GetRequiredService<AppDbContext>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = 2_000_000_000;
    options.KeyLengthLimit = 2_000_000_000;
    options.ValueLengthLimit = 2_000_000_000;
    options.MultipartBodyLengthLimit = 2_000_000_000; // Limit to 1 MB
});

builder.WebHost.ConfigureKestrel(opt =>
{
    opt.Limits.MaxRequestBodySize = 2_000_000_000; // Limit to 1 MB
    opt.Limits.MaxRequestBufferSize = 2_000_000_000; // Limit to 1 MB
});
var app = builder.Build();
app.UseStaticFiles();

app.MapTus("/files", async httpContext => new()
{
    Store = new tusdotnet.Stores.TusDiskStore(Path.Combine(host.WebRootPath, "files")),
    Events = new()
    {
        // What to do when file is completely uploaded?
        OnFileCompleteAsync = async eventContext =>
        {
            var file = await eventContext.GetFileAsync();
            var metadata = await file.GetMetadataAsync(eventContext.CancellationToken);

            // Extract the original file name from metadata
            if (metadata.TryGetValue("filename", out var metadataFilename))
            {
                var originalFileName = metadataFilename.GetString(System.Text.Encoding.UTF8);

                // Get the current file path
                var currentFilePath = Path.Combine(host.WebRootPath, "files", eventContext.FileId);
                //"C:\\Users\\Ahmed Sameh\\Desktop\\New folder\\UploadLargFiles\\UploadLargFile.api\\WebApplication1\\wwwroot\\files\\4cda446f02254512a9b06a231b265e2f"
                // Define the new file path with the original file name

                var newFilePath = Path.Combine(host.WebRootPath, "files", originalFileName);
                //"C:\\Users\\Ahmed Sameh\\Desktop\\New folder\\UploadLargFiles\\UploadLargFile.api\\WebApplication1\\wwwroot\\files\\ASP NET [002] Web Essentials.mp4"
                try
                {
                    // Rename the file
                    File.Move(currentFilePath, newFilePath);
                }
                catch (IOException ex)
                {
                    // Handle the exception (e.g., log it, notify the user, etc.)
                    Console.WriteLine($"An error occurred while renaming the file: {ex.Message}");
                    // Optionally, you can implement a strategy to handle existing files, like appending a unique identifier to the file name
                }

                // Save the new file path to the database
                var fileUploaded = new FileUploaded()
                {
                    Path = newFilePath,
                    fileName = originalFileName,
                };
                await dbContext.files.AddAsync(fileUploaded);
                await dbContext.SaveChangesAsync();
            }
        }
    },
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAll");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
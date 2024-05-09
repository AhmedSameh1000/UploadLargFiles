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
            //var File = await eventContext.GetFileAsync();

            //var metaData = await File.GetMetadataAsync(eventContext.CancellationToken);
            //using FileStream Content = await File.GetContentAsync(eventContext.CancellationToken) as FileStream;

            //await TUS_Process.TUSProcess(Content, metaData);
            var FileUploded = new FileUploaded()
            {
                Path = Path.Combine(host.WebRootPath, "files", eventContext.FileId)
            };
            await dbContext.files.AddAsync(FileUploded);
            await dbContext.SaveChangesAsync();
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

public class TUS_Process
{
    public static Task TUSProcess(FileStream content, Dictionary<string, Metadata> metadata)
    {
        var c = content;
        var m = metadata;
        var name = metadata["filename"].GetString(System.Text.Encoding.UTF8);
        var directory = Path.GetDirectoryName(content.Name);

        var destpath = Path.Combine(directory, name);

        File.Copy(content.Name, destpath, true);

        return Task.CompletedTask;
    }
}
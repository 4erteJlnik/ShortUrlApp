using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using LiteDB;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.WebUtilities;
using UrlShortener.Model;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ILiteDatabase, LiteDatabase>(_ => new LiteDatabase("short-links.db"));
await using var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Home page: A form for submitting a URL
app.MapGet("/", ctx =>
                {
                    ctx.Response.ContentType = "text/html";
                    return ctx.Response.SendFileAsync("index.html");
                });

// API endpoint for shortening a URL and save it to a local database
app.MapPost("/url", ShortenerDelegate);

// Catch all page: redirecting shortened URL to its original address
app.MapFallback(RedirectDelegate);

await app.RunAsync();

static async Task ShortenerDelegate(HttpContext httpContext)
{
    var request = await httpContext.Request.ReadFromJsonAsync<UrlDto>();

    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var inputUri))
    {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsync("Неправильная ссылка.");
        return;
    }

    var liteDb = httpContext.RequestServices.GetRequiredService<ILiteDatabase>();
    var links = liteDb.GetCollection<ShortUrl>(BsonAutoId.Int32);
    var entry = new ShortUrl(inputUri);
    links.Insert(entry);

    var result = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{entry.UrlChunk}";
    await httpContext.Response.WriteAsJsonAsync(new { url = result });
}

static async Task RedirectDelegate(HttpContext httpContext)
{
    var db = httpContext.RequestServices.GetRequiredService<ILiteDatabase>();
    var collection = db.GetCollection<ShortUrl>();

    var path = httpContext.Request.Path.ToUriComponent().Trim('/');
    var id = BitConverter.ToInt32(WebEncoders.Base64UrlDecode(path));
    var entry = collection.Find(p => p.Id == id).FirstOrDefault();

    httpContext.Response.Redirect(entry?.Url ?? "/");

    await Task.CompletedTask;
}

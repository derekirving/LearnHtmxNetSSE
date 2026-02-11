using System.Text.Json;
using System.Text.Json.Serialization;
using PageEditingApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<PageLockService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// SSE endpoint for page lock notifications
app.MapGet("/api/page-locks/stream", async (HttpContext context, PageLockService lockService) =>
{
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    context.Response.Headers.Append("Cache-Control", "no-cache");
    context.Response.Headers.Append("Connection", "keep-alive");

    var clientId = Guid.NewGuid().ToString();
    await lockService.RegisterClient(clientId, context.Response.Body, context.RequestAborted);
});


// API endpoint to unlock a page. Method=POST for sendBeacon compatibility (from browser close/unload)
app.MapPost("/api/page-locks", async (HttpContext context, PageLockService lockService) =>
{
    var beaconData = await JsonSerializer.DeserializeAsync<BeaconPayload>(context.Request.Body);
    if (beaconData != null)
    {
        await lockService.UnlockPage(beaconData.PageId);
    }
   
    return Results.Ok();
});


// API endpoint to get current locks
app.MapGet("/api/page-locks", (PageLockService lockService) => Results.Ok((object?)lockService.GetAllLocks()));
app.Run();

public record BeaconPayload(
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("timestamp")] string Timestamp
);

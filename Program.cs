using PageEditingApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<PageLockService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

// API endpoint to lock a page
app.MapPost("/api/page-locks/{pageId}/lock", async (string pageId, PageLockService lockService, HttpContext context) =>
{
    var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "Anonymous";
    var success = await lockService.LockPage(pageId, userId);
    return success ? Results.Ok() : Results.Conflict(new { message = "Page is already locked" });
});

// API endpoint to unlock a page
app.MapDelete("/api/page-locks/{pageId}", async (string pageId, PageLockService lockService) =>
{
    await lockService.UnlockPage(pageId);
    return Results.Ok();
});

// Handle POST with _method=DELETE for sendBeacon compatibility (from browser close/unload)
app.MapPost("/api/page-locks/{pageId}", async (string pageId, PageLockService lockService, HttpContext context) =>
{
    // Check if this is a DELETE via POST (from sendBeacon)
    var form = await context.Request.ReadFormAsync();
    if (!form.ContainsKey("_method") || form["_method"] != "DELETE")
        return Results.BadRequest(new { message = "Invalid request" });
    
    await lockService.UnlockPage(pageId);
    return Results.Ok();

    // If not a DELETE request, return method not allowed
});

// API endpoint to get current locks
app.MapGet("/api/page-locks", (PageLockService lockService) => Results.Ok((object?)lockService.GetAllLocks()));

app.Run();

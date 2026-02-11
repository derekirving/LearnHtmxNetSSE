# Page Editing App with HTMX and SSE

A .NET 10 Razor Pages application that implements real-time page locking using Server-Sent Events (SSE) and HTMX.

## Features

- **Real-time Page Locking**: When a user opens a page for editing, all other users see it's locked in real-time
- **Server-Sent Events (SSE)**: Uses SSE with HTMX extension for live updates
- **Auto-unlock on Leave**: Pages automatically unlock when the editor closes the page, cancels, or saves
- **Enhanced Visual Feedback**: Locked pages show with yellow highlight, animated lock icon, and disabled edit buttons
- **No Database Required**: Simple in-memory implementation (easily extensible to use a database)
- **Clean UI**: Modern, responsive interface

## How It Works

1. **Index Page**: Displays all available pages with edit buttons
2. **SSE Connection**: Each browser connects to `/api/page-locks/stream` to receive real-time updates
3. **Lock on Edit**: When a user clicks "Edit Page", the app:
   - Sends a POST request to lock the page
   - If successful, navigates to the edit page
   - Broadcasts a `page-locked` event to all connected clients
4. **Visual Feedback**: All users see the locked page with:
   - Yellow/gold background and border
   - Animated lock icon
   - "Currently being edited by [username]" message
   - Disabled edit button with "(Locked)" indicator
5. **Unlock on Save/Cancel/Leave**: The page is unlocked when:
   - User clicks "Save Changes" (form submission)
   - User clicks "Cancel" button
   - User closes the browser tab/window
   - All users are notified via SSE that the page is unlocked

## Project Structure

```
PageEditingApp/
├── Program.cs                          # App configuration and API endpoints
├── Services/
│   └── PageLockService.cs             # Manages locks and SSE connections
├── Pages/
│   ├── Index.cshtml                   # Main page listing (with HTMX + SSE)
│   ├── Index.cshtml.cs               # Index page model
│   ├── Edit.cshtml                    # Edit page
│   ├── Edit.cshtml.cs                # Edit page model
│   └── _ViewImports.cshtml           # Razor imports
├── PageEditingApp.csproj             # Project file
└── appsettings.json                  # Configuration
```

## Running the Application

### Prerequisites
- .NET 10 SDK

### Steps

1. **Navigate to the project directory:**
   ```bash
   cd PageEditingApp
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Open multiple browser windows:**
   - Open `https://localhost:7XXX` (check console for exact port)
   - Open the same URL in another browser window or incognito tab
   - Try editing a page in one window and watch it lock in the other!

## API Endpoints

### SSE Stream
```
GET /api/page-locks/stream
```
Returns a Server-Sent Events stream with lock updates.

**Event Types:**
- `connected`: Initial connection established
- `initial`: Current state of all locks
- `page-locked`: A page was locked
- `page-unlocked`: A page was unlocked

### Lock a Page
```
POST /api/page-locks/{pageId}
Headers: X-User-Id: {username}
```
Attempts to lock a page for editing.

### Unlock a Page
```
DELETE /api/page-locks/{pageId}
```
Unlocks a page.

### Get All Locks
```
GET /api/page-locks
```
Returns current locks.

## HTMX + SSE Extension

The app uses:
- **HTMX 2.0.4**: For AJAX interactions
- **HTMX SSE Extension 2.2.2**: For Server-Sent Events

### Usage in HTML:
```html
<div hx-ext="sse" 
     sse-connect="/api/page-locks/stream"
     sse-swap="message">
    <!-- Content updated via SSE -->
</div>
```

### JavaScript Event Handling:
```javascript
document.body.addEventListener('htmx:sseMessage', function(event) {
    const data = JSON.parse(event.detail.data);
    // Handle different event types
});
```

## Customization

### Adding Database Support
Replace the in-memory `ConcurrentDictionary` in `PageLockService` with Entity Framework:

```csharp
public class PageLockService
{
    private readonly ApplicationDbContext _context;
    
    public async Task<bool> LockPage(string pageId, string userId)
    {
        var existingLock = await _context.PageLocks
            .FirstOrDefaultAsync(l => l.PageId == pageId);
            
        if (existingLock != null)
            return false;
            
        _context.PageLocks.Add(new PageLock { ... });
        await _context.SaveChangesAsync();
        await BroadcastEvent(...);
        return true;
    }
}
```

### Lock Timeout
Add automatic unlock after inactivity:

```csharp
public class PageLockService
{
    private Timer? _cleanupTimer;
    
    public PageLockService()
    {
        _cleanupTimer = new Timer(CleanupExpiredLocks, null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
    
    private async void CleanupExpiredLocks(object? state)
    {
        var expiredLocks = _locks.Values
            .Where(l => DateTime.UtcNow - l.LockedAt > TimeSpan.FromMinutes(30))
            .ToList();
            
        foreach (var lock in expiredLocks)
        {
            await UnlockPage(lock.PageId);
        }
    }
}
```

## Testing the Real-Time Updates

1. Open the app in two browser windows side-by-side
2. Click "Edit Page" on any page in Window 1
3. Watch Window 2 instantly show the page as locked with yellow highlight
4. Save or cancel in Window 1
5. Watch Window 2 instantly show the page as unlocked

## Browser Compatibility

SSE is supported in all modern browsers:
- Chrome/Edge: ✅
- Firefox: ✅
- Safari: ✅
- Opera: ✅

Note: Internet Explorer does not support SSE.

## License

This is a demonstration project. Feel free to use and modify as needed.

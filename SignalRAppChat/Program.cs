using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalRAppChat;
using SignalRAppChat.Data;


var builder = WebApplication.CreateBuilder(args);

//
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
//

builder.Services.AddSignalR(o =>
{
    o.MaximumReceiveMessageSize = 1024 * 1024 * 10;
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHub<ChatHub>("/chat");

app.Run();

using Blazing.Mvvm;
using ToolBox.Services.Ai;
using ToolBox.Web;
using ToolBox.Web.Client.Services;
using ToolBox.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddToolBoxWeb(BlazorHostingModelType.WebApp);
builder.Services.AddToolBoxAiChatBackend();

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.UseWebAssemblyDebugging();
else { app.UseExceptionHandler("/Error", createScopeForErrors: true); app.UseHsts(); }

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!builder.Configuration.GetValue("App:DisableHttpsRedirection", false))
    app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapAiChatApi();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ToolBox.Components.Routes).Assembly, typeof(ToolBox.Web.Client._Imports).Assembly);

app.Run();
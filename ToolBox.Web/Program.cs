using Blazing.Mvvm;
using ToolBox.Web.Client.Services;
using ToolBox.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddToolBoxWeb(BlazorHostingModelType.WebApp);

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.UseWebAssemblyDebugging();
else { app.UseExceptionHandler("/Error", createScopeForErrors: true); app.UseHsts(); }

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ToolBox.Components.Routes).Assembly, typeof(ToolBox.Web.Client._Imports).Assembly);

app.Run();
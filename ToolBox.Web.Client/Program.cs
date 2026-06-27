using Blazing.Mvvm;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ToolBox.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddToolBoxWeb(BlazorHostingModelType.WebAssembly);

await builder.Build().RunAsync();
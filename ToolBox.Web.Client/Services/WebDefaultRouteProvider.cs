using ToolBox.Services;

namespace ToolBox.Web.Client.Services;

public sealed class WebDefaultRouteProvider : IDefaultRouteProvider
{
    public string DefaultRoute => "/json-format";
}
# ToolBox Web — Blazor Web App (SSR + WASM)
# https://learn.microsoft.com/aspnet/core/host-and-deploy/docker/building-net-docker-images

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CommonHelp/CommonHelp.csproj CommonHelp/
COPY ToolBox.Tools/ToolBox.Tools.csproj ToolBox.Tools/
COPY ToolBox.Shared/ToolBox.Shared.csproj ToolBox.Shared/
COPY ToolBox.Web.Client/ToolBox.Web.Client.csproj ToolBox.Web.Client/
COPY ToolBox.Web/ToolBox.Web.csproj ToolBox.Web/

RUN dotnet restore ToolBox.Web/ToolBox.Web.csproj

COPY CommonHelp/ CommonHelp/
COPY ToolBox.Tools/ ToolBox.Tools/
COPY ToolBox.Shared/ ToolBox.Shared/
COPY ToolBox.Web.Client/ ToolBox.Web.Client/
COPY ToolBox.Web/ ToolBox.Web/

RUN dotnet publish ToolBox.Web/ToolBox.Web.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    App__DisableHttpsRedirection=true

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "ToolBox.Web.dll"]

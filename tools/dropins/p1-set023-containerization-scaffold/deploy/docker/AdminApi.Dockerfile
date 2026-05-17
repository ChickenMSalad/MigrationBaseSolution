# Migration Admin API container scaffold
# Build from repo root:
#   docker build -f deploy/docker/AdminApi.Dockerfile -t migration-admin-api:local .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Migration.Admin.Api/Migration.Admin.Api.csproj", "src/Migration.Admin.Api/"]
COPY ["src/Migration.ControlPlane/Migration.ControlPlane.csproj", "src/Migration.ControlPlane/"]
COPY ["src/Migration.Shared/Migration.Shared.csproj", "src/Migration.Shared/"]
COPY ["src/Migration.Orchestration.Abstractions/Migration.Orchestration.Abstractions.csproj", "src/Migration.Orchestration.Abstractions/"]
COPY ["src/Migration.Orchestration/Migration.Orchestration.csproj", "src/Migration.Orchestration/"]
COPY ["src/Migration.GenericRuntime/Migration.GenericRuntime.csproj", "src/Migration.GenericRuntime/"]
COPY ["src/Migration.Infrastructure/Migration.Infrastructure.csproj", "src/Migration.Infrastructure/"]
COPY ["src/Migration.Manifest.Sql/Migration.Manifest.Sql.csproj", "src/Migration.Manifest.Sql/"]
COPY ["src/Migration.Connectors.Aem/Migration.Connectors.Aem.csproj", "src/Migration.Connectors.Aem/"]
COPY ["src/Migration.Connectors.Apimo/Migration.Connectors.Apimo.csproj", "src/Migration.Connectors.Apimo/"]
COPY ["src/Migration.Connectors.AzureBlob/Migration.Connectors.AzureBlob.csproj", "src/Migration.Connectors.AzureBlob/"]
COPY ["src/Migration.Connectors.Bynder/Migration.Connectors.Bynder.csproj", "src/Migration.Connectors.Bynder/"]
COPY ["src/Migration.Connectors.Cloudinary/Migration.Connectors.Cloudinary.csproj", "src/Migration.Connectors.Cloudinary/"]
COPY ["src/Migration.Connectors.ContentHub/Migration.Connectors.ContentHub.csproj", "src/Migration.Connectors.ContentHub/"]
COPY ["src/Migration.Connectors.LocalStorage/Migration.Connectors.LocalStorage.csproj", "src/Migration.Connectors.LocalStorage/"]
COPY ["src/Migration.Connectors.Rclone/Migration.Connectors.Rclone.csproj", "src/Migration.Connectors.Rclone/"]
COPY ["src/Migration.Connectors.S3/Migration.Connectors.S3.csproj", "src/Migration.Connectors.S3/"]
COPY ["src/Migration.Connectors.SharePoint/Migration.Connectors.SharePoint.csproj", "src/Migration.Connectors.SharePoint/"]
COPY ["src/Migration.Connectors.Webdam/Migration.Connectors.Webdam.csproj", "src/Migration.Connectors.Webdam/"]

RUN dotnet restore "src/Migration.Admin.Api/Migration.Admin.Api.csproj"

COPY . .
RUN dotnet publish "src/Migration.Admin.Api/Migration.Admin.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Migration.Admin.Api.dll"]

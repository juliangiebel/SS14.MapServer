# Basic Example Configuration

````yaml
Serilog:
  Using: [ "Serilog.Sinks.Console" ]
  MinimumLevel:
    Default: "Information"
    Override:
      SS14: "Information"
      Microsoft: "Warning"
      Microsoft.Hosting.Lifetime: "Information"
      Microsoft.AspNetCore: "Warning"
      #This service doesn't use data protection
      Microsoft.AspNetCore.DataProtection: "Error"
      #Ignore api key spam
      SS14.MapServer.Security.ApiKeyHandler: "Error" 

  WriteTo:
    - Name: Console
      Args:
        OutputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}"

  Enrich: [ "FromLogContext" ]

AllowedHosts: "*"

Auth:
  ApiKey: "<api key>"

Git:
  RepositoryUrl: "<repository url>"
  Branch: "master"
  # Exclude maps that generally break the map renderer,
  # like planet maps or you just don't want rendered
  mapFileExcludePatterns:
    - "europa.yml"

Github:
  AppName: "<github app name>"
  AppId: <app id>
  AppPrivateKeyLocation: "private-key.pem"
  AppWebhookSecret: "<secret>"

ConnectionStrings:
  default: "Server=map_database;Port=5432;Database=postgres;User Id=postgres;Password=<password>;"

Server:
  CorsOrigins:
    - "<map server host>"
    - "127.0.0.1"
  Host: "<host>"
  EnableSentry: true
  EnableSentryTracing: true

# The sentry integration is entirely optional
Sentry:
  Dsn: "<sentry dsn>"
  EnableTracing: true
  MaxRequestBodySize: "Always"
  ServerName: "Live server"
  # Ensure to keep this too false to prevent GDPR issues
  SendDefaultPii: false 
  MinimumBreadcrumbLevel: "Warning"
  MinimumEventLevel: "Error"
  AttachStackTrace: true
  DiagnosticsLevel: "Error"

Build:
  MapRendererCommand: "Content.MapRenderer"
````
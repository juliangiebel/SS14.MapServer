# Basic Example Configuration

````yaml
Auth:
    ApiKey: "debug"

Processing:
    TargetDirectory: "/tmp/mapstest/build"
    DirectoryPoolMaxSize: 3

Github:
    AppName: "SS14.MapServer"
    TemplateLocation: "Resources/Templates"

Server:
    CorsOrigins:
        - "http://localhost:5173"
    Language: "en-US"

Git:
    Branch: "master"
````
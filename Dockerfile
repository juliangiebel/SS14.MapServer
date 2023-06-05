FROM mcr.microsoft.com/dotnet/sdk:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SS14.MapServer/SS14.MapServer.csproj", "SS14.MapServer/"]
RUN dotnet restore "SS14.MapServer/SS14.MapServer.csproj"
COPY . .
WORKDIR "/src/SS14.MapServer"
RUN dotnet build "SS14.MapServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SS14.MapServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
ENV DOTNET_ENVIRONMENT=Production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://*:80
COPY --from=publish /app/publish .
COPY ./SS14.MapServer/appsettings.yaml .
COPY ./SS14.MapServer/appsettings.Production.yaml .
RUN mkdir /app/build
ENTRYPOINT ["dotnet", "SS14.MapServer.dll", "--environment=Production"]

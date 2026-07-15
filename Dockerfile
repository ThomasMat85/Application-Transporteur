FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Application Camion API.csproj", "./"]
RUN dotnet restore "Application Camion API.csproj"

COPY . .
RUN dotnet publish "Application Camion API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Application Camion API.dll"]

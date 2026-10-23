# ---------- Stage 1: build (full SDK, ~800 MB, thrown away afterwards) ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first so the restore step is cached
COPY MyApi.csproj .
RUN dotnet restore

# Copy the rest of the source and publish a Release build
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ---------- Stage 2: runtime (ASP.NET runtime only, much smaller) ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MyApi.dll"]
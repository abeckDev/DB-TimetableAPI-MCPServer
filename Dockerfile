# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
WORKDIR /src

# Copy solution and project files
COPY ["AbeckDev.DbTimetable.sln", "./"]
COPY ["AbeckDev.DbTimetable.Mcp/AbeckDev.DbTimetable.Mcp.csproj", "AbeckDev.DbTimetable.Mcp/"]
COPY ["AbeckDev.DbTimetable.Mcp.Test/AbeckDev.DbTimetable.Mcp.Test.csproj", "AbeckDev.DbTimetable.Mcp.Test/"]

# Restore dependencies
RUN dotnet restore "AbeckDev.DbTimetable.Mcp/AbeckDev.DbTimetable.Mcp.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/AbeckDev.DbTimetable.Mcp"
RUN dotnet build "AbeckDev.DbTimetable.Mcp.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AbeckDev.DbTimetable.Mcp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS final
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published files
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 3001

# Set environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:3001

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:3001/mcp || exit 1

# Entry point
ENTRYPOINT ["dotnet", "AbeckDev.DbTimetable.Mcp.dll"]

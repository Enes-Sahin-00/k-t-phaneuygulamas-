# Multi-stage build for production
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["kütüphaneuygulaması/kütüphaneuygulaması.csproj", "kütüphaneuygulaması/"]
COPY ["kütüphaneuygulaması.Tests/kütüphaneuygulaması.Tests.csproj", "kütüphaneuygulaması.Tests/"]

# Restore dependencies
RUN dotnet restore "kütüphaneuygulaması/kütüphaneuygulaması.csproj"
RUN dotnet restore "kütüphaneuygulaması.Tests/kütüphaneuygulaması.Tests.csproj"

# Copy source code
COPY . .

# Build the application
RUN dotnet build "kütüphaneuygulaması/kütüphaneuygulaması.csproj" -c Release -o /app/build

# Run tests
RUN dotnet test "kütüphaneuygulaması.Tests/kütüphaneuygulaması.Tests.csproj" --no-build --verbosity normal

FROM build AS publish
RUN dotnet publish "kütüphaneuygulaması/kütüphaneuygulaması.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "kütüphaneuygulaması.dll"] 
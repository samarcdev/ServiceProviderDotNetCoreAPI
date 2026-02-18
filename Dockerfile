# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY APIServiceManagement.sln .

# Copy project files
COPY src/APIServiceManagement.API/APIServiceManagement.API.csproj src/APIServiceManagement.API/
COPY src/APIServiceManagement.Application/APIServiceManagement.Application.csproj src/APIServiceManagement.Application/
COPY src/APIServiceManagement.Domain/APIServiceManagement.Domain.csproj src/APIServiceManagement.Domain/
COPY src/APIServiceManagement.Infrastructure/APIServiceManagement.Infrastructure.csproj src/APIServiceManagement.Infrastructure/

# Restore dependencies
RUN dotnet restore APIServiceManagement.sln

# Copy all source files
COPY src/ src/

# Build and publish the API project
WORKDIR /src/src/APIServiceManagement.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create directory for uploads
RUN mkdir -p /app/uploads

# Copy published files from build stage
COPY --from=build /app/publish .

# Expose port (default ASP.NET Core port)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "APIServiceManagement.API.dll"]

# Use the official .NET SDK image as a build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy the project files and restore dependencies
COPY . ./
RUN dotnet restore

# Build the project
RUN dotnet publish -c Release -o out

# Use the official .NET runtime image as a runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Expose the port your application runs on
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "blockchain cos thesis.dll"]
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /source
COPY ./ ./

# Run the tests and build the application
RUN ./build.sh --target Test Compile --configuration Release

# Run stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /source/artifacts/ ./
ENTRYPOINT ["dotnet", "Universalis.Application.dll"]

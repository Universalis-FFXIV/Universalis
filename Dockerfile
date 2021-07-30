# Build application
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /source
COPY ./ ./
RUN ./build.sh --target TestNoGameData Compile --configuration Release

# Create run stage
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build-env /source/artifacts/ ./
ENTRYPOINT ["dotnet", "Universalis.Application.dll"]
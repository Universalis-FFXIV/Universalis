# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /source
COPY ./ ./

# Run the tests and build the application
RUN ./build.sh --target Test Compile --configuration Release

# Run stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0

# Download sheets
WORKDIR /sqpack/ffxiv
RUN apt update && apt install curl -y
RUN curl -O https://s3.us-west-1.wasabisys.com/universalis-ffxiv-sqpack/sqpack/ffxiv/0a0000.win32.dat0
RUN curl -O https://s3.us-west-1.wasabisys.com/universalis-ffxiv-sqpack/sqpack/ffxiv/0a0000.win32.index2
RUN curl -O https://s3.us-west-1.wasabisys.com/universalis-ffxiv-sqpack/sqpack/ffxiv/0a0000.win32.index

WORKDIR /app
COPY --from=build-env /source/artifacts/ ./
HEALTHCHECK --start-period=1m CMD curl -sf http://localhost:4002/api/74/5 || exit 1
ENTRYPOINT ["dotnet", "Universalis.Application.dll"]

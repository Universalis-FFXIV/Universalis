# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /source
COPY ./ ./

# Run the tests and build the application
RUN ./build.sh --target Test Compile --configuration Release

# Run stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0

# Install dependencies
WORKDIR /
RUN apt update && apt install curl git zsh -y
RUN sh -c "$(curl -fsSL https://raw.githubusercontent.com/ohmyzsh/ohmyzsh/master/tools/install.sh)"
ENV ZSH_THEME=clean

# Install .NET diagnostic tools
WORKDIR /usr/bin
RUN \
    # dotnet-trace allows for profiling the application in a running container
    # > dotnet-trace collect -p1 --duration '00:00:00:30'
    curl -L --output dotnet-trace https://aka.ms/dotnet-trace/linux-x64 && \
    chmod +x dotnet-trace && \
    # dotnet-gcdump allows for collecting heap dumps, but usually results in the container going OOM
    # > dotnet-gcdump collect -p1
    curl -L --output dotnet-gcdump https://aka.ms/dotnet-gcdump/linux-x64 && \
    chmod +x dotnet-gcdump && \
    # Ensure each of the files has a non-zero size
    test -s dotnet-trace && \
    test -s dotnet-gcdump || \
    # Fail this step if any check failed
    exit 1

# Download sheets
WORKDIR /sqpack/ffxiv
RUN \
    # Fetch game data from repo
    curl -OL https://raw.githubusercontent.com/karashiiro/0a0000/main/0a0000.win32.{dat0,index2,index} && \
    # Ensure each of the files has a non-zero size, otherwise game data is corrupt for some reason
    test -s 0a0000.win32.dat0 && \
    test -s 0a0000.win32.index2 && \
    test -s 0a0000.win32.index || \
    # Fail this step if any check failed
    exit 1

WORKDIR /app
COPY --from=build-env /source/artifacts/ ./
HEALTHCHECK --start-period=60s --interval=180s --retries=3 --timeout=30s CMD curl -sf http://localhost:4002/api/74/5 || exit 1
ENTRYPOINT ["dotnet", "Universalis.Application.dll"]

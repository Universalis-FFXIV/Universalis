# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /source
COPY ./ ./

# Install MongoDB for tests
RUN ln -s /bin/echo /bin/systemctl \
    && apt-get update \
    && apt-get install -qqy --no-install-recommends gnupg \
    && wget -qO - https://www.mongodb.org/static/pgp/server-5.0.asc | apt-key add - \
    && echo "deb http://repo.mongodb.org/apt/debian buster/mongodb-org/5.0 main" | tee /etc/apt/sources.list.d/mongodb-org-5.0.list

RUN apt-get update \
    && apt-get install -qqy --no-install-recommends mongodb-org=5.0.1

RUN mkdir /data \
    && mkdir /data/db \
    && mkdir /data/log \
    && chown mongodb /data -R

# Run the tests and build the application
RUN mongod --fork --logpath /var/log/mongod.log \
    && ./build.sh --target Test Compile --configuration Release

# Run stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /source/artifacts/ ./
ENTRYPOINT ["dotnet", "Universalis.Application.dll"]

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /source
COPY ./ ./

# Install MongoDB driver for tests, run the tests, and build
RUN ln -s /bin/echo /bin/systemctl \
    && apt-get update \
    && apt-get install -qqy --no-install-recommends gnupg=2.2.12-1+deb10u1 \
    && wget -qO - https://www.mongodb.org/static/pgp/server-5.0.asc | apt-key add - \
    && echo "deb http://repo.mongodb.org/apt/debian buster/mongodb-org/5.0 main" | tee /etc/apt/sources.list.d/mongodb-org-5.0.list

RUN apt-get update \
    && apt-get install -qqy --no-install-recommends mongodb-org=5.0.1

RUN mkdir /data \
    && mkdir /data/db \
    && mkdir /data/log \
    && chown mongodb /data \
    && chown mongodb /data/db \
    && chown mongodb /data/log

RUN mongod --fork --logpath /var/log/mongod.log \
    && ./build.sh --target Test Compile --configuration Release

# Run stage
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build-env /source/artifacts/ ./
ENTRYPOINT ["dotnet", "Universalis.Application.dll"]

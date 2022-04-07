# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /source
COPY ./ ./

# Install MongoDB driver for tests, run the tests, and build
RUN ln -s /bin/echo /bin/systemctl \
    && apt-get update \
    && apt-get install -qqy --no-install-recommends gnupg \
    && wget -qO - https://www.mongodb.org/static/pgp/server-5.0.asc | apt-key add - \
    && echo "deb http://repo.mongodb.org/apt/debian buster/mongodb-org/5.0 main" | tee /etc/apt/sources.list.d/mongodb-org-5.0.list

# Install Swashbuckle CLI Tool to generate swagger file 
RUN dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.3.0
ENV PATH="${PATH}:/root/.dotnet/tools"

RUN apt-get update \
    && apt-get install -qqy --no-install-recommends mongodb-org=5.0.1

RUN mkdir /data \
    && mkdir /data/db \
    && mkdir /data/log \
    && chown mongodb /data -R

RUN mongod --fork --logpath /var/log/mongod.log \
    && ./build.sh --target Test Compile --configuration Release

# Create Swagger file
RUN swagger tofile --basepath https://universalis.app/ --serializeasv2 --output /source/swagger.json /source/artifacts/Universalis.Application.dll v2

FROM swaggerapi/swagger-codegen-cli:latest
WORKDIR /clients
COPY --from=build-env /source/swagger.json /clients/swagger.json

# Create clients
RUN swagger-codegen generate -l python -o /clients/python -i /clients/swagger.json


# Run stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /source/artifacts/ ./
ENTRYPOINT ["dotnet", "Universalis.Application.dll"]

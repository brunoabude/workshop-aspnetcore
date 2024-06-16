FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
RUN apt update
RUN apt install sqlite3 -y
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /src
# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out
RUN dotnet ef migrations --project Workshop.Netcore.WebApi script -o migrate.sql
RUN sqlite3 app-prod.db < migrate.sql

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /WebApi
COPY --from=build-env /src/out .
COPY --from=build-env /src/app-prod.db app.db
ENTRYPOINT ["dotnet", "Workshop.Netcore.WebApi.dll"]

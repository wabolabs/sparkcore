# syntax=docker/dockerfile:1.7
#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
ARG BUILD_VERSION=3.5.0

#FROM mcr.microsoft.com/dotnet/runtime:9.0.3-noble-amd64 AS base
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
ARG BUILD_VERSION
WORKDIR /app
EXPOSE 80

#FROM mcr.microsoft.com/dotnet/sdk:9.0.202-noble-amd64 AS build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_VERSION
WORKDIR /src
## Root MSBuild files must be present for the restore layer, otherwise the
## security pins in Directory.Build.targets are missing from project.assets.json.
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Build.targets", "./"]
COPY ["Web/Resgrid.Web.Eventing/Resgrid.Web.Eventing.csproj", "Web/Resgrid.Web.Eventing/"]
COPY ["Providers/Resgrid.Providers.Bus.Rabbit/Resgrid.Providers.Bus.Rabbit.csproj", "Providers/Resgrid.Providers.Bus.Rabbit/"]
COPY ["Core/Resgrid.Framework/Resgrid.Framework.csproj", "Core/Resgrid.Framework/"]
COPY ["Core/Resgrid.Config/Resgrid.Config.csproj", "Core/Resgrid.Config/"]
COPY ["Core/Resgrid.Model/Resgrid.Model.csproj", "Core/Resgrid.Model/"]
COPY ["Core/Resgrid.Services/Resgrid.Services.csproj", "Core/Resgrid.Services/"]
COPY ["Providers/Resgrid.Providers.Bus/Resgrid.Providers.Bus.csproj", "Providers/Resgrid.Providers.Bus/"]
COPY ["Providers/Resgrid.Providers.Cache/Resgrid.Providers.Cache.csproj", "Providers/Resgrid.Providers.Cache/"]
RUN dotnet restore "Web/Resgrid.Web.Eventing/Resgrid.Web.Eventing.csproj"
COPY . .
WORKDIR "/src/Web/Resgrid.Web.Eventing"

FROM build AS publish
ARG BUILD_VERSION
## base-image change that drops them fails the build rather than silently shipping
RUN DEBIAN_FRONTEND=noninteractive apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*
RUN dotnet publish "Resgrid.Web.Eventing.csproj" -c Release -o /app/publish  -p:Version=${BUILD_VERSION}
ADD --checksum=sha256:2241be671073520e028b2f12df1e9ef0419014cffb5670b7a80b2080804be17d https://github.com/ufoscout/docker-compose-wait/releases/download/2.12.1/wait /app/publish/wait
RUN chmod +x /app/publish/wait

FROM base AS final
WORKDIR /app
## Copy the IANA timezone database into the final stage. The MCR aspnet base
## does ship tzdata, but the final image is assembled from the publish stage's
## copy so the zone data is explicit rather than inherited.
COPY --from=publish /usr/share/zoneinfo /usr/share/zoneinfo
ENV TZ=Etc/UTC
COPY --from=publish /app/publish .
# docker-compose-wait waits for WAIT_HOSTS, then execs WAIT_COMMAND.
# The MCR base has a shell, so this could be chained with "sh -c"; the exec form
# is kept because it needs no shell and signals the intent more clearly.
ENV WAIT_COMMAND="dotnet Resgrid.Web.Eventing.dll"
ENTRYPOINT ["./wait"]

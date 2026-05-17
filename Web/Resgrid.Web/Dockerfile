#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
ARG BUILD_VERSION=1.0.0

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
ARG BUILD_VERSION
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN apt-get update
RUN apt-get install -y ca-certificates curl gnupg
RUN curl -sL https://deb.nodesource.com/setup_20.x  | bash -
RUN apt-get -y install nodejs
ARG BUILD_VERSION
WORKDIR /src
COPY ["Web/Resgrid.Web/Resgrid.Web.csproj", "Web/Resgrid.Web/"]
COPY ["Providers/Resgrid.Providers.Bus.Rabbit/Resgrid.Providers.Bus.Rabbit.csproj", "Providers/Resgrid.Providers.Bus.Rabbit/"]
COPY ["Core/Resgrid.Framework/Resgrid.Framework.csproj", "Core/Resgrid.Framework/"]
COPY ["Core/Resgrid.Config/Resgrid.Config.csproj", "Core/Resgrid.Config/"]
COPY ["Core/Resgrid.Model/Resgrid.Model.csproj", "Core/Resgrid.Model/"]
COPY ["Providers/Resgrid.Providers.AddressVerification/Resgrid.Providers.AddressVerification.csproj", "Providers/Resgrid.Providers.AddressVerification/"]
COPY ["Core/Resgrid.Services/Resgrid.Services.csproj", "Core/Resgrid.Services/"]
COPY ["Providers/Resgrid.Providers.Bus/Resgrid.Providers.Bus.csproj", "Providers/Resgrid.Providers.Bus/"]
COPY ["Providers/Resgrid.Providers.Cache/Resgrid.Providers.Cache.csproj", "Providers/Resgrid.Providers.Cache/"]
COPY ["Providers/Resgrid.Providers.Geo/Resgrid.Providers.Geo.csproj", "Providers/Resgrid.Providers.Geo/"]
COPY ["Repositories/Resgrid.Repositories.DataRepository/Resgrid.Repositories.DataRepository.csproj", "Repositories/Resgrid.Repositories.DataRepository/"]
COPY ["Providers/Resgrid.Providers.Number/Resgrid.Providers.Number.csproj", "Providers/Resgrid.Providers.Number/"]
COPY ["Providers/Resgrid.Providers.Email/Resgrid.Providers.Email.csproj", "Providers/Resgrid.Providers.Email/"]
COPY ["Providers/Resgrid.Providers.Marketing/Resgrid.Providers.Marketing.csproj", "Providers/Resgrid.Providers.Marketing/"]
COPY ["Providers/Resgrid.Providers.Pdf/Resgrid.Providers.Pdf.csproj", "Providers/Resgrid.Providers.Pdf/"]
COPY ["Providers/Resgrid.Providers.Claims/Resgrid.Providers.Claims.csproj", "Providers/Resgrid.Providers.Claims/"]
COPY ["Providers/Resgrid.Providers.Migrations/Resgrid.Providers.Migrations.csproj", "Providers/Resgrid.Providers.Migrations/"]
COPY ["Providers/Resgrid.Providers.Voip/Resgrid.Providers.Voip.csproj", "Providers/Resgrid.Providers.Voip/"]
RUN dotnet restore "Web/Resgrid.Web/Resgrid.Web.csproj"
COPY . .
WORKDIR "/src/Web/Resgrid.Web"

FROM build AS publish
ARG BUILD_VERSION
RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
ENV PATH="${PATH}:/root/.dotnet/tools"
# Pre-restore client libraries with retries to survive transient CDN failures.
# The csproj PrepublishScript is skipped when SKIP_LIBMAN_RESTORE=1 is set.
RUN for i in 1 2 3; do libman restore && break || { echo "libman restore attempt $i failed, retrying..."; sleep 10; }; done
ENV SKIP_LIBMAN_RESTORE=1

# SPA build — two-layer trick to avoid esbuild ETXTBSY on Docker overlayfs.
# Layer 1: npm ci --ignore-scripts downloads all packages including the
# @esbuild/linux-x64 binary package (no scripts, no ETXTBSY).
# We then pre-place the esbuild binary for vite so it exists on the
# *committed* layer before any validation spawn happens.
WORKDIR /src/Web/Resgrid.Web/Areas/User/Apps
RUN npm ci --ignore-scripts \
    && mkdir -p node_modules/vite/node_modules/esbuild/bin \
    && cp node_modules/vite/node_modules/@esbuild/linux-x64/bin/esbuild \
          node_modules/vite/node_modules/esbuild/bin/esbuild \
    && chmod +x node_modules/vite/node_modules/esbuild/bin/esbuild
# Layer 2: npm rebuild runs esbuild's install.js which finds the binary
# already on the committed layer above — validates without writing, no ETXTBSY.
RUN npm rebuild
# Build the React SPA (dist output consumed by CopyClientApps MSBuild target).
RUN npm run build -- --mode production --outDir ./dist/core --emptyOutDir

# Publish .NET project; MSBuild npm targets are skipped (SKIP_NPM_BUILD=1)
# because we already ran npm install + build above.
WORKDIR /src/Web/Resgrid.Web
ENV SKIP_NPM_BUILD=1
RUN dotnet publish "Resgrid.Web.csproj" -c Release -o /app/publish -p:Version=${BUILD_VERSION}
## Download the wait script in the build stage where a shell is available
ADD https://github.com/ufoscout/docker-compose-wait/releases/download/2.9.0/wait /app/publish/wait
RUN chmod +x /app/publish/wait

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./wait"]
CMD ["dotnet", "Resgrid.Web.dll"]

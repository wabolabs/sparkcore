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
COPY ["Web/Resgrid.Web.Services/Resgrid.Web.Services.csproj", "Web/Resgrid.Web.Services/"]
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
COPY ["Providers/Resgrid.Providers.Authentication/Resgrid.Providers.Authentication.csproj", "Providers/Resgrid.Providers.Authentication/"]
RUN dotnet restore "Web/Resgrid.Web.Services/Resgrid.Web.Services.csproj"
COPY . .
WORKDIR "/src/Web/Resgrid.Web.Services"

FROM build AS publish
ARG BUILD_VERSION
## base-image change that drops them fails the build rather than silently shipping
RUN DEBIAN_FRONTEND=noninteractive apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*
RUN dotnet publish "Resgrid.Web.Services.csproj" -c Release -o /app/publish -p:Version=${BUILD_VERSION}
ADD --checksum=sha256:2241be671073520e028b2f12df1e9ef0419014cffb5670b7a80b2080804be17d https://github.com/ufoscout/docker-compose-wait/releases/download/2.12.1/wait /app/publish/wait
RUN chmod +x /app/publish/wait

## Install wkhtmltopdf and capture its runtime libraries. IPdfProvider (NRecoProvider) shells out to
## /usr/local/bin/wkhtmltopdf for the API's PDF endpoints (invoices, bids, time reports, records
## export templates, bulk packets).
## glibc is left out of the copy so the binary runs against the final image's own loader/libc, and the
## library dir is set as the binary's RPATH rather than a global LD_LIBRARY_PATH so the dotnet process
## keeps loading the base image's libssl/libstdc++.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        wget \
        patchelf \
        fontconfig \
        fonts-dejavu-core \
        init-system-helpers \
        libfontconfig1 \
        libfreetype6 \
        libjpeg62-turbo \
        libpng16-16 \
        libssl3 \
        libx11-6 \
        libxext6 \
        libxrender1 \
        zlib1g \
    && wget -q https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-3/wkhtmltox_0.12.6.1-3.bookworm_amd64.deb \
    && (dpkg -i wkhtmltox_0.12.6.1-3.bookworm_amd64.deb || apt-get install -fy) \
    && rm -f wkhtmltox_0.12.6.1-3.bookworm_amd64.deb \
    && ! ldd /usr/local/bin/wkhtmltopdf | grep -q 'not found' \
    && mkdir -p /tmp/wkdeps \
    && ldd /usr/local/bin/wkhtmltopdf | awk '/=>/ {print $3}' \
        | grep -vE '/(libc|libm|libdl|libpthread|librt)\.so' \
        | while read lib; do cp "$lib" /tmp/wkdeps/; done \
    && patchelf --force-rpath --set-rpath /usr/local/lib/wkhtmltopdf /usr/local/bin/wkhtmltopdf \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

FROM base AS final
WORKDIR /app
## Copy the IANA timezone database into the final stage. The MCR aspnet base
## does ship tzdata, but the final image is assembled from the publish stage's
## copy so the zone data is explicit rather than inherited.
COPY --from=publish /usr/share/zoneinfo /usr/share/zoneinfo
ENV TZ=Etc/UTC
## Copy wkhtmltopdf binary + runtime libraries + fonts from publish stage
COPY --from=publish /usr/local/bin/wkhtmltopdf /usr/local/bin/wkhtmltopdf
COPY --from=publish /tmp/wkdeps/ /usr/local/lib/wkhtmltopdf/
COPY --from=publish /etc/fonts/ /etc/fonts/
COPY --from=publish /usr/share/fonts/ /usr/share/fonts/
## Fail the build, not the first PDF request, if the binary can't load here (exec form).
RUN ["/usr/local/bin/wkhtmltopdf", "--version"]
COPY --from=publish /app/publish .
# docker-compose-wait waits for WAIT_HOSTS, then execs WAIT_COMMAND.
# The MCR base has a shell, so this could be chained with "sh -c"; the exec form
# is kept because it needs no shell and signals the intent more clearly.
ENV WAIT_COMMAND="dotnet Resgrid.Web.Services.dll"
ENTRYPOINT ["./wait"]

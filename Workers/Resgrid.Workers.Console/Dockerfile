#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
ARG BUILD_VERSION=1.0.0

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
ARG BUILD_VERSION
WORKDIR /app
# Install Chromium for PuppeteerSharp PDF generation.
# Set PrintConfig.ChromiumExecutablePath=/usr/bin/chromium via env or ResgridConfig.json.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        chromium \
        fonts-liberation \
        fonts-dejavu-core \
        libgbm1 \
        libnss3 \
        libatk-bridge2.0-0 \
        libgtk-3-0 \
        libasound2 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_VERSION
WORKDIR /src

COPY ["Workers/Resgrid.Workers.Console/Resgrid.Workers.Console.csproj", "Workers/Resgrid.Workers.Console/"]
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
COPY ["Workers/Resgrid.Workers.Framework/Resgrid.Workers.Framework.csproj", "Workers/Resgrid.Workers.Framework/"]
COPY ["Providers/Resgrid.Providers.Migrations/Resgrid.Providers.Migrations.csproj", "Providers/Resgrid.Providers.Migrations/"]
COPY ["Providers/Resgrid.Providers.Voip/Resgrid.Providers.Voip.csproj", "Providers/Resgrid.Providers.Voip/"]
RUN dotnet restore "Workers/Resgrid.Workers.Console/Resgrid.Workers.Console.csproj"
COPY . .
WORKDIR "/src/Workers/Resgrid.Workers.Console"

FROM build AS publish
ARG BUILD_VERSION
RUN dotnet publish "Resgrid.Workers.Console.csproj" -c Release -o /app/publish -p:Version=${BUILD_VERSION}
ADD https://github.com/ufoscout/docker-compose-wait/releases/download/2.9.0/wait /app/publish/wait
RUN chmod +x /app/publish/wait

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./wait"]
CMD ["dotnet", "Resgrid.Workers.Console.dll"]

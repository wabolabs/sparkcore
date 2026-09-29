ARG BUILD_VERSION=3.5.0

## Static ffmpeg/ffprobe sourced from a registry image rather than curl'd from
## johnvansickle.com. That host has no CDN and regularly refuses or stalls
## connections from GitHub Actions runners, which burned 5 x 300s retries per
## build before failing. Registry pulls use the same transport as the base
## images and are cached by buildx. Pinned to the multi-arch manifest digest.
FROM mwader/static-ffmpeg:7.1@sha256:a8090df5f5608daef387e1b2e93b98aaacb4d92153ad904e7d715c725724fca4 AS ffmpeg

#FROM mcr.microsoft.com/dotnet/runtime:9.0.3-noble-amd64 AS base
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
ARG BUILD_VERSION
WORKDIR /app
EXPOSE 8080

#FROM mcr.microsoft.com/dotnet/sdk:9.0.202-noble-amd64 AS build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_VERSION
WORKDIR /src

## Directory.Build.targets carries the transitive security pins; this stage
## publishes with --no-restore, so it must be present before dotnet restore.
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Build.targets", "./"]
COPY ["Web/Resgrid.Web.Tts/Resgrid.Web.Tts.csproj", "Web/Resgrid.Web.Tts/"]
COPY ["Core/Resgrid.Config/Resgrid.Config.csproj", "Core/Resgrid.Config/"]
COPY ["Web/Resgrid.Web.Common/Resgrid.Web.Common.csproj", "Web/Resgrid.Web.Common/"]
RUN dotnet restore "Web/Resgrid.Web.Tts/Resgrid.Web.Tts.csproj"

COPY . .
WORKDIR /src/Web/Resgrid.Web.Tts
RUN dotnet publish "Resgrid.Web.Tts.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore -p:Version=${BUILD_VERSION}

FROM build AS publish
ARG PIPER_VERSION=v1.2.0
ARG PIPER_VOICES_REVISION=e21c7de8d4eab79b902f0d61e662b3f21664b8d2
COPY --from=base /etc/passwd /tmp/base-passwd
COPY --from=base /etc/group /tmp/base-group
RUN set -eu; \
	apt-get update \
	&& apt-get install -y --no-install-recommends \
		libespeak-ng1 \
		ca-certificates \
		curl \
		xz-utils \
		gzip \
		passwd \
	&& rm -rf /var/lib/apt/lists/*

COPY --from=ffmpeg /ffmpeg /usr/local/bin/ffmpeg
COPY --from=ffmpeg /ffprobe /usr/local/bin/ffprobe
## Fail loudly here rather than at runtime if the binaries are unusable.
RUN set -eu; \
	chmod +x /usr/local/bin/ffmpeg /usr/local/bin/ffprobe \
	&& /usr/local/bin/ffmpeg -version > /dev/null \
	&& /usr/local/bin/ffprobe -version > /dev/null

RUN set -eu; \
	echo "Downloading Piper ${PIPER_VERSION}" \
	&& curl -fsSL --retry 5 --retry-delay 5 --retry-all-errors --connect-timeout 15 --max-time 600 "https://github.com/rhasspy/piper/releases/download/${PIPER_VERSION}/piper_amd64.tar.gz" -o /tmp/piper.tar.gz \
	&& tar -xzf /tmp/piper.tar.gz -C /tmp \
	&& mv /tmp/piper/piper /usr/local/bin/piper \
	&& chmod +x /usr/local/bin/piper \
	&& mkdir -p /usr/local/lib \
	&& find /tmp/piper -name '*.so*' -exec cp {} /usr/local/lib/ \; \
	&& if [ -d /tmp/piper/espeak-ng-data ]; then cp -R /tmp/piper/espeak-ng-data /usr/share/; else ln -sf /usr/lib/x86_64-linux-gnu/espeak-ng-data /usr/share/espeak-ng-data; fi \
	&& ldconfig \
	&& rm -rf /tmp/piper /tmp/piper.tar.gz

RUN set -eu; \
	mkdir -p /usr/local/share/piper-voices; \
	for f in \
		"en/en_US/ryan/medium/en_US-ryan-medium" \
		"es/es_MX/claude/high/es_MX-claude-high" \
		"sv/sv_SE/nst/medium/sv_SE-nst-medium" \
		"de/de_DE/thorsten/medium/de_DE-thorsten-medium" \
		"fr/fr_FR/siwis/medium/fr_FR-siwis-medium" \
		"it/it_IT/paola/medium/it_IT-paola-medium" \
		"pl/pl_PL/gosia/medium/pl_PL-gosia-medium" \
		"uk/uk_UA/ukrainian_tts/medium/uk_UA-ukrainian_tts-medium" \
		"ar/ar_JO/kareem/medium/ar_JO-kareem-medium" \
	; do \
		name=$(basename "$f"); \
		echo "Downloading Piper voice ${name}"; \
		curl -fsSL --retry 5 --retry-delay 5 --retry-all-errors --connect-timeout 15 --max-time 600 "https://huggingface.co/rhasspy/piper-voices/resolve/${PIPER_VOICES_REVISION}/${f}.onnx" -o "/usr/local/share/piper-voices/${name}.onnx"; \
		curl -fsSL --retry 5 --retry-delay 5 --retry-all-errors --connect-timeout 15 --max-time 600 "https://huggingface.co/rhasspy/piper-voices/resolve/${PIPER_VOICES_REVISION}/${f}.onnx.json" -o "/usr/local/share/piper-voices/${name}.onnx.json"; \
	done

RUN set -eu; \
	groupadd --gid 10001 appgroup \
	&& useradd --uid 10001 --gid appgroup --create-home --shell /usr/sbin/nologin appuser \
	&& cp /tmp/base-passwd /tmp/app-passwd \
	&& cp /tmp/base-group /tmp/app-group \
	&& grep appuser /etc/passwd >> /tmp/app-passwd \
	&& grep appgroup /etc/group >> /tmp/app-group \
	&& mkdir -p /tmp/ttsdeps \
	&& for bin in /usr/local/bin/piper /usr/local/bin/ffmpeg; do \
		ldd "$bin" 2>/dev/null | awk '/=>/ {print $3}' >> /tmp/ttsdeps/libs.txt; \
	done \
	&& find /usr/local/lib -name '*.so*' -type f >> /tmp/ttsdeps/libs.txt \
	&& sort -u /tmp/ttsdeps/libs.txt | while read lib; do [ -f "$lib" ] && cp "$lib" /tmp/ttsdeps/; done

## base-image change that drops them fails the build rather than silently shipping
## tzdata is a real package in the MCR base, so the forced reinstall the
## hardened image needed is gone; the install is kept so the zone database is
## explicit, with the assertion so a future base change that drops it fails the
## build rather than silently shipping an empty /usr/share/zoneinfo.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*

FROM base AS final
WORKDIR /app

COPY --from=publish /usr/local/bin/piper /usr/local/bin/piper
COPY --from=publish /usr/local/share/piper-voices/ /usr/local/share/piper-voices/
COPY --from=publish /usr/share/espeak-ng-data/ /usr/share/espeak-ng-data/
COPY --from=publish /usr/local/bin/ffmpeg /usr/local/bin/ffmpeg
COPY --from=publish /usr/local/bin/ffprobe /usr/local/bin/ffprobe
COPY --from=publish /tmp/ttsdeps/ /usr/local/lib/tts/
COPY --from=publish /tmp/app-passwd /etc/passwd
COPY --from=publish /tmp/app-group /etc/group
## Copy the IANA timezone database into the final stage. The MCR aspnet base
## does ship tzdata, but the final image is assembled from the publish stage's
## copy so the zone data is explicit rather than inherited.
COPY --from=publish /usr/share/zoneinfo /usr/share/zoneinfo
ENV TZ=Etc/UTC
COPY --from=publish /app/publish .

ENV LD_LIBRARY_PATH="/usr/local/lib/tts"
ENV ASPNETCORE_URLS=http://+:8080
USER appuser
ENTRYPOINT ["dotnet", "Resgrid.Web.Tts.dll"]

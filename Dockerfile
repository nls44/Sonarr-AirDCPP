ARG ALPINE_VERSION="3.24"
FROM alpine:${ALPINE_VERSION} AS base

ARG TARGETPLATFORM
ARG SONARR_RELEASE="latest"

ENV COMPlus_EnableDiagnostics=0 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    XDG_CONFIG_HOME="/config/xdg" \
    TMPDIR="/tmp/sonarr-temp"

RUN \
  echo "**** install packages ****" && \
  apk add --no-cache \
    curl \
    jq \
    bash \
    ca-certificates \
    coreutils \
    icu-libs \
    sqlite-libs \
    xmlstarlet \
    tini && \
  echo "**** install sonarr ****" && \
  echo "TARGETPLATFORM=$TARGETPLATFORM" && \
  RELEASE_FOR_PLATFORM=$(case ${TARGETPLATFORM:-linux/amd64} in \
    "linux/amd64")   echo ".*linux-musl-x64.tar.gz*"  ;; \
    "linux/arm64")   echo ".*linux-musl-arm64.tar.gz*" ;; \
    *)               echo ""        ;; esac) && \
  echo "RELEASE_FOR_PLATFORM=$RELEASE_FOR_PLATFORM" && \
  mkdir -p \
   /app/sonarr/bin \
   /config \
   /tmp/sonarr-temp && \
  curl -o \
    /tmp/sonarr.tar.gz -L \
    "$(curl -s "https://api.github.com/repos/nls44/Sonarr-AirDCPP/releases/${SONARR_RELEASE}" | jq '.assets' | jq -r --arg RELEASE_FOR_PLATFORM "$RELEASE_FOR_PLATFORM" '.[].browser_download_url | match($RELEASE_FOR_PLATFORM;"i") | .string')" && \
  tar xzf \
    /tmp/sonarr.tar.gz -C \
    /app/sonarr/bin --strip-components=1 && \
  chmod +x /app/sonarr/bin/Sonarr && \
  echo "**** cleanup ****" && \
  rm -rf \
    /app/sonarr/bin/Sonarr.Update \
    /tmp/*

# ports and volumes
EXPOSE 8989

VOLUME /config

WORKDIR /app/sonarr/bin

ENTRYPOINT ["/sbin/tini", "--"]
CMD ["./Sonarr", "-nobrowser", "-data=/config"]
ARG ALPINE_VERSION="3.21.3"
ARG TARGETPLATFORM
ARG SONARR_RELEASE="latest"

FROM alpine:${ALPINE_VERSION} AS base

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
    xmlstarlet && \
  echo "**** install sonarr ****" && \
  echo "TARGETPLATFORM=$TARGETPLATFORM" && \
  RELEASE_FOR_PLATFORM=$(case ${TARGETPLATFORM:-linux/amd64} in \
    "linux/amd64")   echo ".*linux-musl-x64.tar.gz*"  ;; \
    "linux/arm64/v8")   echo ".*linux-musl-arm64.tar.gz*" ;; \
    *)               echo ""        ;; esac) && \
  echo "RELEASE_FOR_PLATFORM=$RELEASE_FOR_PLATFORM" && \
  mkdir -p \
   /app/sonarr/bin \
   /config && \
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

CMD ["/app/sonarr/bin/Sonarr", "-nobrowser", "-data=/config"]
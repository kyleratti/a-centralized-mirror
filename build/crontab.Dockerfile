FROM alpine:latest AS app-src
RUN apk add --no-cache git
RUN mkdir /app-src
WORKDIR /app-src
RUN git clone https://github.com/kyleratti/a-centralized-mirror.git .

FROM alpine:latest
RUN mkdir /scripts
COPY --from=app-src /app-src/scripts/acm-crontab.sh /scripts/acm-crontab.sh
CMD "echo * * * * * /scripts/acm-crontab.sh | crontab - && crond -f -L /dev/stdout"

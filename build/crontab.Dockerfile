FROM alpine:latest
RUN apk add --no-cache curl
RUN mkdir /scripts
COPY scripts/acm-crontab.sh /scripts/acm-crontab.sh
RUN chmod +x /scripts/acm-crontab.sh
RUN echo "* * * * * /bin/sh /scripts/acm-crontab.sh" > /var/spool/cron/crontabs/root
CMD crond -f -l 2

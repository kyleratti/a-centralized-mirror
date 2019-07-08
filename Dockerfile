FROM node:10-alpine AS app-build
RUN apk add --no-cache git
RUN mkdir -p /app-src
WORKDIR /app-src
RUN git clone https://github.com/kyleratti/a-centralized-mirror.git .
RUN npm install
RUN npm run build

FROM node:10-alpine AS app
RUN mkdir /data
RUN mkdir /app
COPY --from=app-build /app-src/node_modules/ /app/node_modules/
COPY --from=app-build /app-src/templates/ /app/templates/
COPY --from=app-build /app-src/lib/ /app/lib/

EXPOSE 3010
VOLUME /data

CMD [ "node", "-r", "dotenv/config", "/app/lib/index.js" ]

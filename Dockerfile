FROM node:10-alpine AS app-build
RUN apk add --no-cache git
RUN mkdir -p /app-src
WORKDIR /app-src
ARG CACHEBUST=1
RUN git clone https://github.com/kyleratti/a-centralized-mirror.git .
RUN npm install
RUN npm run build

FROM node:10-alpine AS app
RUN mkdir -p /app
COPY --from=app-build /app-src/node_modules/ /app/node_modules/
COPY --from=app-build /app-src/lib/ /app/lib/

EXPOSE 3010

CMD [ "node", "/app/lib/index.js" ]

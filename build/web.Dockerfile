FROM node:10-alpine AS app-build
RUN apk add --no-cache git
RUN mkdir -p /app-src
WORKDIR /app-src
COPY src/ /app-src/src/
COPY templates/ /app-src/templates/
COPY package.json /app-src/
RUN npm install
RUN npm run build

FROM node:10-alpine AS app-runtime
RUN mkdir /app
COPY --from=app-build /app-src/node_modules/ /app/node_modules/
COPY --from=app-build /app-src/templates/ /app/templates/
COPY --from=app-build /app-src/lib/ /app/lib/
COPY --from=app-build /app-src/scripts/ /app/scripts/

EXPOSE 3010
VOLUME /data

WORKDIR /app
CMD [ "node", "-r", "dotenv/config", "lib/index.js" ]

FROM node:10-alpine AS app-build
RUN mkdir -p /app-src
WORKDIR /app-src
COPY src/ /app-src/src/
COPY package.json /app-src/
RUN npm install --no-package-lock
RUN npm run build

FROM node:10-alpine AS app-runtime
RUN mkdir /app
COPY --from=app-build /app-src/node_modules/ /app/node_modules/
COPY --from=app-build /app-src/lib/ /app/lib/
COPY scripts/ /app/scripts/
COPY templates/ /app/templates/

EXPOSE 3010
VOLUME /data

WORKDIR /app
CMD [ "node", "-r", "dotenv/config", "lib/index.js" ]

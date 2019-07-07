FROM node:10-alpin
RUN mkdir -p /home/node/app/node_modules && chown -R node:node /home/node/app
WORKDIR /home/node/app
COPY package*.json ./
USER node
RUN npm install
RUN npm run build
COPY --chown=node ./lib .
EXPOSE 3010
CMD [ "node", "lib/index.js" ]

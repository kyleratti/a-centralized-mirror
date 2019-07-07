FROM ubuntu:18.04
RUN useradd -ms /bin/bash node
RUN apt update && apt upgrade -y && apt install -y git nodejs npm
RUN mkdir -p /home/node/app/node_modules && chown -R node:node /home/node/app
WORKDIR /home/node/app
USER node
RUN git clone https://github.com/kyleratti/a-centralized-mirror.git
WORKDIR ./a-centralized-mirror
RUN npm install
RUN npm run build
COPY --chown=node . .
EXPOSE 3010
CMD [ "node", "lib/index.js" ]

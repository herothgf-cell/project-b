FROM node:24-alpine

WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --omit=dev --ignore-scripts --no-audit --no-fund
COPY server.mjs ./

ENV PVP_RELAY_BIND=0.0.0.0
ENV PORT=8080
EXPOSE 8080
USER node
CMD ["node", "server.mjs"]

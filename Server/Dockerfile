FROM node:20-alpine AS build
WORKDIR /app
COPY Server/package.json Server/package-lock.json* ./
RUN npm install
COPY Server/tsconfig.json ./
COPY Server/src ./src
RUN npm run build

FROM node:20-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
COPY Server/package.json Server/package-lock.json* ./
RUN npm install --omit=dev
COPY --from=build /app/dist ./dist

RUN addgroup -S app && adduser -S app -G app
USER app

EXPOSE 8080
CMD ["node", "dist/index.js"]

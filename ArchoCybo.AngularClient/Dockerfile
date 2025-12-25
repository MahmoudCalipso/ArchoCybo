# syntax=docker/dockerfile:1
FROM node:20-alpine AS build
WORKDIR /app
COPY ArchoCybo.AngularClient/package*.json ./
RUN npm ci
COPY ArchoCybo.AngularClient ./
RUN npm run build

FROM nginx:alpine
COPY ArchoCybo.AngularClient/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/ArchoCybo.AngularClient /usr/share/nginx/html
EXPOSE 80

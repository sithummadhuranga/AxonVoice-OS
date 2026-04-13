FROM node:22-alpine AS build
WORKDIR /app

# Layer-cache: install deps from lockfile before copying full source
COPY frontend/package.json frontend/package-lock.json* ./
RUN npm ci --prefer-offline

COPY frontend/ ./
RUN npm run build

# Serve the static build via Nginx
FROM nginx:1.27-alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html

# Nginx config: single-page app — all routes serve index.html
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

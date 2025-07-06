# Stage 1: Build static files (nếu dùng build tool, ở đây chỉ copy)
FROM nginx:alpine
WORKDIR /usr/share/nginx/html
COPY . .
RUN rm -f /etc/nginx/conf.d/default.conf
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"] 
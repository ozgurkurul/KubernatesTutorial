# 1. Aşama: Build (Derleme) Aşaması
FROM node:20-alpine AS builder
WORKDIR /app
# Vite ile React projesi oluştur (Normalde bunu lokalde yaparız ama K8s eğitimindeyiz)
RUN npx create-vite@latest frontend --template react
WORKDIR /app/frontend
RUN npm install
# K8s'ten geldiğimizi belli eden küçük bir dokunuş yapalım
RUN sed -i 's/Vite + React/Ozgurun K8s React Ussu/g' index.html
RUN npm run build

# 2. Aşama: Sunum Aşaması (Sadece derlenen dosyaları alır)
FROM nginx:alpine
# Builder aşamasında oluşan dist klasörünü, Nginx'in içine kopyala
COPY --from=builder /app/frontend/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
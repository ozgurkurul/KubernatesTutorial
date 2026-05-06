# 1. Aşama: Derleme (Build) - İçinde .NET 10 SDK var
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TestApi.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# 2. Aşama: Sunum (Runtime) - Sadece kodu çalıştırmak için hafif sürüm
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# .NET 8 ve sonrasında varsayılan port 8080 oldu, K8s için tekrar 80'e sabitliyoruz
ENV ASPNETCORE_HTTP_PORTS=80

ENTRYPOINT ["dotnet", "TestApi.dll"]
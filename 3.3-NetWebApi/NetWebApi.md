## NET 10 Web Api Uygulaması

### Adım 1: Proje Dosyalarını Hazırlama

Sunucunda .NET API için temiz bir klasör oluşturalım ve içine girelim:

```bash
mkdir -p ~/k8s-egitim/dotnet-api
cd ~/k8s-egitim/dotnet-api
```

Önce proje dosyamızı (`TestApi.csproj`) yazalım:

```bash
nano TestApi.csproj
```

İçine şu kodları yapıştır (Swagger paketini de ekliyoruz):

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>
</Project>
```
*(Kaydet ve çık: `Ctrl+O`, `Enter`, `Ctrl+X`)*

Şimdi de Minimal API mantığıyla çalışan harika ve kısa bir `Program.cs` yazalım:

```bash
nano Program.cs
```

İçine şu C# kodlarını yapıştır:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

var builder = WebApplication.CreateBuilder(args);

// Swagger servislerini ekle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// DİKKAT: Normalde Swagger sadece Development'ta açılır. 
// K8s ortamında Production sayılacağı için, her zaman açılmasını sağlıyoruz.
app.UseSwagger();
app.UseSwaggerUI();

// Test Endpoint'imiz
app.MapGet("/api/durum", () => new { 
    Mesaj = "Adana'dan K3s uzerinde calisan .NET 10 API'sine Selamlar!", 
    Tarih = DateTime.Now,
    SunucuAdresi = Environment.MachineName 
});

app.Run();
```
*(Kaydet ve çık)*

---

### Adım 2: Multi-Stage Dockerfile (DevOps Sihri)

Şimdi bu kodu derleyecek ve K8s için hazır hale getirecek Dockerfile'ı yazıyoruz:

```bash
nano Dockerfile
```

```dockerfile
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
```
*(Kaydet ve çık)*

---

### Adım 3: İmajı Üret ve K3s'e Aktar

React'te yaptığımız "kas hafızası" ritüelini tekrar ediyoruz.

**1. .NET uygulamasını Docker ile inşa et:**
*(SDK'yı indireceği için ilk seferinde biraz sürebilir).*
```bash
docker build -t benim-dotnet-api:v1 .
```

**2. İmajı K3s'in içine aktar:**
```bash
docker save benim-dotnet-api:v1 | sudo k3s ctr images import -
```

---

### Adım 4: Kubernetes YAML (Uygulamayı Ayağa Kaldırma)

.NET imajımız K8s'in cebinde hazır. Şimdi Deployment ve Service manifestomuzu yazalım:

```bash
nano dotnet-k8s.yaml
```

Şu YAML kodunu yapıştır:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: dotnet-api-deployment
  labels:
    app: backend-api
spec:
  replicas: 2 # 2 tane .NET API kopyasi calissin
  selector:
    matchLabels:
      app: backend-api
  template:
    metadata:
      labels:
        app: backend-api
    spec:
      containers:
      - name: dotnet-container
        image: benim-dotnet-api:v1
        imagePullPolicy: Never
        ports:
        - containerPort: 80
---
apiVersion: v1
kind: Service
metadata:
  name: dotnet-api-servisi
spec:
  type: NodePort
  selector:
    app: backend-api
  ports:
    - port: 80
      targetPort: 80
      nodePort: 31010 # Bu sefer 31010 portunu sectik
```
*(Kaydet ve çık)*

Ve sistemi ateşle:
```bash
kubectl apply -f dotnet-k8s.yaml
```

---

### Sonuç ve Test Zamanı!

Podların ayağa kalktığını teyit et:
```bash
kubectl get pods
```

Pod'ların (`dotnet-api-deployment-...`) `Running` olduğunu gördükten sonra, MacBook'unun tarayıcısından direkt olarak Swagger arayüzüne git:

**`[http://192.168.1.121:31010/swagger/index.html](http://192.168.1.121:31010/swagger/index.html)`**

Ekranda o meşhur yeşil Swagger UI arayüzünü göreceksin! `/api/durum` endpoint'ine tıklayıp "Try it out" > "Execute" dediğinde, Kubernetes içinde dönen 2 farklı .NET pod'undan birinin sana "Adana'dan..." mesajını ve kendi makine adını (Pod Name) döndürdüğünü göreceksin. Hatta arka arkaya birkaç kez Execute dersen, Load Balancer (Yük Dengeleyici) sayesinde farklı pod isimlerinden cevap geldiğini bile yakalayabilirsin.


## Grafana ve Prometheus

Eğer Grafana ve Prometheus'u tamamen kendi sunucumuzun içine (K3s'e) kursaydık, sadece bu izleme araçları sunucunun 1-2 GB RAM'ini ve CPU'sunun bir kısmını kendilerine ayıracaktı. Ancak Grafana Cloud kullanarak **"Görselleştirme ve Depolama"** yükünü buluta devrediyoruz. Sunucumuzun gücü yine tamamen senin yazdığın kodlara kalıyor!

İşe bu iki efsanevi aracın K8s dünyasındaki görevlerini netleştirerek başlayalım:

### 1. Prometheus (Hafıza ve Toplayıcı)

Prometheus bir veritabanıdır (Time-Series Database). K8s cluster'ının içinde bir ajan gibi dolaşır. "Nginx pod'u ne kadar RAM yiyor?", ".NET API saniyede kaç istek alıyor?", "Sunucunun SSD'sinde ne kadar boş yer kaldı?" gibi binlerce sorunun cevabını her 15 saniyede bir toplar ve kaydeder.

### 2. Grafana (Vitrin ve Alarm Merkezi)

Prometheus veriyi toplar ama bu veri karmaşık sayılardan ibarettir. Grafana ise bu sayıları okur ve o meşhur şık, karanlık temalı gösterge panellerine (Dashboard) dönüştürür. Ayrıca CPU %80'i geçerse sana Slack veya Mail üzerinden alarm gönderen sistem de Grafana'dır.

---

### Bulut Mimarisinde Nasıl Çalışacağız?

Kendi sunucumuza devasa bir Prometheus kurmak yerine, Grafana'nın geliştirdiği çok hafif bir **Grafana Agent (veya yeni adıyla Grafana Alloy)** kuracağız.

Bu küçük ajan senin K3s cluster'ının içine yerleşecek, her bir podun (React, .NET, Postgres vb.) sağlık durumunu ve kaynak tüketimini toplayacak ve bunları tek yönlü, güvenli bir şekilde senin Grafana Cloud hesabına (internete) fırlatacak.

### İlk Adım: Grafana Cloud Bağlantısı

Bu entegrasyonu kurmak için Grafana Cloud'un bize özel üreteceği kurulum komutlarına (YAML dosyalarına) ve güvenlik anahtarlarına ihtiyacımız var.

Bunun için şu adımları takip etmelisin:

1. Tarayıcından **Grafana Cloud** paneline giriş yap.
2. Sol menüden **Connections** (Bağlantılar) -> **Add new connection** (Yeni bağlantı ekle) kısmına tıkla.
3. Arama çubuğuna **Kubernetes Monitoring** yaz ve çıkan K8s ikonuna tıkla.
4. Karşına çıkan ekranda sana Cluster'ını Grafana'ya bağlaman için bazı adımlar ve komutlar (genellikle *Helm* veya *Grafana Agent* kurulum komutları) sunacaktır.

### Kubernetes Monitoring
Ekranda "App" başlığı altında gördüğün o mavi logolu **"Kubernetes Monitoring"** kartı bizim biletiniz. Lütfen o karta tıkla.

Tıkladıktan sonra karşına büyük ihtimalle bir kurulum sihirbazı çıkacak. Bu sihirbaz senden şunları isteyecek:

#### Monitoring Type
1. **Cluster Name (Küme Adı):** Buraya `ozgurvm2-cluster` gibi ayırt edici bir isim yazabilirsin.
2. **Kurulum Yöntemi (Installation Method):** Grafana genellikle bu entegrasyon için **Helm** adlı aracı kullanmanı şiddetle tavsiye eder (ve sana buna uygun komutlar üretir).

#### Backend and token: `ozgurvm2-cluster-token`

#### Helm
```bash
helm repo add grafana https://grafana.github.io/helm-charts &&
  helm repo update &&
  helm upgrade --install --timeout 300s grafana-k8s-monitoring grafana/k8s-monitoring \
    --version "^4" --namespace "default" --create-namespace --values - <<'EOF'
cluster:
  name: ozgurvm2-cluster
destinations:
  grafana-cloud-metrics:
    type: prometheus
    url: https://prometheus-prod-58-prod-eu-central-0.grafana.net./api/prom/push
    auth:
      type: basic
      username: "3099622"
      password: ${GRAFANA_TOKEN}
  grafana-cloud-logs:
    type: loki
    url: https://logs-prod-039.grafana.net./loki/api/v1/push
    auth:
      type: basic
      username: "1545486"
      password: ${GRAFANA_TOKEN}
  gc-otlp-endpoint:
    type: otlp
    url: https://otlp-gateway-prod-eu-central-0.grafana.net./otlp
    protocol: http
    auth:
      type: basic
      username: "1587697"
      password: ${GRAFANA_TOKEN}
    metrics:
      enabled: true
    logs:
      enabled: true
    traces:
      enabled: true
  grafana-cloud-profiles:
    type: pyroscope
    url: https://profiles-prod-024.grafana.net.:443
    auth:
      type: basic
      username: "1587697"
      password: ${GRAFANA_TOKEN}
clusterMetrics:
  enabled: true
  collector: alloy-metrics
hostMetrics:
  enabled: true
  collector: alloy-metrics
  linuxHosts:
    enabled: true
  windowsHosts:
    enabled: true
  energyMetrics:
    enabled: true
costMetrics:
  enabled: true
  collector: alloy-metrics
clusterEvents:
  enabled: true
  collector: alloy-singleton
podLogsViaLoki:
  enabled: true
  collector: alloy-logs
applicationObservability:
  enabled: true
  collector: alloy-receiver
  receivers:
    otlp:
      grpc:
        enabled: true
        port: 4317
      http:
        enabled: true
        port: 4318
    zipkin:
      enabled: true
      port: 9411
autoInstrumentation:
  enabled: true
  collector: alloy-metrics
  beyla:
    deliverTracesToApplicationObservability: false
profiling:
  enabled: true
  collector: alloy-profiles
collectors:
  alloy-metrics:
    presets:
      - clustered
      - statefulset
  alloy-singleton:
    presets:
      - singleton
  alloy-logs:
    presets:
      - filesystem-log-reader
      - daemonset
  alloy-receiver:
    presets:
      - deployment
  alloy-profiles:
    presets:
      - privileged
      - daemonset
collectorCommon:
  alloy:
    remoteConfig:
      enabled: true
      url: https://fleet-management-prod-024.grafana.net
      auth:
        type: basic
        username: "1587697"
        password: ${GRAFANA_TOKEN}
telemetryServices:
  kube-state-metrics:
    deploy: true
  node-exporter:
    deploy: true
  windows-exporter:
    deploy: true
  opencost:
    deploy: true
    metricsSource: grafana-cloud-metrics
    opencost:
      exporter:
        defaultClusterId: ozgurvm2-cluster
      prometheus:
        existingSecretName: grafana-cloud-metrics-grafana-k8s-monitoring-k8s-monitoring
        external:
          url: https://prometheus-prod-58-prod-eu-central-0.grafana.net./api/prom
  kepler:
    deploy: true
EOF
```

#### Configure application instrumentation
After the Helm chart is deployed, configure your application instrumentation to send telemetry data to Grafana Alloy using one of these addresses:
* OTLP/gRPC endpoint: `http://grafana-k8s-monitoring-alloy-receiver.default.svc.cluster.local:4317`
* OTLP/HTTP endpoint: `http://grafana-k8s-monitoring-alloy-receiver.default.svc.cluster.local:4318`
* Zipkin endpoint: `grafana-k8s-monitoring-alloy-receiver.default.svc.cluster.local:9411`

### Helm Nedir ve Neden Lazım?

Nasıl ki Ubuntu'da paket kurmak için `apt`, Node.js'te `npm`, .NET'te `nuget` kullanıyorsan; Kubernetes dünyasında da karmaşık uygulamaları (örneğin Grafana Agent gibi onlarca YAML dosyasından oluşan sistemleri) tek bir komutla kurmak için **Helm** (Kubernetes Paket Yöneticisi) kullanılır.

Sihirbazdaki adımlara devam et. Ekranda sana `helm repo add grafana...` ile başlayan komutlar verdiğinde bana haber ver.

Eğer sunucunda henüz Helm yüklü değilse, o komutları girmeden önce şu sihirli komutla hemen `ozgurvm2` sunucuna Helm'i kuralım:

```bash
curl -fsSL -o get_helm.sh https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3
chmod 700 get_helm.sh
./get_helm.sh

```

Grafana'nın sana verdiği o özel anahtarlı (Token) kurulum komutlarını ekranda gördün mü?



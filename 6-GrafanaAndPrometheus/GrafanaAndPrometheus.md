# Grafana ve Prometheus

Eğer Grafana ve Prometheus'u tamamen kendi sunucumuzun içine (K3s'e) kursaydık, sadece bu izleme araçları sunucunun 1-2 GB RAM'ini ve CPU'sunun bir kısmını kendilerine ayıracaktı. Ancak Grafana Cloud kullanarak **"Görselleştirme ve Depolama"** yükünü buluta devrediyoruz. Sunucumuzun gücü yine tamamen senin yazdığın kodlara kalıyor!

İşe bu iki efsanevi aracın K8s dünyasındaki görevlerini netleştirerek başlayalım:

### 1. Prometheus (Hafıza ve Toplayıcı)

Prometheus bir veritabanıdır (Time-Series Database). K8s cluster'ının içinde bir ajan gibi dolaşır. "Nginx pod'u ne kadar RAM yiyor?", ".NET API saniyede kaç istek alıyor?", "Sunucunun SSD'sinde ne kadar boş yer kaldı?" gibi binlerce sorunun cevabını her 15 saniyede bir toplar ve kaydeder.

### 2. Grafana (Vitrin ve Alarm Merkezi)

Prometheus veriyi toplar ama bu veri karmaşık sayılardan ibarettir. Grafana ise bu sayıları okur ve o meşhur şık, karanlık temalı gösterge panellerine (Dashboard) dönüştürür. Ayrıca CPU %80'i geçerse sana Slack veya Mail üzerinden alarm gönderen sistem de Grafana'dır.

---

## Helm Nedir ve Neden Lazım?

Nasıl ki Ubuntu'da paket kurmak için `apt`, Node.js'te `npm`, .NET'te `nuget` kullanıyorsan; Kubernetes dünyasında da karmaşık uygulamaları (örneğin Grafana Agent gibi onlarca YAML dosyasından oluşan sistemleri) tek bir komutla kurmak için **Helm** (Kubernetes Paket Yöneticisi) kullanılır.

Sihirbazdaki adımlara devam et. Ekranda sana `helm repo add grafana...` ile başlayan komutlar verdiğinde bana haber ver.

Eğer sunucunda henüz Helm yüklü değilse, o komutları girmeden önce şu sihirli komutla hemen `ozgurvm2` sunucuna Helm'i kuralım:

```bash
curl -fsSL -o get_helm.sh https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3
chmod 700 get_helm.sh
./get_helm.sh
```

---

## Bulut Mimarisinde Nasıl Çalışacağız?

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

#### Helm: [Grafana.yml](grafana.yml)

#### Configure application instrumentation
After the Helm chart is deployed, configure your application instrumentation to send telemetry data to Grafana Alloy using one of these addresses:
* OTLP/gRPC endpoint: `http://grafana-k8s-monitoring-alloy-receiver.default.svc.cluster.local:4317`
* OTLP/HTTP endpoint: `http://grafana-k8s-monitoring-alloy-receiver.default.svc.cluster.local:4318`
* Zipkin endpoint: `grafana-k8s-monitoring-alloy-receiver.default.svc.cluster.local:9411`

## Sunucuya Kurulum
Grafana'nın sana verdiği o özel anahtarlı (Token) ve Helm kurulum komutlarını aldıysak sonraki aşamaya geçelim.

Şimdi DevOps sürecimizin en keyifli anlarından birine, yani **Sistemi Gözlemleme (Observability)** aşamasına geçiyoruz. Grafana'nın sana verdiği o komut bloğu, `ozgurvm2` sunucunun içine "Grafana Alloy" adında çok hafif bir ajan yerleştirecek ve bu ajan, sistemdeki tüm metrikleri senin bulut hesabına fırlatacak.


### Adım 1: Grafana Komutlarını Çalıştır

Grafana Cloud ekranında kopyaladığın o komut bloğunu (genellikle `helm repo add ...` ile başlar ve `helm upgrade --install ...` ile devam eder) doğrudan terminaline yapıştır ve çalıştır.

### Adım 2: Ajanların Uyanmasını Bekle

Helm komutu başarıyla tamamlandığında, sana "Happy Helming!" veya "Deployed successfully" gibi bir mesaj verecektir.
Ajanların çalışmaya başlayıp başlamadığını K8s üzerinden teyit edelim. Grafana genellikle bu kurulumu ya `default` namespace'ine ya da kendine özel bir namespace'e (örneğin `grafana-k8s-monitoring`) kurar. Sistemi tam görmek için şu komutu çalıştır:

```bash
kubectl get pods -A
```

Listede adında `alloy`, `grafana-agent` veya `kube-state-metrics` geçen yeni Pod'ların `Running` durumuna gelmesini bekle (1-2 dakika sürebilir).

### Adım 3: Hatalara Çözümler

#### **K3s İçin Kritik Bir İpucu:** > Normalde `kubectl` komutların sorunsuz çalışıyor ancak Helm, K3s'in yapılandırma dosyasının nerede olduğunu bazen bulamayabilir. Eğer komutu çalıştırdığında `Kubernetes cluster unreachable` veya `could not get apiVersions` gibi bir hata alırsan, komutu çalıştırmadan hemen önce terminale şu satırı yapıştırarak Helm'e yolu göster:
```bash
export KUBECONFIG=/etc/rancher/k3s/k3s.yaml
```

#### **Eğer `--rollback-on-failure` şeklinde hata alırsan**
İşte DevOps dünyasının cilvelerinden biri daha! Bazen devasa bulut şirketlerinin (Grafana) otomatik ürettiği script'ler bile güncel araçlarla uyumsuzluk yaşayabiliyor.

Kodu çalıştırdığın güncel Helm aracı (v3.20.2), Grafana'nın script'inin içine koyduğu `--rollback-on-failure` adındaki parametreyi (flag) tanımıyor. Helm dünyasında bu işlem için genellikle `--atomic` parametresi kullanılır.

Tek yapman gereken, o kopyaladığın uzun komut bloğunun ilk birkaç satırından bu sorunlu parametreyi silmek.

Komutun baş tarafını şu şekilde düzenleyip, altındaki o uzun `cluster: ...` kısmını kendi şifrelerinle aynı kalacak şekilde tekrar terminale yapıştırabilirsin:

*(Dikkat edersen 3. satırdaki `--rollback-on-failure` kısmını sildik, sadece `--timeout 300s` kaldı).*

Bu düzeltilmiş haliyle komutu çalıştırdıktan sonra `kubectl get pods -A` ile baktığında, K3s ortamında `grafana-k8s-monitoring` ile başlayan o gözlemci ajanların `Running` durumuna geçtiğini görebildin mi?


#### **Kubeconfig Dosyasını Sahiplenmek**
Hata: `Error: Kubernetes cluster unreachable: error loading config file "/etc/rancher/k3s/k3s.yaml": open /etc/rancher/k3s/k3s.yaml: permission denied`
İşte bu da K3s'in meşhur güvenlik önlemlerinden biridir! Karşılaştığın hata tamamen yetki (permission) kaynaklı.

Hata mesajında diyor ki: *"K3s'in yapılandırma dosyasının yerini buldum (/etc/rancher/k3s/k3s.yaml) ama ben `ozgur` kullanıcısıyım ve bu dosyayı sadece `root` (yönetici) okuyabilir. İçeri girmeme izin verilmedi!"*
Bu, DevOps süreçlerinde çok sık rastlanan bir durumdur. Her seferinde `sudo` yazmakla veya yolları export etmekle uğraşmamak için, gerçek bir sistem yöneticisi (SysAdmin) gibi bu konuyu **kalıcı olarak** çözelim.

Çözüm: **Kubeconfig Dosyasını Sahiplenmek**
K3s'in sadece `root`'a ait olan o gizli yapılandırma dosyasını kopyalayıp, kendi kullanıcının (`ozgur`) ana dizinine alacağız ve sahipliğini sana vereceğiz. Böylece hem `helm` hem de `kubectl` artık senden hiçbir ek yetki istemeyecek.
Sırasıyla şu üç komutu terminaline yapıştır:

**1. Kendi kullanıcında gizli bir `.kube` klasörü oluştur:**
```bash
mkdir -p ~/.kube
```

**2. K3s'in ayar dosyasını bu klasöre kopyala (Burada sudo gerekiyor):**
```bash
sudo cp /etc/rancher/k3s/k3s.yaml ~/.kube/config
```

**3. Dosyanın sahipliğini (owner) `root` kullanıcısından alıp `ozgur` kullanıcısına ver:**
```bash
sudo chown $USER:$USER ~/.kube/config
```

*(İpucu: Bu işlemden sonra daha önce yazdığımız `export KUBECONFIG...` komutuna da artık gerek kalmadı, sistem varsayılan olarak bu yeni kopyaladığımız dosyaya bakacak).*


**Hafızayı Temizleme**
Terminale şu komutu yazarak o ortam değişkenini sıfırla (Bunu yazınca sistem artık otomatik olarak senin yeni `~/.kube/config` dosyana bakacaktır):

```bash
unset KUBECONFIG
```

**Şimdi Tekrar Sahneye Çıkıyoruz**
Bu sefer `Permission denied` hatası almadan doğrudan kuruluma geçecek. Kurulum bittikten sonra `kubectl get pods -A` yazdığında o `grafana-k8s-monitoring-alloy...` isimli ajanların doğduğunu görebiliyor musun?


#### *"service port not found" hatası:* `2026-05-11T09:04:40Z ERR Cannot create service error="service port not found" ingress=ana-yonlendirici namespace=default providerName=kubernetes serviceName=postgres-service servicePort=&ServiceBackendPort{Name:,Number:80,}`

Hemen logu bir DevOps gözüyle tercüme edeyim:
Traefik diyor ki: *"Bana `ana-yonlendirici` adında bir kural (Ingress) verdin. Bu kuralın içinde trafiği `postgres-service` isimli servise **80 portu (HTTP)** üzerinden yönlendirmemi istiyorsun. Ancak ben `postgres-service`'in içine baktım, orada 80 diye bir port yok! Sadece 5432 var. Ben bu servisi oluşturamıyorum!"*

Hatırlarsan Ingress sürecini konuşurken bu konuya değinmiş ve Postgres'in bir web sitesi olmadığını, Layer 4 (TCP) seviyesinde 5432 portundan çalıştığını söylemiştik. Biz o YAML dosyasının temiz halini konuşmuştuk ama görünüşe göre ilk uyguladığın o 5. kural (Postgres kuralı), K3s'in hafızasında asılı kalmış.

**Çözüm: K8s Üzerinde "Canlı Ameliyat" (Live Edit)**

Hazır işin mutfağındayken sana çok havalı ve hayat kurtaran bir DevOps numarası göstereyim. YAML dosyasıyla uğraşmadan, doğrudan K3s'in beynindeki kuralı anında değiştireceğiz.

K8s'te objeleri canlı olarak düzenlemek için `kubectl edit` komutu kullanılır. Ancak varsayılan olarak zorlu `vi` editörüyle açılır. Biz bunu senin aşina olduğun `nano` ile açması için küçük bir hile yapacağız.

Terminale şu komutu yapıştır:

```bash
KUBE_EDITOR="nano" kubectl edit ingress ana-yonlendirici
```

Karşına K8s'in şu an hafızasında tuttuğu gerçek Ingress kuralları gelecek. Ok tuşlarıyla en aşağıya in. Orada şu bloğu göreceksin:

```yaml
  - host: postgres.ozgur.lokal
    http:
      paths:
      - backend:
          service:
            name: postgres-service
            port:
              number: 80
        path: /
        pathType: Prefix
```

Bu `postgres.ozgur.lokal` ile başlayan kuralın **tamamını sil**. (Dikkat et, `web.ozgur.lokal` veya diğer kurallara dokunma).

Sildikten sonra bildiğimiz gibi kaydedip çık (`Ctrl+O`, `Enter`, `Ctrl+X`).

Ekranda `ingress.networking.k8s.io/ana-yonlendirici edited` yazısını gördüğünde işlem tamamdır! Traefik anında "oh be" diyecek ve Grafana'daki o "service port not found" hatası anında kesilecektir.


### Adım 4: Grafana Cloud'da Hasat Vakti

Pod'lar `Running` olduktan sonra tarayıcından Grafana Cloud paneline geri dön.

* Kurulumu yaptığın ekranda **"Test connection"** veya **"View Dashboards"** gibi bir buton belirecektir.
* Ona tıkladığında sol menüden **Dashboards** kısmına git. Grafana senin için "Kubernetes / Compute Resources / Cluster" gibi hazır, karanlık temalı ve harika Dashboard'lar oluşturmuş olacak.

Komutları çalıştırdıktan sonra o efsanevi Dashboard ekranlarına ulaşabildin mi, yoksa Helm kurulumunda herhangi bir pürüzle karşılaştık mı?


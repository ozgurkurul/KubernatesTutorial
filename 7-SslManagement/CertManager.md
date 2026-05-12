# Kurumsal SSL ve Traefik Yapılandırması

Docker üzerinde Cloudflare DNS-01 challenge yöntemini kullanarak `*.ozgurkurul.com.tr` için SSL sertifikası alıyordun. Şimdi bu yapıyı K3s üzerinde çok daha profesyonel bir araç olan **Cert-Manager** ile kurgulayacağız.

Bu sistemin güzelliği şu: Sertifikaların süresi dolmadan Cert-Manager sessizce Cloudflare'e gidecek, sertifikayı yenileyecek ve Traefik bu yeni sertifikayı anında kullanmaya başlayacak.

### Adım 1: Cert-Manager Kurulumu

Önce K8s'in SSL yöneticisini kuralım:

```bash
# Helm deposunu ekle
helm repo add jetstack https://charts.jetstack.io
helm repo update

# Cert-Manager'ı kur
helm install cert-manager jetstack/cert-manager \
  --namespace cert-manager \
  --create-namespace \
  --set installCRDs=true
```

### Adım 2: Cloudflare API Token'ı K8s'e Tanıtmak

Traefik dosyadaki `CF_DNS_API_TOKEN` bilgisini hatırlıyor musun? Bu şifreyi K8s içinde güvenli bir "Secret" olarak saklamalıyız:

```bash
kubectl create secret generic cloudflare-api-token-secret \
  --from-literal=api-token=${CLOUDFLARE_API_TOKEN} \
  --namespace cert-manager

kubectl create secret generic cloudflare-api-token-secret \
  --from-literal=api-token="${CLOUDFLARE_API_TOKEN}" \
  -n default
```

### Adım 3: "Sertifika Üreticisi" (ClusterIssuer) Tanımlama

Şimdi K3s'e, "Sertifika lazım olduğunda bu token ile Cloudflare'e git" talimatını veriyoruz. Aşağıdaki kodu `[cloudflare-issuer.yaml](cloudflare-issuer.yaml)` adıyla kaydet:

Kaydet ve uygula: `kubectl apply -f cloudflare-issuer.yaml`

Bu yapı kurulduktan sonra, kuracağımız tüm servisler (PostgreSQL, MongoDB, Kafka, .NET API vb.) otomatik olarak gerçek ve güvenli SSL sertifikalarına sahip olacak.

Cert-Manager kurulumunu tamamlayıp `kubectl get pods -n cert-manager` dediğinde ajanların ayağa kalktığını görebiliyor musun? Sonrasında Layer 1'e, yani veritabanlarını Helm ile kurmaya geçelim.


## Wildcard Sertifikayı Üretmek

**Cloudflare DNS-01 Challenge** kullanarak Wildcard (`*.ozgurkurul.com.tr`) sertifika almak, sektördeki en "Pro" seviye çözümlerden biridir.

Bu sayede sunucunun 80 (HTTP) portunu internete açmak zorunda kalmadan, doğrudan DNS kayıtları üzerinden Let's Encrypt sertifikası üretebiliriz. Gönderdiğin dosyayı incelediğimde, üretim (Production) ortamı için Let's Encrypt sunucusunun (`https://acme-v02.api.letsencrypt.org/directory`) doğru bir şekilde yapılandırıldığını görüyorum. Sadece `privateKeySecretRef` isminde `letsencrypt-staging` yazmışsın ama URL prod olduğu için gerçek ve geçerli bir sertifika üretecektir.

Madem bu muazzam altyapıya (Cert-Manager + Cloudflare) sahipsin, hadi SNI (Domain) tabanlı ve tamamen şifrelenmiş (TLS) o kusursuz veritabanı yönlendirmemizi 3 adımda kuralım:

### 1. Adım: Wildcard Sertifikayı Üretmek

Cert-Manager'ın o yapılandırdığın `cloudflare-issuer` üzerinden bize gerçek bir SSL sertifikası vermesi ve bunu K3s içinde bir Secret olarak tutması gerekiyor.

YAML dosyasını `[ozgurkurul-cert.yaml](ozgurkurul-cert.yaml)` olarak kaydet ve uygula (`kubectl apply -f ozgurkurul-cert.yaml`):

*Not: K3s bu isteği aldıktan sonra Cloudflare ile konuşup sertifikayı alması 1-2 dakika sürebilir. Durumu `kubectl get certificate -n default` komutuyla "READY: True" olana kadar izleyebilirsin.*

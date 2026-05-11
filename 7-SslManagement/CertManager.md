# Kurumsal SSL ve Traefik Yapılandırması

Docker üzerinde Cloudflare DNS-01 challenge yöntemini kullanarak `*.ozgurkurul.com.tr` için SSL sertifikası alıyordun. Şimdi bu yapıyı K3s üzerinde çok daha profesyonel bir araç olan **Cert-Manager** ile kurgulayacağız.

Bu sistemin güzelliği şu: Sertifikaların süresi dolmadan Cert-Manager sessizce Cloudflare'e gidecek, sertifikayı yenileyecek ve Traefik bu yeni sertifikayı anında kullanmaya başlayacak.

#### Adım 1: Cert-Manager Kurulumu

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

#### Adım 2: Cloudflare API Token'ı K8s'e Tanıtmak

Traefik dosyadaki `CF_DNS_API_TOKEN` bilgisini hatırlıyor musun? Bu şifreyi K8s içinde güvenli bir "Secret" olarak saklamalıyız:

```bash
kubectl create secret generic cloudflare-api-token-secret \
  --from-literal=api-token=${CLOUDFLARE_API_TOKEN} \
  --namespace cert-manager
```

#### Adım 3: "Sertifika Üreticisi" (ClusterIssuer) Tanımlama

Şimdi K3s'e, "Sertifika lazım olduğunda bu token ile Cloudflare'e git" talimatını veriyoruz. Aşağıdaki kodu `[cloudflare-issuer.yaml](cloudflare-issuer.yaml)` adıyla kaydet:

Kaydet ve uygula: `kubectl apply -f cloudflare-issuer.yaml`

Bu yapı kurulduktan sonra, kuracağımız tüm servisler (PostgreSQL, MongoDB, Kafka, .NET API vb.) otomatik olarak gerçek ve güvenli SSL sertifikalarına sahip olacak.

Cert-Manager kurulumunu tamamlayıp `kubectl get pods -n cert-manager` dediğinde ajanların ayağa kalktığını görebiliyor musun? Sonrasında Layer 1'e, yani veritabanlarını Helm ile kurmaya geçelim.


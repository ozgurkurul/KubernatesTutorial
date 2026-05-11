# HashiCorp Vault'u K3s'e Kurma

Bu kurulumu yaparken Vault'u K3s sunucumuzda **"Dev Mode" (Geliştirici Modu)** ile kuracağız. Bunun önemli bir sebebi var: Vault'un Production (Canlı) sürümü aşırı güvenlikli olduğu için, sunucu her yeniden başladığında şifre kasasını açmak için 3 farklı kişinin 3 farklı "Unseal" anahtarını girmesini ister. Ev laboratuvarımızda otomasyonu bozmamak ve hemen ESO entegrasyonuna geçmek için Dev Mode en kusursuz çözümdür.

Hadi kendi yerel kasanızı inşa edelim:

### 1. Adım: HashiCorp Vault'u K3s'e Kurma

Önce Vault'un resmi Helm deposunu sunucumuza ekleyelim ve kurulumu başlatalım:

```bash
# HashiCorp deposunu ekle
helm repo add hashicorp https://helm.releases.hashicorp.com
helm repo update
```

# Vault'u Dev modunda ve sabit bir kök şifre ile kur
```bash
# 1. Namespace'i yeniden aç
kubectl create namespace vault

# 2. Vault'u sorunsuz parametrelerle kur
helm install vault hashicorp/vault \
  -n vault \
  --set "server.dev.enabled=true" \
  --set "server.dev.devRootToken=${HASHICORP_VAULT_DEV_ROOT_TOKEN}" \
  --set "server.extraEnvironmentVars.VAULT_DISABLE_MLOCK=true"
```

*Not: Kök şifreyi (Root Token) `${HASHICORP_VAULT_DEV_ROOT_TOKEN}` olarak belirledik.*

### 2. Adım: ESO'ya Vault'un Anahtarını Verme

External Secrets Operator'ın (ESO) bu kasaya bağlanıp içinden şifreleri alabilmesi için ona az önce belirlediğimiz kök şifreyi vermemiz gerekiyor. K3s terminalinden şu komutla şifreyi güvenli bir şekilde K8s içine kaydedelim:

```bash
# 3. Kasa köprümüz için K8s içine o temiz şifreyi (${HASHICORP_VAULT_DEV_ROOT_TOKEN}) tanımla
kubectl create secret generic vault-token \
  --namespace external-secrets \
  --from-literal=token=${HASHICORP_VAULT_DEV_ROOT_TOKEN}
```


### 3. Adım: Kasa Köprüsünü (ClusterSecretStore) Kurma

Şimdi ESO'ya, "Senin şifreleri çekeceğin ana kaynak bizim kendi sunucumuzdaki HashiCorp Vault'tur" diyoruz.

`[vault-store.yaml](vault-store.yaml)` adıyla içeriği ile kopyala.

**Uygula:** `kubectl apply -f vault-store.yaml`

---

### Lens Desktop'tan Başarıyı Teyit Edelim

Şimdi Lens Desktop'a geçip operasyonun başarısını izleyebiliriz:

1. **Namespaces:** Sol menüden `vault` isimli namespace'e gir ve **Pods** sekmesinde `vault-0` isimli podun yemyeşil (`Running`) olduğunu gör.
2. **ESO Bağlantısı:** Sol menüden **Custom Resources** -> **external-secrets.io** -> **ClusterSecretStore** yolunu izle.
3. Listede `vault-backend` satırını göreceksin. Karşısında **STATUS: Valid** yazıyorsa tebrikler!

Artık dışarıdaki hiçbir bulut sistemine (Cloudflare, AWS vb.) bağlı değilsin; kendi K3s sunucun içinde askeri düzeyde şifreleme yapan tam bağımsız bir Vault mimarisi kurdun.

Lens üzerinde Vault podunu ve `Valid` yazısını görebildin mi?

---

#### Vault'u ve ona ait tüm kalıntıları sistemden temizleme

Aşağıdaki komutları sırasıyla terminaline yapıştır. Bu işlemler Vault'u, hatalı şifreleri ve bozuk konfigürasyonları tamamen silecek:

```bash
# 1. Helm üzerindeki Vault kurulumunu kaldır
helm uninstall vault -n vault

# 2. Vault namespace'ini içindeki her şeyle (takılı kalan podlar dahil) birlikte yok et
kubectl delete namespace vault

# 3. External Secrets içindeki o eski/hatalı vault şifresini temizle
kubectl delete secret vault-token -n external-secrets --ignore-not-found
```

*(Not: `namespace` silme işlemi 10-15 saniye sürebilir, terminalin komutu tamamlamasını bekle.)*

---

## Vault Arayüzünü (UI) Lens Üzerinden Açmak

HashiCorp Vault'un dünya çapında bu kadar sevilmesinin sebebi harika bir görsel arayüze (Web UI) sahip olmasıdır. Lens sayesinde bu arayüze tek tıkla bağlanacağız:

### Sürecin Felsefesi: "Kasa, Kurye ve Vitrin"

Sistemi 3 ana aktörle düşünebilirsin:

1. **HashiCorp Vault (Kasa):** Şifrelerin şifrelenmiş halde tutulduğu, kimsenin dışarıdan doğrudan göremediği yüksek güvenlikli çelik kasadır.
2. **External Secrets Operator - ESO (Kurye):** Kasanın şifresini (root) bilen tek yetkilidir. Sen ona "Bana Vault'tan *postgres* şifresini getir" dersin (`ExternalSecret` kuralı ile). O da gider, kasadan alır ve K3s'in içine standart bir K8s Secret olarak bırakır.
3. **Lens Desktop (Vitrin/Kokpit):** Kuryenin getirdiği şifreleri gördüğün ve podları izlediğin ekrandır.

**Önemli Detay:** Lens Desktop doğrudan Vault'un *içini* göremez ve değiştiremez (güvenlik gereği). Lens, sadece ESO'nun (Kurye) getirdiği nihai şifreleri görebilir.

Peki biz bu şifreleri Vault içinde nasıl yöneteceğiz? Sürekli terminale komut mu yazacağız? **Hayır!**


### UI Lens Üzerinden Açmak

1. **Lens Desktop**'ı aç.
2. Sol menüden **Network** -> **Services** kısmına tıkla.
3. Çıkan listede `vault` isimli servisi bul ve üzerine tıkla.
4. Sağ taraftan açılan detay panelinde, aşağılara doğru **Ports** bölümünü göreceksin. Orada `8200/TCP` portunun yanındaki **"Forward" (İleri Ok/Bağlantı)** ikonuna tıkla.
5. Lens senin için otomatik olarak tarayıcında `http://localhost:XXXX` gibi bir sayfa açacaktır.

**Vault'a Giriş:**

* Karşına çıkan ekranda "Token" seçeneğini işaretle.
* Token kısmına `root` yaz ve "Sign In" de.

Hoş geldin! Şu an doğrudan i5 işlemcili sunucunda çalışan, banka düzeyindeki güvenlik kasanın arayüzündesin.

### Şifreleri Vault UI Üzerinden Yönetmek

Arayüze girdiğinde şifreleri yönetmek çok kolaydır:

1. Ekranda **`secret/`** yazan bir klasör göreceksin (buna Engine denir), ona tıkla.
2. Sağ üstten **"Create secret"** butonuna bas.
3. **Path for this secret:** Şifrenin adını yaz (Örn: `mongodb-credentials`).
4. **Secret Data:** Alt kısımda anahtar-değer (Key-Value) şeklinde şifrelerini ekle. Örneğin:
* Key: `username` | Value: `admin`
* Key: `password` | Value: `cokGizliSifre123`


5. **Save** diyerek kaydet.

Artık Kasa'da (Vault) yeni bir şifren var!

---

### Lens ile Süreci Tamamlamak

Şifreyi Vault arayüzünden ekledikten sonra, tek yapman gereken Kurye'ye (ESO) bunu K3s içine getirmesini söylemektir.

1. Lens Desktop'ta sol menüden **Custom Resources -> external-secrets.io -> ExternalSecret** kısmına gel.
2. Sağ alt köşedeki **"+"** butonuna bas.
3. Aşağıdaki gibi basit bir "Kurye Talimatı" yazıp kaydet:
```yaml
apiVersion: external-secrets.io/v1
kind: ExternalSecret
metadata:
  name: mongodb-secret-kurye # Kuralın adı
  namespace: default
spec:
  refreshInterval: "1h"
  secretStoreRef:
    name: vault-backend # Kasamızın adı
    kind: ClusterSecretStore
  target:
    name: mongo-auth # K3s içinde oluşacak son şifrenin adı
  data:
  - secretKey: mongodb-password # K3s'teki değişkenin adı
    remoteRef:
      key: secret/mongodb-credentials # Vault'taki dizin
      property: password # Vault'taki anahtar

```


4. Bunu kaydettiğin an Lens menüsünden **Config -> Secrets** sekmesine gidip, `mongo-auth` şifresinin saniyeler içinde oraya otomatik geldiğini gözlerinle görebilirsin.

**Özetle Workflow:** Vault UI üzerinden form doldurarak şifreni kasaya koy -> Lens üzerinden `ExternalSecret` kuralını ekle -> Şifrenin otomatik olarak K3s Secrets bölümüne gelmesini Lens'ten izle.




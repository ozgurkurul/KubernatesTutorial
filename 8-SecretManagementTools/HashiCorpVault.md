# HashiCorp Vault'u K3s'e Kurma

Bu kurulumu yaparken Vault'u K3s sunucumuzda **"Dev Mode" (Geliştirici Modu)** ile kuracağız. Bunun önemli bir sebebi var: Vault'un Production (Canlı) sürümü aşırı güvenlikli olduğu için, sunucu her yeniden başladığında şifre kasasını açmak için 3 farklı kişinin 3 farklı "Unseal" anahtarını girmesini ister. Ev laboratuvarımızda otomasyonu bozmamak ve hemen ESO entegrasyonuna geçmek için Dev Mode en kusursuz çözümdür.

Hadi kendi yerel kasanızı inşa edelim:

### HashiCorp Vault'u K3s'e Kurma

Önce Vault'un resmi Helm deposunu sunucumuza ekleyelim ve kurulumu başlatalım:

```bash
# HashiCorp deposunu ekle
helm repo add hashicorp https://helm.releases.hashicorp.com
helm repo update
```

---

## In-Memory ve Dev Modunda bir kök şifre ile kur

**"Dev Mode" (Geliştirme Modu)**: Vault'u `server.dev.enabled=true` parametresiyle kurduğumuzda, Vault varsayılan olarak **"In-Memory Storage" (Bellek İçi Depolama)** kullanır. Yani tüm şifreler sunucunun **RAM**'inde tutulur. Sunucu kapandığında veya `vault-0` podu yeniden başladığında RAM boşaltıldığı için tüm veriler uçar. Bu mod sadece hızlı testler içindir.

### 1. Adım: Vault'u memory cache parametrelerle kur

**Namespace'i yeniden aç**
```bash
kubectl create namespace vault
```

**Vault'u memory cache parametrelerle kur**
```bash
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

## Kalıcı Production-Ready Kurulum (Raft)

### Raft Nedir?

Raft, bir "Konsensüs Algoritması"dır. Normalde kurumsal dünyada 3 veya 5 sunuculu kümelerde "Hangi veri doğru?" kararını vermek için kullanılır. Biz senin tek sunuculu sisteminde bunu **"Persistence" (Kalıcılık)** katmanı olarak kullanacağız.

Raft sayesinde:

1. **Snapshot:** Vault belirli aralıklarla kasanın o anki fotoğrafını çeker ve diske yazar.
2. **Write-Ahead Log (WAL):** Her yeni "key" eklediğinde, önce bu işlem diske bir kütük olarak işlenir, sonra kasaya girer. Bu sayede elektrik kesilse bile veri kaybı imkansız hale gelir.

### Seal (Mühürleme) Kavramı

Kalıcı kuruluma geçtiğimizde karşımıza çıkan en büyük fark şudur: **Vault "Mühürlü" (Sealed) başlar.** Kasayı bir banka kasası gibi düşün; anahtarlar sende değilse, sunucuyu fiziksel olarak çalsalar bile içindeki veriyi okuyamazlar. Vault başladığında RAM'deki şifreleme anahtarlarını siler. Sen gelip "Unseal" anahtarlarını girmeden kasa açılmaz.

### 1. Adım: Kalıcı Kurulumu Başlatıyoruz

**Önemli:** Eğer daha önce temizlemediysen önce `helm uninstall vault -n vault` ile eskiyi sil.

#### 1. Kurulum Komutu (1 GB Disk Alanıyla)

```bash
helm install vault hashicorp/vault \
  -n vault \
  --create-namespace \
  --set "server.dev.enabled=false" \
  --set "server.dataStorage.enabled=true" \
  --set "server.dataStorage.size=1Gi" \
  --set "server.ha.enabled=true" \
  --set "server.ha.raft.enabled=true" \
  --set "server.ha.raft.setNodeId=true" \
  --set "server.extraEnvironmentVars.VAULT_DISABLE_MLOCK=true"
```

#### 2. Kasanın İlk Kurulumu (Initialize)

Pod ayağa kalktığında (`vault-0`), kasanın anahtarlarını oluşturmamız gerekiyor. Bu komutu **hayatında sadece bir kez** çalıştıracaksın:

```bash
kubectl exec -it vault-0 -n vault -- vault operator init
```

**DİKKAT:** Bu komut sana 5 tane **"Unseal Key"** ve 1 tane **"Initial Root Token"** verecek. Bunları hemen güvenli bir yere (Not defteri vb.) kopyala. Bunları kaybedersen kasanın içine bir daha asla giremeyiz.

#### 3. Kasanın Kilidini Açma (Unseal)

Şu an Lens'te bakarsan `vault-0` podu `0/1 Ready` görünecektir çünkü kilitli. Kilidi açmak için az önce aldığın 5 anahtardan 3 tanesini sırayla girmelisin:

```bash
# Bu komutu 3 farklı anahtar için 3 kez çalıştır:
kubectl exec -it vault-0 -n vault -- vault operator unseal
```

*(Her seferinde senden bir anahtar isteyecek. 3. anahtardan sonra pod `1/1 Ready` olacak).*

#### 4. Replica Vaults
Eğer `kubectl get pods -A` çıktısında hemen gözüme çarpan bir mimari detay var. `vault-0` aslanlar gibi çalışırken, `vault-1` ve `vault-2` podları **Pending (Beklemede)** kalmışsa.

Neden bekliyorlar? Çünkü biz Vault'a "HA (Yüksek Erişilebilirlik) modunda çalış" dedik. Vault'un resmi Helm şeması HA modunu gördüğü an varsayılan olarak **3 pod (replica)** ayağa kaldırmaya çalışır. Ancak K3s gibi tek sunuculu (Single-Node) yapılarda, güvenlik kuralları (Anti-Affinity) gereği 3 kasanın aynı fiziksel diske/sunucuya kurulmasına izin verilmez. Bu yüzden diğer ikisi sonsuza kadar `Pending` kalacaktır.

Sistem kaynaklarını (RAM/CPU) boş yere meşgul eden bu hayalet podları hemen temizleyelim ve Kurye'mize (ESO) yeni kasanın anahtarını verelim.

**1. Aşama: Hayalet Podları Temizleme (Optimizasyon)**

Vault'a "Biz tek sunucudayız, HA modunu sadece Raft (diske yazma) için kullanıyoruz, o yüzden sadece 1 pod çalıştır" diyeceğiz.

Mevcut ayarlarımızı koruyarak sadece kopya sayısını (replica) 1'e düşüren şu komutu çalıştır:

```bash
helm upgrade vault hashicorp/vault \
  -n vault \
  --reuse-values \
  --set "server.ha.replicas=1"
```

Bu komuttan sonra `kubectl get pods -n vault` yaptığında o `Pending` olan podların anında yok olduğunu ve ortamın tertemiz kaldığını göreceksin.


### 2. Aşama: Kuryeye (ESO) Yeni Anahtarı Verme

Kasamızı sıfırlayıp `init` komutuyla mühürlediğimiz için, daha önce Kurye'ye (ESO) verdiğimiz o basit `root` şifresi artık iptal oldu. Kasanın sana verdiği o **Initial Root Token**'ı ESO'ya tanıtmamız gerekiyor.

Lütfen aşağıdaki komutlardaki `${HASHICORP_VAULT_DEV_INITIAL_ROOT_TOKEN}` kısmına kendi yeni Root Token'ını yapıştırıp terminalde çalıştır:

```bash
# 1. Eski geçersiz şifreyi sil
kubectl delete secret vault-token -n external-secrets

# 2. Yeni Root Token'ı K3s'e kaydet
kubectl create secret generic vault-token \
  --namespace external-secrets \
  --from-literal=token="${HASHICORP_VAULT_DEV_INITIAL_ROOT_TOKEN}"
```

### 3. Aşama: Kasa Köprüsünü (ClusterSecretStore) Yenileme

Kurye yeni şifreyi aldı ama köprüyü bir kez dürtmemiz (tetiklememiz) gerekiyor ki gidip kasaya bağlanmayı tekrar denesin.

Daha önce oluşturduğumuz `[vault-store.yaml](vault-store.yaml)` dosyasını yeniden uygulayalım:

```bash
kubectl apply -f vault-store.yaml
```

Lens Desktop üzerinden **Custom Resources -> external-secrets.io -> ClusterSecretStore** sekmesine girip `vault-backend` kuralının karşısında tekrar **"Valid"** yazısını gördüğünden emin ol.

Bu 3 adımı tamamlayıp `Valid` yazısını gördüğün an, sistem %100 "Production-Ready" hale gelmiş olacak.


---

## Vault'u ve ona ait tüm kalıntıları sistemden temizleme

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


## Lens Desktop Kullanımı

### Sürecin Felsefesi: "Kasa, Kurye ve Vitrin"

Sistemi 3 ana aktörle düşünebilirsin:

1. **HashiCorp Vault (Kasa):** Şifrelerin şifrelenmiş halde tutulduğu, kimsenin dışarıdan doğrudan göremediği yüksek güvenlikli çelik kasadır.
2. **External Secrets Operator - ESO (Kurye):** Kasanın şifresini (root) bilen tek yetkilidir. Sen ona "Bana Vault'tan *postgres* şifresini getir" dersin (`ExternalSecret` kuralı ile). O da gider, kasadan alır ve K3s'in içine standart bir K8s Secret olarak bırakır.
3. **Lens Desktop (Vitrin/Kokpit):** Kuryenin getirdiği şifreleri gördüğün ve podları izlediğin ekrandır.

**Önemli Detay:** Lens Desktop doğrudan Vault'un *içini* göremez ve değiştiremez (güvenlik gereği). Lens, sadece ESO'nun (Kurye) getirdiği nihai şifreleri görebilir.

Peki biz bu şifreleri Vault içinde nasıl yöneteceğiz? Sürekli terminale komut mu yazacağız? **Hayır!**


### Lens Desktop'tan Başarıyı Teyit Edelim

Şimdi Lens Desktop'a geçip operasyonun başarısını izleyebiliriz:

1. **Namespaces:** Sol menüden `vault` isimli namespace'e gir ve **Pods** sekmesinde `vault-0` isimli podun yemyeşil (`Running`) olduğunu gör.
2. **ESO Bağlantısı:** Sol menüden **Custom Resources** -> **external-secrets.io** -> **ClusterSecretStore** yolunu izle.
3. Listede `vault-backend` satırını göreceksin. Karşısında **STATUS: Valid** yazıyorsa tebrikler!

Artık dışarıdaki hiçbir bulut sistemine (Cloudflare, AWS vb.) bağlı değilsin; kendi K3s sunucun içinde askeri düzeyde şifreleme yapan tam bağımsız bir Vault mimarisi kurdun.

Lens üzerinde Vault podunu ve `Valid` yazısını görebildin mi?


### Yeni Key Ekleme

Kasa açıldıktan sonra, Lens üzerinden Web UI'ya bağlanabilirsin (Token kısmına `root` değil, az önce `init` komutundan aldığın yeni **Root Token**'ı yazarak).

Artık `secret/test/api/track-me-in-the-road` yoluna istediğin kadar parametre ekle; sunucu kapansa da, K3s yeniden başlasın da verilerin o 1 GB'lık disk alanında sapasağlam kalacak.


## Vault Arayüzünü (UI) Lens Üzerinden Açmak

HashiCorp Vault'un dünya çapında bu kadar sevilmesinin sebebi harika bir görsel arayüze (Web UI) sahip olmasıdır. Lens sayesinde bu arayüze tek tıkla bağlanacağız:

### UI Lens Üzerinden Açmak

1. **Lens Desktop**'ı aç.
2. Sol menüden **Network -> Services** sekmesine git.
3. Listeden `vault` (veya `vault-active`) servisine tıkla.
4. Sağ taraftan açılan detay panelinde, aşağılara doğru **Ports** bölümünü göreceksin. Orada `8200/TCP` portunun yanındaki **"Forward" (İleri Ok/Bağlantı)** ikonuna tıkla.
5. Lens senin için otomatik olarak tarayıcında `http://localhost:XXXX` gibi bir sayfa açacaktır.
6. Tarayıcıda açılan ekranda **Token** seçeneğini işaretle ve şifreleme adımında aldığın o **Root Token** veya **Initial Root Token**'ı girerek, "Sign In" de ve içeri gir.


## Şifreleri Vault UI Üzerinden Yönetmek 

### Dev Mode Üzerinden Şifre Oluşturma
Arayüze girdiğinde şifreleri yönetmek çok kolaydır:

1. Ekranda **`secret/`** yazan bir klasör göreceksin (buna Engine denir), ona tıkla.
2. Sağ üstten **"Create secret"** butonuna bas.
3. **Path for this secret:** Şifrenin adını yaz (Örn: `mongodb-credentials`).
4. **Secret Data:** Alt kısımda anahtar-değer (Key-Value) şeklinde şifrelerini ekle. Örneğin:
* Key: `username` | Value: `admin`
* Key: `password` | Value: `cokGizliSifre123`

5. **Save** diyerek kaydet.

Artık Kasa'da (Vault) yeni bir şifren var!


### Production-Ready Üzerinden Şifre Oluşturma


Yalnız burada "Production-Ready" (Canlıya Hazır) Vault kullanmanın getirdiği ufak ama çok önemli bir mimari fark var: **Kasa şu an tamamen boş bir oda gibi.** "Dev Mode" kullanırken Vault bizim için `secret/` adında bir çekmeceyi otomatik hazırlıyordu. Şimdi gerçek bir DevOps uzmanı gibi o çekmeceyi (Secrets Engine) kendi ellerimizle kuracağız.

### 1. Adım: Çekmeceyi Kurma (Sadece İlk Sefere Mahsus)

1. Sol menüden **Secrets Engines**'e tıkla.
2. Sağ üstteki **"Enable new engine"** butonuna bas.
3. Çıkan listeden **"KV"** (Key-Value) seçeneğini işaretleyip aşağıdan **Next** de.
4. **Path:** `secret` yaz (küçük harfle).
5. **Version:** `2` olarak kalsın (versiyonlama özelliği sağlar).
6. **"Enable Engine"** diyerek çekmecemizi kur.

### 2. Adım: TrackMeInTheRoad Şifrelerini Ekleme

1. Az önce oluşturduğun **`secret/`** engine'ine tıkla.
2. Sağ üstten **"Create secret"** butonuna bas.
3. **Path for this secret** kısmına klasör hiyerarşimizi yaz:
`test/api/track-me-in-the-road`
4. **Secret Data** bölümünde, "Key" ve "Value" kutucuklarına o çift alt tireli (`__`) verilerimizi sırayla girmeye başla (her yeni veri için "Add" butonuna bas):
* **Key:** `ConnectionStrings__Database`
* **Value:** `Host=localhost;Port=5432;Database=TrackMeInTheRoad.Test;Username=sql_admin;Password=xxxxxxxxx;`
* **Key:** `Jwt__Issuer`
* **Value:** `TrackMeInTheRoadApi`
* **Key:** `Jwt__Audience`
* **Value:** `TrackMeInTheRoadApp`
* **Key:** `Jwt__SecretKey`
* **Value:** `xxxxxxxxx`
* **Key:** `Jwt__ExpirationInMinutes`
* **Value:** `1440`


5. Tüm verileri girdikten sonra en alttaki **"Save"** butonuna basarak kasanın kapısını kilitle!

İşlem tamam! Veriler şu an SSD'nde mühürlü ve güvende. Bu verileri Lens arayüzünde Vault içinde başarılı bir şekilde kaydedebildin mi? Gördüysen, bir sonraki adımda Kurye'mize (ESO) komut verip bu verileri K3s'in içine tek bir tıkla `track-me-test-env-bundle` olarak indireceğiz!





























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



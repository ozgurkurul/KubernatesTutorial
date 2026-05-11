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


## Vault'u ve ona ait tüm kalıntıları sistemden temizleme

### 1. Aşama: Tam Temizlik (Nükleer Seçenek)

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






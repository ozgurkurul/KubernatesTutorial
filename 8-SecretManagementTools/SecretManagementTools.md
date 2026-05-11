# Secret Management Tools

CLI kodlarıyla sürekli `secret` eklemek, yönettiğin projeler büyüdüğünde tam bir işkenceye ve güvenlik riskine dönüşebilir. Kubernetes dünyasında bu işi "manuel komut" olmaktan çıkarıp profesyonel bir sürece dönüştüren harika çözümler var.

### 1. En Profesyonel Yol: External Secrets Operator (ESO)

Bu, modern Kubernetes dünyasının en popüler "Secret Manager" çözümüdür.

* **Nasıl Çalışır?** Sen şifrelerini Kubernetes dışında bir yerde (örneğin HashiCorp Vault, AWS Secrets Manager hatta basit bir JSON dosyası) tutarsın. ESO, bu şifreleri otomatik olarak Kubernetes'in içine senkronize eder.
* **Neden Bunu Seçmelisin?** Şifre değiştirdiğinde sadece dışarıdaki merkezde değiştirmen yeterlidir; Kubernetes içindeki tüm servisler otomatik olarak güncellenir. CLI ile hiçbir işin kalmaz.

### 2. Görsel Yönetim: Lens Desktop veya Portainer

"Ben sadece görsel bir arayüz istiyorum, her seferinde komut yazmayayım" diyorsan:

* **Lens Desktop:** Kubernetes için dünyanın en iyi "IDE"sidir. Tüm Secret'larını, Deployment'larını ve servislerini bir tıklama ile oluşturabilir, düzenleyebilir ve silebilirsin. CLI'dan %100 kurtulursun.
* **Portainer (Kubernetes Mode):** Zaten Docker yapında Portainer kullanıyorsun. Portainer'ı K3s'e bağlayarak, Docker'daki gibi formlar üzerinden Secret yönetimi yapabilirsin.

### 3. GitOps Yolu: Sealed Secrets (Bitnami)

Eğer şifrelerini GitHub/GitLab üzerinde kodunla birlikte saklamak istiyorsan:

* **Nasıl Çalışır?** Şifrelerini şifreleyerek (`encrypted`) Git'e gönderirsin. Bu şifreleri sadece senin K3s cluster'ın çözebilir.
* **Faydası:** Sunucun çökse bile tüm şifrelerin kodunla birlikte güvenle saklanmış olur.

---

### Karşılaştırma Tablosu

| Özellik | Standart CLI | External Secrets (ESO) | Lens / Portainer |
| --- | --- | --- | --- |
| **Kullanım Kolaylığı** | Zor (Manuel) | Orta (Kurulumdan sonra oto) | **Çok Kolay (Görsel)** |
| **Otomasyon** | Yok | **Tam Otomatik** | Kısmen |
| **Güvenlik Standartı** | Düşük | **Çok Yüksek** | Orta |
| **RAM Tüketimi** | Yok | Çok Düşük (~50MB) | Yok (Desktop uygulama) |

---

## Lens Desktop Kurulumu

### 1. Adım: Lens Kurulumu (Kendi Bilgisayarına)

* Windows bilgisayarına [Lens Desktop](https://k8slens.dev/) indir ve kur.
* Kurduktan sonra `~/.kube/config` dosyanı Lens'e tanıt. Artık tüm cluster'ı (podlar, servisler, secretlar) karşında renkli ve interaktif bir panel olarak göreceksin.

### 2. Adım: Kimlik Kartını (Kubeconfig) Kopyala

Öncelikle SSH ile sunucuna bağlan ve terminale şu komutu yazarak yapılandırma dosyasının içeriğini ekrana bas:

```bash
sudo cat /etc/rancher/k3s/k3s.yaml

```

Ekranda `apiVersion: v1` ile başlayan uzun bir metin belirecek. Bu metnin **tamamını** seçip kopyala.

### 3. Adım: Windows Bilgisayarında Dosyayı Hazırla

1. Kendi Windows bilgisayarında masaüstüne gel ve `ozgurvm.yaml` adında boş bir metin belgesi oluştur.
2. Sunucudan kopyaladığın o uzun metni bu dosyanın içine yapıştır.
3. **Çok Önemli Bir Ayar:** Dosyanın içinde üst sıralarda şöyle bir satır göreceksin:
`server: https://127.0.0.1:6443`
Buradaki `127.0.0.1` kısmını silip, sunucunun **gerçek yerel IP adresini** (örneğin `192.168.1.121` veya `192.168.1.120`) yazmalısın.
*Son hali şuna benzemeli:* `server: https://192.168.1.121:6443`
4. Dosyayı kaydet ve kapat.

### 4. Adım: Lens Desktop'a Bağlama

1. Windows bilgisayarına kurduğun **Lens Desktop** uygulamasını aç.
2. Sağ alt veya sol üst köşede bulunan **"+"** (Add Cluster / Küme Ekle) butonuna tıkla. Alternatif olarak sol menüden **Catalog** -> **Clusters** -> sağ üstten **"+"** simgesini de kullanabilirsin.
3. Ekrana gelen büyük metin kutusuna (Sync Kubeconfig), az önce masaüstünde hazırladığın `ozgurvm.yaml` dosyasının içindeki metnin tamamını yapıştır. (Veya *Browse* diyerek dosyayı da seçebilirsin).
4. **"Add Cluster"** butonuna bas.

İşte bu kadar! Sol panelde sunucunun adını (genelde `default` olarak gelir) göreceksin. Üzerine tıkladığın an; işlemcinin, 16 GB RAM'inin anlık durumunu, çalışan podları, ağ kurallarını ve en önemlisi "Secret" (Şifreler) bölümünü rengarenk bir arayüzde görebileceksin.


### 5. Adım: Lens ile Şifre Oluşturma

1. **Lens Desktop**'ı aç ve cluster'ına tıkla.
2. Sol menüden **Config** -> **Secrets** sekmesine gir.
3. Sağ alt köşedeki mavi **"+"** (Create Secret) butonuna bas.
4. Açılan formda:
* **Name:** `postgres-auth` yaz.
* **Namespace:** `default` seçili kalsın.
* **Data:** Bölümünde **Key** kısmına `postgres-password`, **Value** kısmına ise Docker'da kullandığın o güçlü şifreni yaz.


5. **Create & Close** butonuna bas.

İşte bu kadar! Artık `postgres-auth` isimli şifren Kubernetes'in güvenli kasasında yerini aldı.



## K3s Üzerine External Secrets Operator (ESO) Kurulumu

```bash
# ESO için Helm deposunu ekle ve kur
helm repo add external-secrets https://charts.external-secrets.io
helm install external-secrets external-secrets/external-secrets -n external-secrets --create-namespace
```

### HashiCorp Vault Kurulumu
* [HashiCorp Vault Kurulumu](HashiCorpVault.md)


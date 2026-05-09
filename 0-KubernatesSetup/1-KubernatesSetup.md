# Kubernetes Kurulumu

### Neden Tam Sürüm (Vanilla) Kubernetes Değil de K3s?
Tam sürüm Kubernetes, sadece kendi yönetim bileşenleri (control plane) için 2-3 GB RAM tüketebilir. Rancher tarafından geliştirilen ve tamamen "production-ready" (canlı ortam onaylı) olan **K3s**, içindeki gereksiz eklentilerden arındırılmış tek bir binary dosyasıdır. Sadece **~500 MB RAM** harcar. 

### K3s ile Tam Sürüm (Vanilla) K8s Arasında Ne Fark Var?

K3s bir "çakma" veya "eksik" Kubernetes değildir. Cloud Native Computing Foundation (CNCF) tarafından **resmi olarak sertifikalandırılmış, %100 uyumlu bir Kubernetes dağıtımıdır.** Vanilla K8s'in geçtiği tüm uygunluk testlerinden geçer.

Peki Rancher ekibi K8s'i nasıl 50 MB'lık tek bir dosyaya sığdırdı? Şunları atarak:

1. **Bulut Sağlayıcı Çöplüğü (Cloud-Provider Bloat):** Vanilla K8s'in kaynak kodunda Amazon AWS, Google GCP, Azure, Alibaba Cloud gibi platformlara özel milyonlarca satır eski (in-tree) kod bulunur. K3s bunları silip atmıştır. (Zaten kendi çıplak sunucunda çalışıyorsun, bunlara ihtiyacın yok).
2. **Ağır Veritabanı (Etcd yerine SQLite):** K8s, cluster durumunu tutmak için `etcd` adında RAM canavarı bir veritabanı kullanır. K3s ise senin gibi tek sunuculu (single-node) kurulumlarda arka planda son derece hafif olan **SQLite** kullanır.
3. **Docker Bağımlılığı:** Zaten konuştuğumuz gibi, K3s doğrudan `containerd` ile gelir.
4. **Eski/Ölü Özellikler:** Alpha (deneysel) aşamasında kalmış, kimsenin kullanmadığı özellikleri koddan çıkarmışlardır.

### Geliştirme Sürecinde Bir Eksiklik Hisseder misin?

**Kesinlikle hayır. Koca bir sıfır.**

Mikroservis mimarileri tasarlarken, .NET API'lerini veya cross-platform frontend uygulamalarını K8s'e deploy ederken yazacağın YAML dosyalarının bir harfi bile değişmez. `kubectl` komutları %100 aynı çalışır.

Vanilla K8s kursaydın, sırf K8s'in kendi yönetim bileşenleri (Control Plane) sunucuya ayırdığın o 10 GB RAM'in 2-3 GB'ını yutacaktı. K3s ise şu an sadece 500 MB civarı RAM harcıyor ve geriye kalan tüm gücü senin uygulamalarına bırakıyor. Bir eksiklik hissetmek bir yana, performans açısından büyük bir avantaj yaşıyorsun. Tek eksiği, 10.000 sunuculuk bir cluster kurmak istersen yaşarsın (ki orada zaten bulut firmalarının K8s hizmetleri devreye girer).

---

Kurulumu çok basittir. Terminalden şu adımları izleyerek sunucunu saniyeler içinde bir Kubernetes Node'una çevirebiliriz:

### 1. K3s Kurulumu
Aşağıdaki komut, K3s'in en güncel stabil sürümünü indirip kuracak ve gerekli servisleri başlatacaktır:

```bash
curl -sfL https://get.k3s.io | sh -
```

### 2. İzinlerin Ayarlanması (Sudo Olmadan Kullanım)
Kurulum bittikten sonra Kubernetes'in yönetim komutu olan `kubectl`'i sürekli `sudo` ile yazmamak için yetki yapılandırmasını kendi kullanıcına kopyalamalısın:

```bash
mkdir -p ~/.kube
sudo cp /etc/rancher/k3s/k3s.yaml ~/.kube/config
sudo chown $(id -u):$(id -g) ~/.kube/config
```

Güvenlik duvarı kullanıyorsan (UFW vb.) K3s'in yerel ağda çalışabilmesi için şu portu açman iyi olur:
```bash
sudo ufw allow 6443/tcp
```

### 3. Kurulumu Test Etme
Artık kendi Kubernetes cluster'ının yöneticisisin! Her şeyin yolunda olduğunu görmek için şu komutları çalıştır:

```bash
# Node'un durumunu gör (Status "Ready" olmalı)
kubectl get nodes

# Arka planda çalışan sistem pod'larını gör
kubectl get pods --all-namespaces
```

### MacBook Üzerinden Uzaktan Yönetim (Bonus)
Bu işin en keyifli yanı, sunucuya hiç SSH yapmadan MacBook'un üzerinden cluster'ı yönetebilmektir. 
1. Sunucudaki `~/.kube/config` dosyasının içeriğini kopyala.
2. MacBook'unda `~/.kube/config` dosyası oluşturup içine yapıştır.
3. Dosyanın içindeki `server: [https://127.0.0.1:6443](https://127.0.0.1:6443)` satırını `server: [https://192.168.1.121:6443](https://192.168.1.121:6443)` olarak değiştir.
Artık MacBook terminalinde `kubectl get nodes` yazdığında doğrudan Ubuntu sunucundaki ortamı yönetebileceksin!

Komutları çalıştırdıktan sonra `kubectl get nodes` çıktısı "Ready" olarak dönüyor mu? Eğer öyleyse ilk test olarak basit bir Nginx imajı mı ayağa kaldıralım, yoksa doğrudan bir .NET API projesiyle mi deneme yapmak istersin?


### Hata
```Bash
ozgur@ozgurvm2:~$ kubectl get nodes
WARN[0000] Unable to read /etc/rancher/k3s/k3s.yaml, please start server with --write-kubeconfig-mode or --write-kubeconfig-group to modify kube config permissions
error: error loading config file "/etc/rancher/k3s/k3s.yaml": open /etc/rancher/k3s/k3s.yaml: permission denied
ozgur@ozgurvm2:~$ kubectl get pods --all-namespaces
WARN[0000] Unable to read /etc/rancher/k3s/k3s.yaml, please start server with --write-kubeconfig-mode or --write-kubeconfig-group to modify kube config permissions
error: error loading config file "/etc/rancher/k3s/k3s.yaml": open /etc/rancher/k3s/k3s.yaml: permission denied
```

Bu çok klasik bir K3s güvenlik önlemidir, endişelenecek hiçbir şey yok! 

K3s, güvenlik amacıyla varsayılan ayar dosyasını (`/etc/rancher/k3s/k3s.yaml`) sadece `root` kullanıcısının okuyabileceği şekilde ayarlar. `kubectl` komutu da doğrudan bu dosyaya bakmaya çalıştığı için "permission denied" (erişim engellendi) hatası alıyorsun.

Bunu kalıcı olarak çözmek ve `kubectl` komutunun senin kendi kullanıcı dizinindeki (`~/.kube/config`) ayar dosyasını okumasını sağlamak için terminale sırasıyla şu komutları yapıştır:

### Çözüm Adımları

**1. Ayar dosyasını kendi dizinine kopyala ve yetkilerini al:**
*(Eğer daha önce yaptıysan bile garanti olması için tekrar çalıştırabilirsin)*
```bash
mkdir -p ~/.kube
sudo cp /etc/rancher/k3s/k3s.yaml ~/.kube/config
sudo chown $(id -u):$(id -g) ~/.kube/config
```

**2. Sistem değişkenini kalıcı olarak kendi dosyana yönlendir:**
```bash
echo 'export KUBECONFIG=~/.kube/config' >> ~/.bashrc
source ~/.bashrc
```

---

Bu adımlardan sonra `kubectl` artık root yetkisi istemeden doğrudan senin ayar dosyanı kullanacaktır. Şimdi komutu tekrar test edelim:

```bash
kubectl get nodes
```

Ekranda `ozgurvm2` sunucunu **Ready** (Hazır) durumunda görüyor musun?

Harika! Tablo gibi bir çıktı. Kendi Kubernetes (K3s) ortamın şu an tamamen sağlıklı ve emrine amade.

Çıktıdaki listede gördüğün o sistem parçaları, aslında mikroservis mimarisinin can damarlarıdır. Hazır yeri gelmişken en önemlilerine hızlıca değineyim:
*   **`traefik`**: Sistemin yerleşik Load Balancer (Yük Dengeleyici) ve Ingress kontrolcüsüdür. Dışarıdan gelen web isteklerini (örneğin `api.ozgurkurul.com.tr` üzerinden gelen) içerideki doğru .NET servisine yönlendirecek olan trafik polisidir.
*   **`local-path-provisioner`**: Tam da yeni bir SATA SSD taktığımız bu dönemde en çok işine yarayacak parça. Veritabanı (PostgreSQL vb.) kurduğunda verilerin o hızlı SSD'ye kalıcı olarak yazılmasını bu arkadaş organize edecek.
*   **`coredns`**: İçerideki servislerin (örneğin `order-api`nin `payment-api`yi) IP adresi bilmeden isimleriyle birbirlerini bulmalarını sağlar.

### İlk Uygulamamızı (Hello World) Ayağa Kaldıralım

Sistemin çalıştığını tam olarak hissetmek için hızlıca standart bir Nginx web sunucusu ayağa kaldıralım ve dışarıdan (MacBook'undan veya Windows makinesinden) bu sunucuya erişelim.

Sırasıyla şu komutları çalıştır:

**1. Nginx uygulamasını (Deployment) oluştur:**
```bash
kubectl create deployment ilk-uygulamam --image=nginx
```

**2. Uygulamanın durumunu kontrol et:**
```bash
kubectl get pods
```
*(Ekranda `ilk-uygulamam-...` isimli pod'un durumunun `ContainerCreating`'den `Running`'e geçmesini bekle, 5-10 saniye sürebilir).*

**3. Uygulamayı yerel ağa aç (Service):**
Şimdi bu pod'u sunucunun dışından da erişilebilir hale getirmek için bir servis (NodePort) oluşturacağız:
```bash
kubectl expose deployment ilk-uygulamam --type=NodePort --port=80
```

**4. Erişilecek Portu Öğren:**
Kubernetes, uygulamanı dışarı açarken ona rastgele bir port atar. Bu portu öğrenmek için:
```bash
kubectl get svc ilk-uygulamam
```

Ekranda `80:XXXXX/TCP` şeklinde bir bölüm göreceksin. Oradaki **XXXXX** (genelde 30000 ile 32767 arasında bir sayıdır) senin dışarıya açılan portundur.

Şimdi hemen kendi bilgisayarının tarayıcısını açıp `[http://192.168.1.121](http://192.168.1.121):XXXXX` (XXXXX yerine bulduğun portu yazarak) adresine git. "Welcome to nginx!" yazısını gördün mü?

#### Sonuç
```bash
ozgur@ozgurvm2:~$ kubectl create deployment ilk-uygulamam --image=nginx
deployment.apps/ilk-uygulamam created
ozgur@ozgurvm2:~$ kubectl get pods
NAME                             READY   STATUS    RESTARTS   AGE
ilk-uygulamam-5dbdd9856b-j9bz6   1/1     Running   0          19s
ozgur@ozgurvm2:~$ kubectl expose deployment ilk-uygulamam --type=NodePort --port=80
service/ilk-uygulamam exposed
ozgur@ozgurvm2:~$ kubectl get svc ilk-uygulamam
NAME            TYPE       CLUSTER-IP    EXTERNAL-IP   PORT(S)        AGE
ilk-uygulamam   NodePort   10.43.143.3   <none>        80:32097/TCP   8s
```
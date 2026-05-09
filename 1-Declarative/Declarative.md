
## Konu 1: Kubernates Declarative (Bildirimsel) altyapı yönetimi

Daha önce `kubectl create...` yazarak Nginx kurmuştuk. Buna **Imperative (Emirsel)** yöntem denir. Ancak gerçek projelerde (özellikle sen bir ekibi yönetirken) kimse sunucuya girip elle komut yazmaz. Bunun yerine ne istediğimizi bir metin dosyasına (YAML) yazarız ve Kubernetes'e "Al bu dosyayı, sistemi bu hale getir" deriz. Buna da **Infrastructure as Code (Kod Olarak Altyapı)** denir.

Hadi başlayalım.

### 1. Adım: Temizlik Vakti
Önce eski yaptığımız manuel kurulumu silelim ki temiz bir sayfa açalım. Terminale şu iki komutu yazarak eski Nginx'i ve servisini yok et:
```bash
kubectl delete service ilk-uygulamam
kubectl delete deployment ilk-uygulamam
```

### 2. Adım: İlk YAML Dosyamızı Yazıyoruz
Şimdi sunucunda boş bir dosya oluşturacağız. Adı `nginx-deployment.yaml` olsun.

```bash
nano nginx-deployment.yaml
```

Açılan boş ekrana şu kodları kopyala ve yapıştır (YAML dosyalarında boşluklar çok önemlidir, baştaki hizalamalara dikkat et):

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: profesyonel-nginx
  labels:
    app: web-sunucu
spec:
  replicas: 3
  selector:
    matchLabels:
      app: web-sunucu
  template:
    metadata:
      labels:
        app: web-sunucu
    spec:
      containers:
      - name: nginx-container
        image: nginx:latest
        ports:
        - containerPort: 80
```
*(Kopyaladıktan sonra kaydetmek için `Ctrl+O`, `Enter`, çıkmak için `Ctrl+X` yapabilirsin).*

**Bu Dosyada Ne İstedik? (Kodun Anatomisi)**
*   **`kind: Deployment`**: K8s'e bir "Yönetici" oluşturmasını söyledik.
*   **`replicas: 3`**: K8s'e dedik ki; "Bana bu Nginx uygulamasından tam 3 kopya (Pod) çalıştır. Eğer biri çökerse anında yenisini aç ve sayıyı hep 3'te tut."
*   **`image: nginx:latest`**: Docker'dan bildiğin imaj adı. (İleride buraya kendi yazdığın .NET imajının adını koyacaksın).

### 3. Adım: Sihri Gerçekleştirme
Dosyamız hazır. Şimdi K8s'e bu dosyayı okutup hayata geçirmesini söyleyeceğiz. İşte DevOps'un en çok kullandığı komut:

```bash
kubectl apply -f nginx-deployment.yaml
```

Bu komutu çalıştırdığında sana "deployment.apps/profesyonel-nginx created" gibi bir mesaj dönmesi lazım.

**Şimdi İlk Sınavın:**
Komutu çalıştırdıktan hemen sonra terminale `kubectl get pods` yaz. K8s'in bizim için kaç tane Nginx pod'u oluşturduğunu görüyorsun? Çıktıyı benimle paylaşır mısın?


#### **Çıktı**
```bash
ozgur@ozgurvm2:~/test$ kubectl get pods
NAME                                 READY   STATUS    RESTARTS   AGE
profesyonel-nginx-554cb7499c-szfv6   1/1     Running   0          12s
profesyonel-nginx-554cb7499c-tzz9k   1/1     Running   0          12s
profesyonel-nginx-554cb7499c-wwlm7   1/1     Running   0          12s
```

Mükemmel! Çıktıda gördüğün o 3 satır, YAML dosyasında yazdığımız `replicas: 3` komutunun vücut bulmuş halidir. Artık altyapını kod (Infrastructure as Code) ile yönetiyorsun.

Şimdi Kubernetes'in (K8s) neden yazılım dünyasında bir devrim olduğunu, kendi kurduğun bu sistem üzerinde iki küçük **"kaos ve büyü" deneyi** yaparak kanıtlayalım.

---

### Deney 1: Kaos ve Kendi Kendini İyileştirme (Self-Healing)
Deployment'ın (Yöneticinin) işinin sadece Pod yaratmak değil, onları "hayatta tutmak" olduğunu söylemiştik. Diyelim ki sunuculardan birinde donanımsal bir hata oldu ve Nginx çöktü.

Bunu simüle etmek için o listedeki Pod'lardan birini acımasızca silelim. Terminale şu komutu yaz (kendi listendeki pod isimlerinden birini kopyalayabilirsin):

```bash
kubectl delete pod profesyonel-nginx-554cb7499c-szfv6
```

Bu komutu Enter'a bastıktan **hemen sonra** zaman kaybetmeden tekrar durum kontrolü yap:
```bash
kubectl get pods
```

**Ne Göreceksin?**
Sildiğin pod'un "Terminating" (Yok ediliyor) durumuna geçtiğini, ancak saniyesinde yepyeni bir pod'un "ContainerCreating" durumunda doğduğunu göreceksin. K8s panik yapmaz; YAML dosyasındaki sözleşmeye (replicas: 3) bakar ve "Şu an 2 tane var, hemen 1 tane daha açmalıyım" diyerek durumu düzeltir. Gece 3'te sunucun çökse bile senin uyanmana gerek kalmaz.

---

### Deney 2: Kod ile Ölçekleme (Declarative Scaling)
Diyelim ki uygulaman Black Friday kampanyasına girdi ve trafik aniden 5 katına çıktı. 3 Pod artık yetmiyor.

Eskiden olsa sunucuya girer, yeni Docker container'ları ayağa kaldırmaya çalışırdın. Şimdi K8s DevOps dünyasındasın. Komut vermek yok, sadece "istediğimiz durumu" değiştireceğiz:

**1.** YAML dosyanı tekrar aç:
```bash
nano nginx-deployment.yaml
```

**2.** `replicas: 3` yazan yeri `replicas: 5` olarak değiştir ve dosyayı kaydet.

**3.** K8s'e "Sözleşmeyi güncelledim, sistemi bu yeni duruma uydur" de:
```bash
kubectl apply -f nginx-deployment.yaml
```

Hemen ardından tekrar `kubectl get pods` komutunu çalıştır. Sistemin kesintiye uğramadan, var olan 3 Pod'a dokunmadan anında 2 yeni Pod daha ekleyip toplamı 5'e çıkardığını göreceksin.


---

## Service YAML dosyası

Harika! Madem Pod'larımızı (işçilerimizi) kodla yönettik, şimdi onları dış dünyaya açacak olan "Santrali" de kodla (YAML) inşa edelim.

Hatırlarsan Pod'ların IP adresleri sürekli değişir. Nginx'i 5'e çıkardık, hepsi farklı bir IP aldı. İşte **Service**, bu 5 Pod'un önüne geçen, IP'si asla değişmeyen ve gelen trafiği bu 5 Pod'a eşit dağıtan (Load Balancer) bir Kubernetes nesnesidir.

### 1. Adım: Service YAML Dosyamızı Yazıyoruz
Sunucunda yeni bir dosya oluştur, adı `nginx-service.yaml` olsun.

```bash
nano nginx-service.yaml
```

İçerisine şu kodları yapıştır:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: profesyonel-nginx-servisi
spec:
  type: NodePort
  selector:
    app: web-sunucu
  ports:
    - port: 80
      targetPort: 80
      nodePort: 32000
```

**Bu Kodda Ne Yaptık? (Burası Çok Kritik)**
*   **`selector: app: web-sunucu`**: İşte bütün sihir burada! Service'e diyoruz ki; "Git, üzerinde `app: web-sunucu` etiketi (label) olan tüm Pod'ları bul ve onlara bekçilik yap." Hatırlarsan Deployment YAML dosyamızda Nginx pod'larına tam olarak bu etiketi yapıştırmıştık.
*   **`type: NodePort`**: Bu servisi sunucumuzun dışına açmak istediğimizi belirtir.
*   **`targetPort: 80`**: Nginx'in içeride dinlediği port.
*   **`nodePort: 32000`**: Önceki manuel kurulumda K8s bize rastgele bir port (32097) vermişti. Artık altyapı kodla yönetildiği için ipler bizim elimizde. K8s'e "Bana rastgele port verme, bu servisi tam olarak 32000 portundan dışarı aç" dedik. *(NodePort aralığı varsayılan olarak 30000-32767'dir).*

### 2. Adım: Servisi Hayata Geçirme
Dosyayı kaydedip çık (`Ctrl+O`, `Enter`, `Ctrl+X`) ve ardından sihirli komutumuzu çalıştır:

```bash
kubectl apply -f nginx-service.yaml
```

Ve durumunu kontrol et:
```bash
kubectl get svc
```

Ekranda `profesyonel-nginx-servisi` adında ve `80:32000/TCP` portuyla çalışan yeni servisimizi görmelisin.

### 3. Adım: Test Et
Artık K8s üzerinde her şey senin kontrolünde. MacBook'unun tarayıcısından direkt olarak şu adrese gidebilirsin:
**`[http://192.168.1.121:32000](http://192.168.1.121:32000)`**

O sayfayı her yenilediğinde, K8s arka planda isteğini o an çalışan 5 farklı Nginx Pod'undan birine yönlendiriyor (Round-robin Load Balancing).


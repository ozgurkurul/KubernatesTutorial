# ConfigMap Oluşturma
`kubectl create configmap` komutu, Kubernetes üzerinde uygulama konfigürasyonlarını (veritabanı hostu, port bilgileri, flaglar vb.) saklamak için kullanılan ConfigMap nesnelerini komut satırı üzerinden hızlıca oluşturmanızı sağlar.
Bu komutu dört temel farklı yöntemle kullanabilirsiniz:

* Literal (Doğrudan Değer) ile Oluşturma:
Herhangi bir dosyaya ihtiyaç duymadan, anahtar-değer çiftlerini doğrudan komut içine yazarsınız.
`kubectl create configmap my-config --from-literal=anahtar1=deger1 --from-literal=anahtar2=deger2`
* Tek Bir Dosyadan Oluşturma:
Belirli bir dosyanın içeriğini ConfigMap olarak kaydeder. Dosya adı "anahtar", dosya içeriği ise "değer" olur.
`kubectl create configmap my-config --from-file=ayarlar.conf`
* Dizin İçindeki Tüm Dosyalardan Oluşturma:
Bir klasördeki tüm dosyaları tek seferde ConfigMap içine aktarır. Her dosya ayrı bir anahtar olarak kaydedilir.
`kubectl create configmap my-config --from-file=config-dizini/`
* Env Dosyasından Oluşturma:
.env dosyası gibi `KEY=VALUE` formatındaki dosyalardan veri çekmek için kullanılır.
`kubectl create configmap my-config --from-env-file=.env`

## Doğrulama ve Kontrol
Oluşturduğunuz ConfigMap'in içeriğini doğrulamak için şu komutları kullanabilirsiniz:

* Listeleme: `kubectl get configmaps`
* Detaylı Görüntüleme (YAML formatında): `kubectl get configmap my-config -o yaml`

Uygulamanızda bu verileri kullanmak için Pod tanımınızda env (environment variable) veya volume (dosya olarak mount etme) yöntemlerini tercih edebilirsiniz.


## Tüm Klasör İle Çalışma
Bulunduğun klasördeki tüm dosyaları (index.html, css ve js klasörleri dahil) tek bir ConfigMap içine aktarmak için en pratik yol, komutu nokta . (geçerli dizin) operatörü ile kullanmaktır.
Ancak burada dikkat etmen gereken kritik bir nokta var: ConfigMap'ler hiyerarşik klasör yapısını (sub-directories) desteklemez. Yani css/style.css gibi bir alt klasörün varsa, Kubernetes bunu tek bir anahtar olarak kaydetmeye çalışır. İşte kurgulama yöntemleri:

### 1. Klasördeki Her Şeyi Tek Komutla Almak
Eğer tüm dosyaların aynı dizindeyse veya alt klasörleri de düz bir yapıda içeri almak istiyorsan şu komutu kullanabilirsin:
`kubectl create configmap web-icerik --from-file=.`

Bu komut, bulunduğun dizindeki tüm dosyaları okur. Dosya isimlerini key (anahtar), içeriklerini ise value (değer) yapar.

### 2. Alt Klasörler Varsa (Önerilen Yöntem)
Eğer css/ ve js/ gibi klasörlerin varsa, en sağlıklı yöntem her klasör için ayrı bir ConfigMap oluşturmak veya hepsini farklı `--from-file` argümanlarıyla belirtmektir.
Ancak en temiz kurgu şudur:

```bash
kubectl create configmap site-files \
  --from-file=index.html \
  --from-file=css/ \
  --from-file=js/
```

Bu komutla index.html'i tekil olarak alır, css ve js klasörlerinin içindeki dosyaları da bu ConfigMap'e dahil eder.

### 3. Pod İçinde Klasör Yapısını Korumak
ConfigMap oluşturduktan sonra, bu dosyaları Pod'a "mount" ederken (bağlarken) klasör yapısını korumak için Pod YAML dosyasında şu kurguyu yapmalısın:

```bash
spec:
  containers:
  - name: nginx
    image: nginx
    volumeMounts:
    - name: html-vol
      mountPath: /usr/share/nginx/html # Dosyaların gideceği yer
  volumes:
  - name: html-vol
    configMap:
      name: site-files # Oluşturduğun ConfigMap'in adı
```

### Önemli Uyarılar:

* Boyut Limiti: Bir ConfigMap'in toplam boyutu 1 MB'ı geçemez. Eğer resimleriniz (png, jpg) veya çok büyük JS kütüphaneleriniz varsa ConfigMap yerine Persistent Volume veya bir Sidecar (git-sync gibi) kullanmalısınız.
* İsimlendirme: Dosya isimlerindeki karakterlere dikkat edin; Kubernetes anahtar isimlerinde sadece belirli karakterlere (harf, rakam, nokta, tire) izin verir.





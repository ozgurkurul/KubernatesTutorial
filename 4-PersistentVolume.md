# Persistent Volume

Önceki adımlarda kurduğumuz Nginx, React veya .NET API uygulamaları **"Stateless" (Durumsuz)** uygulamalardı. Yani Pod çöküp yenisi doğduğunda hiçbir şey kaybetmiyorduk, çünkü içeride sakladıkları bir veri yoktu. Ancak PostgreSQL gibi **"Stateful" (Durum Tutan)** bir uygulamada Pod silinirse, içindeki tüm veritabanı uçup gider. 

Bunu engellemek ve yeni taktığın hızlı SATA SSD'yi tam verimle kullanmak için Kubernetes'in **Persistent Volume (PV)** ve **Persistent Volume Claim (PVC)** mimarisini kullanacağız. 

Şimdi bu süreci adım adım inşa edelim:

### Adım 1: Çalışma Alanı ve Parola Yönetimi (Secret)

K8s dünyasında veritabanı şifreleri YAML dosyalarına açıkça yazılmaz. Bunun yerine K8s'in **Secret** nesnesi kullanılır.

Önce yeni bir klasör açalım:
```bash
mkdir -p ~/k8s-egitim/postgres
cd ~/k8s-egitim/postgres
```

Şifremizi tutacak Secret dosyasını oluşturalım:
```bash
nano postgres-secret.yaml
```
İçine şu kodu yapıştır:
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: postgres-secret
# Opaque, varsayılan şifreleme türüdür
type: Opaque 
stringData:
  POSTGRES_PASSWORD: "OzgurPassword123!"
```

### Adım 2: SSD'den Alan İstemek (PVC)

K3s, bare-metal (sunucu) kurulumlarında SSD diskini otomatik olarak yönetebilen `local-path-provisioner` adında harika bir araçla gelir. Biz sadece "Bana 2 GB kalıcı alan ver" diyeceğiz, o gidip SSD'de güvenli bir klasör oluşturup bunu veritabanına bağlayacak.

```bash
nano postgres-pvc.yaml
```
Şu kodu yapıştır:
```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: postgres-pvc
spec:
  accessModes:
    - ReadWriteOnce # Bu alanı aynı anda sadece 1 Pod okuyup yazabilir
  resources:
    requests:
      storage: 2Gi # SSD'den 2 GB'lık alan rezerve ediyoruz
```

### Adım 3: Veritabanını Ayağa Kaldırmak (Deployment & Service)

Şimdi PostgreSQL'i çalıştıracak ve biraz önce oluşturduğumuz PVC (Disk) ile Secret'ı (Şifre) içine monte edecek olan ana dosyamızı yazıyoruz.

```bash
nano postgres-k8s.yaml
```
Şu kodu yapıştır (Girintilere dikkat et):
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: postgres-db
spec:
  replicas: 1 # Veritabanları özel cluster yapılandırması yoksa genelde 1 kopya çalışır
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
      - name: postgres
        image: postgres:15-alpine
        ports:
        - containerPort: 5432
        env:
        - name: POSTGRES_USER
          value: "ozgur"
        - name: POSTGRES_DB
          value: "testdb"
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: postgres-secret
              key: POSTGRES_PASSWORD
        volumeMounts:
        - name: postgres-storage
          mountPath: /var/lib/postgresql/data # Postgres'in verilerini yazdığı varsayılan Linux dizini
      volumes:
      - name: postgres-storage
        persistentVolumeClaim:
          claimName: postgres-pvc # Adım 2'de oluşturduğumuz disk talebini buraya bağlıyoruz
---
apiVersion: v1
kind: Service
metadata:
  name: postgres-service
spec:
  type: NodePort
  selector:
    app: postgres
  ports:
    - port: 5432
      targetPort: 5432
      nodePort: 31543 # Dışarıdan DBeaver vb. ile bağlanmak için açtığımız port
```

### Adım 4: Sistemi Ateşle ve Test Et

Dosyalarımız hazır. Hepsini tek bir komutla K8s'e uygulayalım:
```bash
kubectl apply -f .
```
*(Bu komut, bulunduğun klasördeki tüm `.yaml` dosyalarını sırasıyla K8s'e uygular).*

Şimdi disk alanımızın SSD'de ayrılıp ayrılmadığını (Bound durumunu) kontrol et:
```bash
kubectl get pvc
```
Pod'un sorunsuz çalışıp çalışmadığına bak:
```bash
kubectl get pods
```

Pod `Running` durumuna geldiyse, MacBook'undaki herhangi bir veritabanı aracıyla (DBeaver, pgAdmin veya DataGrip) şu bilgilerle sunucuna bağlanmayı deneyebilirsin:
*   **Host:** `192.168.1.121`
*   **Port:** `31543`
*   **Database:** `testdb`
*   **Username:** `ozgur`
*   **Password:** `OzgurPassword123!`

Bağlantıyı başarıyla kurabildin mi? İşlemleri tamamladığında, o "Ölümsüz Veri" deneyini (Pod'u silip verilerin kaybolmadığını görme) test etmeye geçebiliriz.

### Adım 5: Veritabanına Test Verisi Yazalım
Önce PostgreSQL pod'unun tam adını öğrenelim:

```bash
ozgur@ozgurvm2:~/test/postgres$ kubectl get pods
NAME                                     READY   STATUS    RESTARTS   AGE
postgres-db-5488dcd68f-vtf9n             1/1     Running   0          37s
```

Pod ismini kopyala (örneğin `postgres-db-5488dcd68f-vtf9n`) ve aşağıdaki komutta doğrudan Pod'un içine K8s üzerinden bir SQL komutu gönderelim:

```bash
kubectl exec -it postgres-db-5488dcd68f-vtf9n -- psql -U ozgur -d testdb -c "CREATE TABLE k8s_test (id SERIAL, mesaj VARCHAR(100)); INSERT INTO k8s_test (mesaj) VALUES ('Ozgur K8s Veritabani'), ('Adana Merkezli SSD'); SELECT * FROM k8s_test;"
```
*(Bu komut önce tabloyu oluşturacak, içine iki satır veri ekleyecek ve ardından ekrana bu verileri yazdıracaktır).*

### Adım 2: Kaos Vakti (Pod'u Yok Et!)

Verilerimiz içeride. Diyelim ki gece yarısı sunucuda bir şeyler ters gitti, RAM doldu veya donanımsal bir takılma oldu ve PostgreSQL çöktü. Bunu simüle etmek için pod'u acımasızca silelim (Yine `postgres-db-5488dcd68f-vtf9n` kısmını kendi eski pod adınla değiştir):

```bash
kubectl delete pod postgres-db-5488dcd68f-vtf9n
```

Hemen ardından zaman kaybetmeden durum kontrolü yap:
```bash
ozgur@ozgurvm2:~/test/postgres$ kubectl get pods
NAME                                     READY   STATUS    RESTARTS   AGE
postgres-db-5488dcd68f-m2z9c             1/1     Running   0          5s
```

K8s'in eski pod'u "Terminating" durumuna alıp, sözleşmedeki (`replicas: 1`) kurala uymak için yepyeni isimli bir `postgres-db-5488dcd68f-m2z9c` pod'u ayağa kaldırdığını göreceksin. Yeni pod'un durumu `Running` olana kadar birkaç saniye bekle.


### Adım 3: Gerçeğin Anı (Veriler Duruyor Mu?)

Eski pod öldü, yeni pod tertemiz bir şekilde doğdu. Peki verilerimiz nerede? 

Yeni oluşan pod'un adını kopyala ve sadece `SELECT` sorgumuzu tekrar gönderelim:

```bash
kubectl exec -it postgres-db-5488dcd68f-m2z9c -- psql -U ozgur -d testdb -c "SELECT * FROM k8s_test;"
```

Ekranda o girdiğimiz **"Ozgur K8s Veritabani"** ve **"Adana Merkezli SSD"** kayıtlarını tekrar gördün mü?

---

### Sırada Ne Var?
Eğer verileri başarıyla gördüysen, K8s üzerinde Storage (Depolama) mantığını tam anlamıyla çözdün demektir! SSD'n harika bir şekilde kalıcı veri tutuyor. 

Bu adımı da başarıyla cebimize koyduğumuza göre, artık o karmaşık port numaralarından (`31005`, `31010` vb.) kurtulma vakti geldi. Sistemine **Traefik Ingress** yapılandırmasını kurarak, `.NET API` veya `React` projene portsuz, doğrudan isimle (örneğin `react.ozgur.lokal` veya `api.ozgur.lokal`) erişmeni sağlayacak "Kurumsal Ağ Yönlendirmesi" sürecine geçelim.
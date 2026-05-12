# PostgreSQL (PostGIS) Kurulumu (Master-Replica)

### Zalando vs. CloudNativePG (CNPG)

| Özellik | Zalando Postgres Operator | CloudNativePG (CNPG) |
| --- | --- | --- |
| **Mimari Yaşı** | Biraz eski (eski K8s standartlarına dayanır). | Yeni nesil, tamamen Kubernetes API'si ile entegre. |
| **Kaynak Tüketimi** | Spilo ve Patroni içerdiği için biraz ağır çalışır. | Sadece Go ile yazılmıştır, inanılmaz hafiftir. |
| **Read/Write Ayrımı** | Servisleri manuel veya Patroni etiketleriyle ayırmak gerekir. | Kurulduğu an sana otomatik olarak `-rw` (Read/Write) ve `-ro` (Read-Only) olmak üzere iki hazır servis sunar. |
| **Yedekleme (Backup)** | WAL-E / WAL-G gibi harici araçlara bağımlıdır. | S3 (Cloudflare R2 veya AWS) uyumlu yerleşik ve kusursuz bir yedekleme mimarisi vardır. |

**CloudNativePG (CNPG)** ile bu kurulumu yaptığımızda K3s sistemin sana otomatik olarak 2 farklı servis verecek:

* `production-postgres-rw`: Yazma ve okuma işlemleri için (Master).
* `production-postgres-ro`: Sadece okuma işlemleri için (Replica).

İşte o profesyonel ortama geçişin 3 adımı:

### 1. Adım: CloudNativePG Operatörünü Kuralım

Öncelikle bu modern veritabanı yöneticisini (Operatörü) sistemimize tanıtmamız gerekiyor. K3s terminalinde şu komutu çalıştırarak sunucuya (Server-Side) doğrudan kurabiliriz:

```bash
kubectl apply --server-side -f https://raw.githubusercontent.com/cloudnative-pg/cloudnative-pg/release-1.22/releases/cnpg-1.22.1.yaml
```

*(Kurulum birkaç saniye sürecektir. İşlem bitince `kubectl get pods -n cnpg-system` ile operatörün ayağa kalktığını teyit edebilirsin).*

### 2. Adım: Şifrelerimizi CNPG Formatına Getirelim (Kurye Ayarı)

CNPG, veritabanını ilk kurarken (`bootstrap`) kendi beklediği standart bir Secret formatı ister. Bu Secret'ın içinde mutlaka `username` ve `password` anahtarları olmalıdır.

Vault'tan şifreyi çeken o meşhur ESO Kuryemizi CNPG'nin anlayacağı şekilde güncelleyelim. Aşağıdaki YAML'ı uygulayarak K3s içinde `cnpg-auth-secret` adında yeni bir paket oluşturalım: [cnpg-auth-secret.yml](cnpg-auth-secret.yml)

`[cnpg-auth-secret.yml](cnpg-auth-secret.yml)` içeriğini K3s üzerinde `kubectl apply -f cnpg-auth-secret.yml` komutuyla ateşle.

### 3. Adım: Master-Replica Cluster'ı Ateşleyelim!

İşte Docker Compose dosyanın tamamen K8s ve CNPG mimarisine dökülmüş, 1 Master ve 1 Replica ile çalışan "Production" hali.

`[cnpg-cluster.yaml](cnpg-cluster.yaml)` içeriğini K3s üzerinde `kubectl apply -f cnpg-cluster.yaml` komutuyla ateşle:

Bu YAML'ı uyguladığında Lens Desktop'ta `production-postgres-1` (Master) ve `production-postgres-2` (Replica) podlarının harika bir senkronizasyonla ayağa kalktığını izleyebilirsin. 

**İndirme ve kurulum işlemini canlı izle**
```bash
kubectl get pods -w -n default
```

#### Hata Durumunda Log Kontrolü

```bash
kubectl logs -n default production-postgres-1-initdb-hcb9p
```

```bash
kubectl describe pod production-postgres-1-initdb-hcb9p -n default
```

### 4. Adım: Sonuç Çıktı
**İşlemler**
```bash
ozgur@ozgurvm2:~/test/posgresql$ kubectl apply -f cnpg-auth-secret.yml
externalsecret.external-secrets.io/cnpg-database-secret-rule created

ozgur@ozgurvm2:~/test/posgresql$ kubectl apply -f cnpg-cluster.yaml
cluster.postgresql.cnpg.io/production-postgres created

ozgur@ozgurvm2:~/test/posgresql$ kubectl get pods -w -n default
NAME                                                         READY   STATUS            RESTARTS        AGE
beyla-k8s-cache-74f655bc88-x82pd                             1/1     Running           5 (3h4m ago)    3d
grafana-k8s-monitoring-alloy-logs-zggrf                      2/2     Running           10 (3h4m ago)   3d
grafana-k8s-monitoring-alloy-metrics-0                       2/2     Running           10 (3h4m ago)   3d
grafana-k8s-monitoring-alloy-operator-8455b95bb8-twpbp       1/1     Running           5 (3h4m ago)    3d
grafana-k8s-monitoring-alloy-profiles-8q99v                  2/2     Running           10 (3h4m ago)   3d
grafana-k8s-monitoring-alloy-receiver-7b9986455b-j75wn       2/2     Running           10 (3h4m ago)   3d
grafana-k8s-monitoring-alloy-singleton-7c54bdbc8c-4cvjw      2/2     Running           10 (3h4m ago)   3d
grafana-k8s-monitoring-beyla-t5vbp                           1/1     Running           5 (3h ago)      3d
grafana-k8s-monitoring-kepler-cpg9z                          1/1     Running           5 (3h4m ago)    3d
grafana-k8s-monitoring-kube-state-metrics-64c57bdd6d-klkh2   1/1     Running           5 (3h4m ago)    3d
grafana-k8s-monitoring-node-exporter-rv5ft                   1/1     Running           5 (3h4m ago)    3d
grafana-k8s-monitoring-opencost-56fc754567-6jsdg             1/1     Running           18 (3h ago)     3d
production-postgres-1-initdb-dfw95                           0/1     PodInitializing   0               41s
production-postgres-1-initdb-dfw95                           1/1     Running           0               49s
production-postgres-1-initdb-dfw95                           0/1     Completed         0               54s
production-postgres-1-initdb-dfw95                           0/1     Completed         0               55s
production-postgres-1-initdb-dfw95                           0/1     Completed         0               56s
production-postgres-1                                        0/1     Pending           0               0s
production-postgres-1                                        0/1     Pending           0               0s
production-postgres-1                                        0/1     Init:0/1          0               0s
production-postgres-1                                        0/1     PodInitializing   0               1s
production-postgres-1                                        0/1     Running           0               2s
production-postgres-1                                        0/1     Running           0               3s
production-postgres-1                                        0/1     Running           0               10s
production-postgres-1                                        1/1     Running           0               11s
production-postgres-2-join-5mw8f                             0/1     Pending           0               0s
production-postgres-2-join-5mw8f                             0/1     Pending           0               0s
production-postgres-2-join-5mw8f                             0/1     Pending           0               4s
production-postgres-2-join-5mw8f                             0/1     Init:0/1          0               4s
production-postgres-2-join-5mw8f                             0/1     PodInitializing   0               5s
production-postgres-2-join-5mw8f                             1/1     Running           0               6s
production-postgres-2-join-5mw8f                             0/1     Completed         0               10s
production-postgres-2-join-5mw8f                             0/1     Completed         0               11s
production-postgres-2-join-5mw8f                             0/1     Completed         0               12s
production-postgres-2                                        0/1     Pending           0               0s
production-postgres-2                                        0/1     Pending           0               0s
production-postgres-2                                        0/1     Init:0/1          0               0s
production-postgres-2                                        0/1     Init:0/1          0               0s
production-postgres-2                                        0/1     PodInitializing   0               1s
production-postgres-2                                        0/1     Running           0               2s
production-postgres-2                                        0/1     Running           0               3s
production-postgres-2                                        0/1     Running           0               10s
production-postgres-2                                        1/1     Running           0               10s
production-postgres-1-initdb-dfw95                           0/1     Completed         0               94s
production-postgres-1-initdb-dfw95                           0/1     Completed         0               94s
production-postgres-2-join-5mw8f                             0/1     Completed         0               24s
production-postgres-2-join-5mw8f                             0/1     Completed         0               24s

^Cozgur@ozgurvm2:~/test/posgresql$ ^C
ozgur@ozgurvm2:~/test/posgresql$ kubectl get pods -A
NAMESPACE          NAME                                                         READY   STATUS      RESTARTS        AGE
cert-manager       cert-manager-5957746d66-flqkm                                1/1     Running     4 (3h7m ago)    23h
cert-manager       cert-manager-cainjector-567c6b47ff-scm4d                     1/1     Running     4 (3h7m ago)    23h
cert-manager       cert-manager-webhook-7cc5c588cb-2dk6s                        1/1     Running     4 (3h7m ago)    23h
cnpg-system        cnpg-controller-manager-6787dfd466-ljj54                     1/1     Running     0               142m
default            beyla-k8s-cache-74f655bc88-x82pd                             1/1     Running     5 (3h7m ago)    3d
default            grafana-k8s-monitoring-alloy-logs-zggrf                      2/2     Running     10 (3h7m ago)   3d
default            grafana-k8s-monitoring-alloy-metrics-0                       2/2     Running     10 (3h7m ago)   3d
default            grafana-k8s-monitoring-alloy-operator-8455b95bb8-twpbp       1/1     Running     5 (3h7m ago)    3d
default            grafana-k8s-monitoring-alloy-profiles-8q99v                  2/2     Running     10 (3h7m ago)   3d
default            grafana-k8s-monitoring-alloy-receiver-7b9986455b-j75wn       2/2     Running     10 (3h7m ago)   3d
default            grafana-k8s-monitoring-alloy-singleton-7c54bdbc8c-4cvjw      2/2     Running     10 (3h7m ago)   3d
default            grafana-k8s-monitoring-beyla-t5vbp                           1/1     Running     5 (3h3m ago)    3d
default            grafana-k8s-monitoring-kepler-cpg9z                          1/1     Running     5 (3h7m ago)    3d
default            grafana-k8s-monitoring-kube-state-metrics-64c57bdd6d-klkh2   1/1     Running     5 (3h7m ago)    3d
default            grafana-k8s-monitoring-node-exporter-rv5ft                   1/1     Running     5 (3h7m ago)    3d
default            grafana-k8s-monitoring-opencost-56fc754567-6jsdg             1/1     Running     18 (3h2m ago)   3d
default            production-postgres-1                                        1/1     Running     0               2m18s
default            production-postgres-2                                        1/1     Running     0               113s
external-secrets   external-secrets-5c77f8c67b-xwcwd                            1/1     Running     4 (3h7m ago)    21h
external-secrets   external-secrets-cert-controller-75976d9c6b-r5gdr            1/1     Running     4 (3h7m ago)    22h
external-secrets   external-secrets-webhook-64b5b9c8d4-mhd24                    1/1     Running     4 (3h7m ago)    22h
kube-system        coredns-c4dbffb5f-v6fsd                                      1/1     Running     8 (3h7m ago)    6d2h
kube-system        helm-install-traefik-crd-r2742                               0/1     Completed   0               6d2h
kube-system        helm-install-traefik-qfcjd                                   0/1     Completed   1               6d2h
kube-system        local-path-provisioner-5c4dc5d66d-9s8s7                      1/1     Running     8 (3h7m ago)    6d2h
kube-system        metrics-server-786d997795-68f8z                              1/1     Running     8 (3h7m ago)    6d2h
kube-system        svclb-traefik-eada35a1-qhgnc                                 2/2     Running     16 (3h7m ago)   6d2h
kube-system        traefik-9bcdbbd9-vqlwq                                       1/1     Running     8 (3h7m ago)    6d2h
vault              vault-0                                                      1/1     Running     1 (3h3m ago)    3h34m
vault              vault-agent-injector-8c76487db-xgm7w                         1/1     Running     1 (3h7m ago)    3h34m

ozgur@ozgurvm2:~/test/posgresql$ kubectl get svc -n default | grep postgres
production-postgres-r                          ClusterIP   10.43.116.41    <none>        5432/TCP                               3m54s
production-postgres-ro                         ClusterIP   10.43.187.7     <none>        5432/TCP                               3m54s
production-postgres-rw                         ClusterIP   10.43.198.67    <none>        5432/TCP                               3m54s
```

1. Önce `production-postgres-1-initdb` çalıştı; Vault'tan şifreyi aldı, `Test_Db_2026` veritabanını kurdu, PostGIS eklentilerini (extension) içeri bastı ve görevini tamamladı (`Completed`).
2. Hemen ardından asıl Master podumuz olan `production-postgres-1` ayağa kalktı.
3. Asıl büyü burada başlıyor: CNPG operatörü hemen `production-postgres-2-join` adında geçici bir pod başlattı. Bu pod, Master'a bağlanıp verilerin bir kopyasını alarak Replica'yı hazırladı (`Completed`).
4. Ve finalde `production-postgres-2` (Replica) podu `1/1 Running` olarak sahneye çıktı.

Tebrikler! Şu an tek bir YAML dosyasıyla, kendi kendini onarabilen, PostGIS eklentileri hazır, Master-Replica mimarisinde çalışan kurumsal seviyede bir veritabanı kümesini başarıyla ayağa kaldırdın.

### Peki Şimdi Ne Oldu? (Bağlantı Noktaları)

CloudNativePG, bu podları ayağa kaldırırken arka planda Kubernetes içine senin için çok akıllı 3 farklı Servis (Service) oluşturdu. Bunları görmek için terminale şu komutu yazabilirsin:

```bash
kubectl get svc -n default | grep postgres
```

Karşına çıkacak olan servislerin görevi şudur:

* **`production-postgres-rw` (Read/Write):** Bu senin asıl bağlantı noktan. .NET 10 ile geliştirdiğin Spatial Engine projesinin `ConnectionString` ayarına bu servisi yazacaksın. Buraya gelen tüm istekler doğrudan **Master** poda gider (Yazma işlemleri için).
* **`production-postgres-ro` (Read-Only):** Sadece okuma (SELECT) yapacak raporlama araçları veya API'nin sadece okuma yapan kısımları için kullanılır. Yükü doğrudan **Replica** poda dağıtır.
* **`production-postgres-r` (Read):** Hem okuma hem yazma podlarına karışık istek atar (Genelde çok kullanılmaz, `rw` ve `ro` ayrımı en sağlıklısıdır).

Bir sonraki mimari adımımız için bu veritabanına nasıl erişmek istersin; dışarıdan DBeaver/pgAdmin gibi bir araçla bağlanmak için Traefik `IngressRouteTCP` kuralını mı yazalım, yoksa podun içine girip tabloları mı kontrol edelim?




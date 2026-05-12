# IngressRouteTCP Ayarlaması

Traefik üzerinde PostgreSQL gibi HTTP tabanlı olmayan protokolleri dışarıya açmak için `IngressRoute` yerine **`IngressRouteTCP`** kullanıyoruz. Bu, trafiğin uygulama katmanına (L7) çıkmadan taşıma katmanında (L4) doğrudan veritabanına yönlendirilmesini sağlar.

CloudNativePG (CNPG) mimarimizde iki farklı servisimiz olduğu için, dış dünyadan hem yazma (Master) hem de okuma (Replica) uç noktalarına erişmek için iki ayrı kural tanımlayacağız.

### 1. Adım: Traefik Entrypoint Kontrolü

Traefik'in 5432 portundan gelen istekleri kabul edebilmesi için K3s üzerindeki Traefik konfigürasyonunda bir "entrypoint" tanımlı olmalıdır. Eğer K3s varsayılan ayarlarıyla kuruluysa, 5432 portunu Traefik servisine eklememiz gerekebilir.

Genellikle ev laboratuvarı kurulumlarında Traefik'e ek port açmak yerine, doğrudan `LoadBalancer` servisi üzerinden port yönlendirmesi yapılır. Ancak biz standart Traefik TCP yönlendirme mantığıyla ilerleyelim.

### 2. Adım: IngressRouteTCP Kurallarını Yazalım

YAML dosyasını `[postgres-ingress.yaml](postgres-ingress.yaml)` olarak kaydedebilirsin. Burada iki farklı strateji uyguluyoruz:

1. **Master (RW):** Yazma ve okuma işlemleri için 5432 portunu kullanır.
2. **Replica (RO):** Sadece okuma işlemleri için (örneğin) 5433 portunu veya farklı bir kuralı kullanabilir.

### TCP Yönlendirme Nasıl Çalışır?

HTTP yönlendirmesinde Traefik gelen "Host" başlığına bakarak hangi siteye gideceğini anlar. Ancak veritabanı bağlantılarında (TCP) paketlerin içinde bu bilgi her zaman açıkça bulunmaz. Bu yüzden ya her servis için ayrı bir giriş kapısı (Port/Entrypoint) kullanırız ya da **SNI (Server Name Indication)** kullanarak TLS üzerinden yönlendirme yaparız.


Bu kuralları uyguladıktan sonra, dışarıdan (örneğin bilgisayarındaki bir SQL aracından) sunucunun IP'sine 5432 portundan bağlandığında doğrudan **Master** veritabanına ulaşırsın. Eğer her şey tamamsa, veritabanına ilk tablonu oluşturmak için `test-sql-db.sql` dosyasını içeri basmaya hazır mısın?
## Traefik: Ingress Büyüsü

Artık NodePort'ların (31005, 31010 vb.) o çirkin dünyasından çıkıp kurumsal ağ yapısına geçiyoruz.

K8s dünyasında Traefik'e "Trafiği şu adrese yönlendir" demek için **Ingress** adında bir obje (kural kitabı) kullanırız.

**1. Ingress Kuralımızı Yazalım**
Daha önce oluşturduğumuz .NET API ve React projelerini tek bir Ingress üzerinden iki farklı alan adına (domain) bağlayacağız.

Yeni bir dosya oluştur:

```bash
nano rule-ingress.yaml

```

İçine şu kodu yapıştır (Dikkat: Buradaki `backend` kısımlarında, önceki YAML'larda verdiğimiz Servis isimlerini ve o servislerin iç portlarını kullanıyoruz):

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: ana-yonlendirici
  annotations:
    # Bu kuralı Traefik'in işlemesini söylüyoruz
    kubernetes.io/ingress.class: traefik
spec:
  rules:
  # 1. Kural: .NET API için
  - host: api.ozgur.lokal
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: dotnet-api-servisi
            port:
              number: 80
  # 2. Kural: React Uygulaması için
  - host: app.ozgur.lokal
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: react-servisi
            port:
              number: 80

```

Kaydet ve uygula:

```bash
kubectl apply -f rule-ingress.yaml

```

**2. Yerel Ağını Kandır (Hosts Dosyası)**
Bu alan adları (`api.ozgur.lokal` ve `app.ozgur.lokal`) gerçek dünyada yok. Bu yüzden ana bilgisayarındaki (MacBook veya Windows) işletim sistemine "Bu isimleri gördüğünde internete gitme, doğrudan benim Ubuntu sunucuma (192.168.1.121) git" dememiz lazım.

* **Windows kullanıyorsan:** Not Defteri'ni Yönetici Olarak Çalıştır, `C:\Windows\System32\drivers\etc\hosts` dosyasını aç.
* **Mac kullanıyorsan:** Terminali aç, `sudo nano /etc/hosts` yaz.

Dosyanın en altına şu iki satırı ekle ve kaydet:

```text
192.168.1.121  api.ozgur.lokal
192.168.1.121  app.ozgur.lokal

```

**3. Gerçeğin Anı (Test Vakti)**
Artık hiçbir port yazmana gerek yok! Traefik 80 portunda pür dikkat bekliyor.

Bilgisayarının tarayıcısını aç ve şuraya git:
**`[http://api.ozgur.lokal/swagger/index.html](http://api.ozgur.lokal/swagger/index.html)`**

Ardından yeni bir sekme aç ve şuraya git:
**`[http://app.ozgur.lokal](http://app.ozgur.lokal)`**

Her iki projen de o çirkin port numaraları olmadan, kendi özel domain isimleriyle bir profesyonel gibi açıldı mı?



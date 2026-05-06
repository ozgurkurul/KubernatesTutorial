# Kubernates

## 1. K8s veya daha küçüğü K3s

### Kısım 1: Neden Kubernetes'e İhtiyacımız Var?
Docker çok güzeldir; uygulamamızı alır, paketler ve her yerde çalışmasını sağlar. `docker run` deriz ve ayağa kalkar. 

*Ama ya uygulaman çok popüler olursa ve 1 sunucu yetmezse?*
*Ya sunucunun fişi çekilirse ve o container ölürse?*
*Yeni bir versiyon çıkacağında, sistemi kapatmadan (zero-downtime) yenisini nasıl devreye alırsın?*

İşte Kubernetes (Kısaca K8s) burada devreye girer. K8s bir **Orkestrasyon Aracıdır**. Senin yerine şu sorulara cevap verir:
1.  **Self-Healing (Kendi Kendini İyileştirme):** "Bu container çöktü, hemen yenisini başlatmalıyım."
2.  **Scaling (Ölçekleme):** "Trafik arttı, bu uygulamadan 3 tane daha kopyalayıp yükü dağıtmalıyım."
3.  **Service Discovery:** "A servisi B servisiyle konuşmak istiyor, IP'ler sürekli değişse bile onları birbirleriyle konuşturmalıyım."

---

### Kısım 2: K8s'in Kutsal Üçlüsü (Temel Kavramlar)
K8s dünyasında her şeye **"Object" (Nesne)** denir. Öğrenmemiz gereken ilk 3 temel nesne şunlardır:

#### 1. Pod (Docker Container'ın Kılıfı)
K8s, Docker container'ları ile doğrudan muhatap olmaz. Container'ları alır, **Pod** adını verdiği bir zarfın (veya kapsülün) içine koyar. 
*   K8s'teki en küçük birim Pod'dur. 
*   Bir Pod içinde genellikle 1 tane Docker container çalışır (Örn: senin Nginx uygulaman).
*   **Önemli Kural:** Pod'lar ölümlüdür. Çökerlerse yenileri doğar, dolayısıyla IP adresleri sürekli değişir. Asla bir Pod'un IP'sine güvenilmez!

#### 2. Deployment (Yönetici)
Eğer Pod'lar işçiyse, Deployment onların yöneticisidir. Sen K8s'e "Bana 1 tane Pod ver" demezsin; "Bana bir Deployment oluştur, içinde her zaman Nginx çalışan 3 tane Pod olsun" dersin.
*   Eğer Pod'lardan biri çökerse, Deployment durumu fark eder ve sayıyı tekrar 3'e tamamlamak için anında yeni bir Pod yaratır.
*   Biraz önce terminalde yazdığın `kubectl create deployment ilk-uygulamam --image=nginx` komutu tam olarak buydu. Bir yönetici atadın ve o yönetici de Nginx container'ı barındıran 1 tane Pod yarattı.

#### 3. Service (Santral / Trafik Polisi)
Pod'ların IP adreslerinin sürekli değiştiğini söylemiştik. Peki dışarıdan gelen kullanıcı veya diğer uygulamalar, bu Nginx'i nasıl bulacak? 
*   İşte **Service** burada devreye girer. Service'ler kalıcı, statik bir IP adresine veya isme sahiptir.
*   Senin isteklerin Service'e gelir; Service arka planda hangi Pod'ların hayatta olduğunu bilir ve trafiği onlara dağıtır (Load Balancing).
*   Yine biraz önce terminalde yazdığın `kubectl expose deployment ilk-uygulamam --type=NodePort --port=80` komutu ile Nginx için bir Service yaratmış oldun.
* Erişilecek Portu Öğrenme komutu: `kubectl get svc ilk-uygulamam`

---

### Önümüzdeki Yol Haritası (DevOps Süreçlerine Giden Yol)
Senin hedeflerine ulaşmak için şu adımları izleyeceğiz:
1.  **Adım:** Terminal komutlarını (Imperative) bırakıp, her şeyi kod (YAML) ile yazmayı (Declarative) öğrenmek. *(K8s'in asıl gücü buradadır).*
2.  **Adım:** Uygulamaları otomatik ölçeklemek (Auto-Scaling) ve kesintisiz güncellemek (Rolling Updates).
3.  **Adım:** SSD diskimizi kullanarak kalıcı veri depolamak (Persistent Volumes & PostgreSQL kurulumu).
4.  **Adım:** Nginx NodePort yerine, profesyonel bir ağ geçidi (Ingress/Traefik) ile domain üzerinden yönlendirme yapmak.
5.  **Adım:** Tüm bu ekosistemin loglarını ve işlemci/RAM analizlerini görmek için izleme araçları kurmak.

### İlk Görevin / Pratiğin
Şu ana kadar anlattığım "Kutsal Üçlü" (Pod, Deployment, Service) mantığı, kafandaki Docker mimarisiyle örtüştü mü? Eğer bu mantık oturduysa, bir sonraki mesajımızda o Nginx uygulamasını terminalden silip, **gerçek bir DevOps mühendisi gibi bir YAML dosyası yazarak** ayağa kaldırmaya başlayabiliriz. Hazır mısın?

## Konular
0. [Kubernates Kurulumu](./0-KubernatesSetup.md)
    1. [Docker Kurulumu](./0.1-DockerInstall.md.md)
1. [Kubernates Declarative (Bildirimsel) altyapı yönetimi](1-Declarative.md)
2. [ConfigMap Oluşturma](./2-ConfigMap.md)
3. Örnek Proje Yüklemeleri
    1. [Statik HTML Sayfası ve Nginx](./3.1-InstallStaticWeb.md)
    2. [React Projesi](./3.2-ReactProject.md)
    3. [Net Web Api Projesi](./3.3-NetWebApi.md)

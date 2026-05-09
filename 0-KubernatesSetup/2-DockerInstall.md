## Dockera Giriş
 Biz biraz önce **K3s** kurduğumuzda, o aslında arka planda RAM tasarrufu yapmak için Docker yerine **`containerd`** adında daha hafif bir motor kurdu ve kullanıyor. Ancak `containerd` sadece imajları *çalıştırmak* için harikadır; imajları *inşa etmek (build)* için bizim kesinlikle emektar **Docker**'a ihtiyacımız var.

Hemen `ozgurvm2` sunucunu donatalım.

### Adım 1: Docker Kurulumu
Ubuntu'nun resmi depolarındaki kararlı sürümü kurmak en hızlısıdır. Terminaline şu komutu yapıştır:

```bash
sudo apt update && sudo apt install docker.io -y
```

### Adım 2: Yetki Ayarları (Çok Önemli)
Kurulum bittikten sonra Docker, güvenlik gereği her komutta senden `sudo` yazmanı isteyecektir. Kendi kullanıcını (`ozgur`) Docker yetkili grubuna ekleyerek bu dertten kurtulalım:

```bash
sudo usermod -aG docker $USER
```

Bu yetkinin anında aktif olması için sunucudan çıkıp girmene gerek yok, şu komutu çalıştırman yeterli:
```bash
newgrp docker
```

### Adım 3: Test
Artık Docker hazır. Çalıştığını teyit etmek için `docker ps` yazabilirsin (hata vermiyorsa yetkiler tamamdır).

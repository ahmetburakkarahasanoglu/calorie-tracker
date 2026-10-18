# Yerel kurulum

## PostgreSQL ve gizli ayarlar

API `src/KaloriTakip.Api/.env` dosyasını yükler. Bu dosya Git tarafından yok sayılır ve yalnızca dosya sahibi okuyabilir olmalıdır. `.env.example` örneğini kullanarak yerel dosyanız yoksa oluşturun:

```bash
cp src/KaloriTakip.Api/.env.example src/KaloriTakip.Api/.env
nano src/KaloriTakip.Api/.env
```

`ConnectionStrings__KaloriTakip` satırındaki `Password` değerini yerel PostgreSQL parolanızla değiştirin. `.env` dosyasını, yedeğini veya parolayı kaynak kontrolüne eklemeyin.

## Çalıştırma

Veritabanını bir kez oluşturun (mevcutsa bu adımı atlayın), sonra API'yi başlatın:

```bash
sudo -u postgres createdb kaloritakip
dotnet run --project src/KaloriTakip.Api/KaloriTakip.Api.csproj --launch-profile http
```

Başlangıçta EF Core migration'ları `users` hesabı sütunlarını ve diğer tabloları uygular. Tarayıcıda [http://localhost:5070](http://localhost:5070) sayfasını açıp hesap oluşturun veya giriş yapın.

## DBeaver'da uygulama veritabanını açma

Uygulama `postgres` adlı yönetim veritabanına değil, bağlantı dizesindeki `kaloritakip` veritabanına kayıt yapar. DBeaver'da mevcut PostgreSQL bağlantısına sağ tıklayıp **Edit Connection** ekranında **Database** alanını `kaloritakip` olarak ayarlayın; host `localhost`, port `5432`, kullanıcı `postgres` olmalıdır. Bağlantıyı yenileyip **Databases → kaloritakip → Schemas → public → Tables** yolunu açın. API en az bir kez başarıyla başlatıldıktan sonra `users`, `calorie_limit_entries` ve `food_log_entries` tabloları görünür. DBeaver'da `postgres` veritabanının `public` şemasına bakmak uygulama verilerini göstermez.

## Hesap güvenliği

- Parola veritabanına düz metin olarak yazılmaz; ASP.NET Core Identity'nin salt içeren PBKDF2 parola hash'i kullanılır.
- Kullanıcı adı veritabanında normalize edilmiş düz metin olarak saklanır; veritabanı yedeklerine erişimi sınırlandırın.
- Oturum, HttpOnly ve SameSite Strict çerezle tutulur; giriş/kayıt denemeleri IP başına sınırlandırılmıştır.
- Hesaplama ve geçmiş endpoint'leri oturum ister ve kullanıcı ID'sini yalnızca imzalı oturum claim'inden alır.
- Bu yerel prototip e-posta doğrulama, parola sıfırlama veya genel internete açılma için gerekli ek korumaları henüz içermez.

Kullanıcı adı saklama biçimi hash'ten düz metne geçirildi. Eski hash'lerden özgün kullanıcı adları geri elde edilemediğinden migration eski kullanıcı adı hash'lerini kaldırır; mevcut hesaplar yeniden kaydedilmelidir. Eski kullanıcılara bağlı günlük kayıtları veritabanında kalır ancak yeni hesaplara otomatik bağlanmaz.

## Fotoğrafla yiyecek kaydetme

Model servisini kurup başlatmak için [`model-service/README.md`](../../model-service/README.md) dosyasına bakın. Model ağırlıkları kaynak koduna eklenmez; `FOOD_MODEL_PATH` mevcut `turk_yemek_modeli_102` klasörünü göstermelidir. API `.env` ayarlarında `FoodRecognition__NutritionCsvPath` değerini `kalori_tablosu.csv` dosyasının konumuna göre düzenleyin. Varsayılan yol, `Dıyet_Model` klasörünün bu repository ile aynı üst klasörde olduğunu varsayar.

Önce model servisini başlatın, sonra PostgreSQL ve API'yi çalıştırın. Servis hazır olduğunda `http://127.0.0.1:8001/health` adresi `ready` döndürür. Giriş yaptıktan sonra JPG, PNG veya WebP fotoğraf seçin. Yerel ViT modeli yiyeceği sınıflandırır; sınıfın `kalori_tablosu.csv` kaydı ve 100 gram başına kalorisi gösterilir. Gram miktarını onayladığınızda kalori `kcal/100 g × gram/100` formülüyle sunucuda hesaplanıp günlük kayda eklenir. Fotoğraf diske veya veritabanına kaydedilmez. Model servisi durursa API fotoğraf analizinde hata döndürür.

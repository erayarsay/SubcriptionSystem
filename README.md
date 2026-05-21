# Advanced Subscription & Invoice Management System (SaaS)

Bu proje, **Kodpit Teknoloji A.Ş** bünyesinde gerçekleştirilen bilgisayar programcılığı stajı kapsamında geliştirilmiş; modern, katmanlı mimariye (Layered/Onion Architecture) sahip kurumsal bir **Abonelik ve Faturalandırma Yönetim Sistemi** simülasyonudur. Projenin arayüzünde kullanıcı deneyimini artırmak adına fitüristik ve estetik çizgiler barındıran modern bir tasarım dili tercih edilmiştir.

---

## Mimari Yapı (Architecture)

Proje, iş kurallarının (Business Logic) arayüzden ve veritabanından tamamen bağımsız olmasını sağlayan **Katmanlı Mimari (Layered Architecture)** prensiplerine uygun olarak Clena Code standartlarında geliştirilmiştir:

* **Domain:** Çekirdek varlıkların (Entities) ve veri yapılarının (AppUser, Subscription, Plan, Transaction, Notification) yer aldığı katman.
* **Application / Services:** Finansal hesaplamaların, abonelik iş kurallarının ve iş mantığının döndüğü ana motor katmanı.
* **Insfrastructure:** Iyzico ödeme geçidi API entegrasyonu ve harici servislerin yönetildiği katman.
* **Web (Presentation):** ASP .Net Core MVC mimarisinin, Razor Wiev yapılarının ve kullanıcı arayüzünün (UI) yer aldığı sunum katmanı.

---

## Öne Çkan Fonksiyonel Özellikler

Proje, sadece basit bir CRUD uygulaması olmayıp, gerçek hayat senaryolarını barındıran gelişmiş finansal ve operasyonel algoritmalara sahiptir:

### 1. Güvenli Iyzico Entegrasyonu & 3DS Filtreleme
Kullanıcılar kredi kartı bilgileriyle cüzdanlarına bakiye yükleyebilirler. Iyzico Sandbox API'den dönen callback'ler sıkı bir **Güvenlik Duvarından** geçirilir. Sadece istek durumuna değil, bankanın paraı çektiğini doğrulayan `PaymentStatus == "SUCCESS"` alanına bakılır; böylece iptal edilen veya başarısız olan işlemlerle sahte bakiye yükleme açıkları kesin olarak engellenir.

### 2. Abonelik Dondurma ve Geri Açma (Freeze & Unfreeze)
Kullanıcı aktif aboneliğini dondurduğunda, sistem o anki bitiş tarihinden kalan gün hakkını hesaplar ve dondurulan gün sayısını veritabanına saklayarak paketi pasife çeker. Paket geri açıldığında, `DateTime.UtcNow` üzerine o saklanan haklar eklenerek yeni bir biiş tarihi hesaplanır ve kullanıcı hak kaybı yaşamaz.

### 3. Abonelik Süresi Uzatma (Extension)
Kullanıcılar aktif paketi varken tekrar aynı paketi satın alması durumunda mevcut paketi yanmaz. Sistem otomatik olarak yeni paket süresini mevcut paket bitiş tarihinin (`EndDate`) üzerine zincirleme olarak ekler.

### 4. Kalan Gün İade Sistemi (Prorated Refund)
Kullanıcı aboneliğini dönem bitmeden iptal etmek isterse, kalan gün hakkı kuruşu kuruşuna finansal algoritmalarla hesaplanır:
$$\text{Günlük Fiyat} = \frac{\text{Paket Tutarı}}{\text{Toplam Gün}}$$
$$\text{İade Tutarı} = \text{Günlük Fiyat} \times \text{Kalan Gün}$$
Hesaplanan tutar `Math.Round` ile yuvarlanarak kullanıcının cüzdan bakiyesine iade edilir ve veri tutarlılığı için `Transactions` tablosuna "İade" olarak işlenir. 

### 5. Tek Formda Güvenli Profil ve Fotoğraf Yönetimi
Kullanıcı ad, e-posta ve fotoğrafını tek bir buton tetiklemesiyle günceller. Fotoğraf seçildiğinde JavaScript `FileReader` API ile anlık önizleme (prewiev) yapılır ancak kullanıcı "Bilgileri Güncelle" butonuna basmadan sayfada refresh veya sunucuya POST işlemi gerçekleşmez. Dosya yolları `.Net Core` `IWebHosEnvionment` ile `wwwwroot/img/profile` kök dizinine benzersiz `Guid` isimleriyle kaydedilir.

---

## Teknoloji Yığını (Tech Stack)

* **Backend:** .Net Core 9, Entitiy Framework Core (Code-First)
* **Veritabanı:** Postgresql
* **Güvenlik & Oturum:** Microsoft ASP .Net Core Identity, Bcrypt Hashing
* **Ödeme Geçidi:** Iyzico Sandbox API integration
* **Frontend:** HTML5, CSS3, JavaScript, AOS (Animate on Scroll)

---

## Kurulum ve Çalıştırma

Projeyi yerel bilgisayarınızda ayağa kaldırmak için aşşağıdaki adımları takip edebilrisiniz: 

1. Projeyi bilgisayarınıza klonlayın:
   ```bash
   git clone <github-repo-linkiniz>

2. Proje ana dizinine giren terminali açın:
    dotnet restor

3. Veritabanı migration'larını uygulayın (Postgresql veritabanı otomatik oluşacaktır):
    dotnet ef database update

4. Projeyi canlı izleme modunda çalıştırın:
    dotnet watch run --project SubscriptionSystem.WebUI

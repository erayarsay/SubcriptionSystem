# 🚀 Advanced Subscription & Invoice Management System (SaaS)

Bu proje, **Kodpit Teknoloji A.Ş.** bünyesinde gerçekleştirilen bilgisayar programcılığı stajı kapsamında geliştirilmiş; modern, katmanlı mimariye (Layered/Onion Architecture) sahip kurumsal bir **Abonelik ve Faturalandırma Yönetim Sistemi** simülasyonudur. Projenin arayüzünde kullanıcı deneyimini artırmak adına fütüristik, estetik ve koyu/neon çizgiler barındıran modern bir tasarım dili tercih edilmiştir.

---

## 🏗️ Mimari Yapı (Architecture)

Proje, iş kurallarının (Business Logic) arayüzden ve veri tabanından tamamen bağımsız olmasını sağlayan **Katmanlı Mimari (Layered Architecture)** prensiplerine uygun olarak Clean Code standartlarında geliştirilmiştir:

* **Domain:** Çekirdek varlıkların (Entities) ve veri yapılarının (AppUser, Subscription, Plan, Transaction, Notification) yer aldığı katman.
* **Application / Services:** Finansal hesaplamaların, abonelik iş kurallarının ve iş mantığının döndüğü ana motor katmanı.
* **Infrastructure:** iyzico ödeme geçidi API entegrasyonu ve harici servislerin yönetildiği katman.
* **Web (Presentation):** ASP.NET Core MVC mimarisinin, Razor View yapılarının ve kullanıcı arayüzünün (UI) yer aldığı sunum katmanı.

---

## 🛠️ Öne Çıkan Fonksiyonel Özellikler

Proje, sadece basit bir CRUD uygulaması olmayıp, gerçek hayat senaryolarını barındıran gelişmiş finansal ve operasyonel algoritmalara sahiptir:

### 1. 💳 Güvenli iyzico Entegrasyonu & 3DS Filtreleme
Kullanıcılar kredi kartı bilgileriyle cüzdanlarına bakiye yükleyebilirler. iyzico Sandbox API'den dönen callback'ler sıkı bir **Güvenlik Duvarından** geçirilir. Sadece istek durumuna değil, bankanın parayı çektiğini doğrulayan `PaymentStatus == "SUCCESS"` alanına bakılır; böylece iptal edilen veya başarısız olan işlemlerle sahte bakiye yükleme açıkları kesin olarak engellenir.

### ❄️ 2. Abonelik Dondurma & Geri Açma (Freeze / Unfreeze)
Kullanıcı aktif paketini dondurduğunda, sistem o anki bitiş tarihinden kalan gün hakkını hesaplar ve dondurulan gün sayısını veritabanına saklayarak paketi pasife çeker. Paket geri açıldığında, `DateTime.UtcNow` üzerine o saklanan haklar eklenerek yepyeni bir bitiş tarihi hesaplanır ve kullanıcı hak kaybı yaşamaz.

### ⏳ 3. Abonelik Süresi Uzatma (Extension)
Kullanıcının aktif bir paketi varken tekrar paket satın alması durumunda mevcut paketi yanmaz. Sistem otomatik olarak yeni paket süresini mevcut paketin bitiş tarihinin (`EndDate`) üzerine zincirleme olarak ekler.

### 🔄 4. Kalan Gün İadesi Sistemi (Prorated Refund)
Kullanıcı aboneliğini dönem bitmeden iptal etmek isterse, kalan gün hakkı kuruşu kuruşuna finansal algoritmalarla hesaplanır:
$$\text{Günlük Fiyat} = \frac{\text{Paket Tutarı}}{\text{Toplam Gün}}$$
$$\text{İade Tutarı} = \text{Günlük Fiyat} \times \text{Kalan Gün}$$
Hesaplanan tutar `Math.Round` ile yuvarlanarak kullanıcının cüzdan bakiyesine iade edilir ve veri tutarlılığı için `Transactions` tablosuna "Gelir" olarak işlenir.

### 👤 5. Tek Formda Güvenli Profil & Fotoğraf Yönetimi
Kullanıcı ad, e-posta ve fotoğrafını tek bir buton tetiklemesiyle günceller. Fotoğraf seçildiğinde JavaScript `FileReader` API ile anlık önizleme (preview) yapılır ancak kullanıcı "Bilgileri Güncelle" butonuna basmadan sayfada refresh veya sunucuya POST işlemi gerçekleşmez. Dosya yolları `.NET Core` `IWebHostEnvironment` ile `wwwroot/img/profile` kök dizinine benzersiz `Guid` isimleriyle kaydedilir.

---

## 🧰 Teknoloji Yığını (Tech Stack)

* **Backend:** .NET Core 8, Entity Framework Core (Code-First)
* **Veritabanı:** SQLite (Hafif, taşınabilir ve sunucu bağımsız)
* **Güvenlik & Oturum:** Microsoft ASP.NET Core Identity, BCrypt Hashing
* **Ödeme Geçidi:** iyzico API integration
* **Frontend:** HTML5, CSS3, JavaScript, Bootstrap, FontAwesome, AOS (Animate on Scroll)

---

## 🚀 Kurulum ve Çalıştırma

Projeyi yerel bilgisayarınızda ayağa kaldırmak için aşağıdaki adımları takip edebilirsiniz:

1. Projeyi bilgisayarınıza klonlayın:
   ```bash
   git clone <github-repo-linkiniz>
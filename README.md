# 📚 Kütüphane Yönetim Sistemi

Modern bir kütüphane yönetim uygulaması. ASP.NET Core 8.0, Entity Framework Core ve SQL Server kullanılarak geliştirilmiştir.

## 🚀 Özellikler

### 👥 Kullanıcı Yönetimi
- Kullanıcı kaydı ve girişi
- Admin ve müşteri rolleri
- Güvenli şifre hashleme (BCrypt)
- Session tabanlı kimlik doğrulama

### 📖 Kitap Yönetimi
- Kitap ekleme, düzenleme, silme
- Kategori bazlı sınıflandırma
- Resim yükleme desteği
- Stok takibi

### 🛒 Sipariş Sistemi
- Sepete kitap ekleme
- Sipariş oluşturma
- Sipariş geçmişi görüntüleme
- Admin sipariş yönetimi

### ❤️ Favori Sistemi
- Kitap favorilere ekleme
- Favori listesi görüntüleme
- Favori yönetimi

### 🔍 Arama ve Filtreleme
- Kitap arama
- Kategori bazlı filtreleme
- Gelişmiş arama seçenekleri

## 🛠️ Teknolojiler

- **Backend**: ASP.NET Core 8.0
- **Veritabanı**: SQL Server
- **ORM**: Entity Framework Core
- **Frontend**: Bootstrap 5, jQuery
- **Kimlik Doğrulama**: Session-based
- **Şifreleme**: BCrypt

## 📋 Gereksinimler

- .NET 8.0 SDK
- SQL Server 2019 veya üzeri
- Visual Studio 2022 veya VS Code

## ⚙️ Kurulum

### 1. Projeyi Klonlayın
```bash
git clone [repository-url]
cd kütüphaneuygulaması
```

### 2. Veritabanı Bağlantısını Yapılandırın
`appsettings.json` dosyasında connection string'i güncelleyin:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=kütüphaneuygulaması;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### 3. Veritabanını Oluşturun
```bash
# Package Manager Console
Add-Migration InitialCreate
Update-Database
```

### 4. Uygulamayı Çalıştırın
```bash
dotnet run
```

## 👤 Varsayılan Admin Hesabı

- **Kullanıcı Adı**: `admin`
- **Şifre**: `Admin123!`
- **Email**: `admin@admin.com`

## 📁 Proje Yapısı

```
kütüphaneuygulaması/
├── Controllers/          # MVC Controller'ları
├── Models/              # Veri modelleri
├── Views/               # Razor view'ları
├── Data/                # Entity Framework context
├── Services/            # İş mantığı servisleri
├── Middleware/          # Özel middleware'ler
└── wwwroot/            # Statik dosyalar
```

## 🔧 API Endpoints

### Kimlik Doğrulama
- `POST /usersaccounts/Login` - Kullanıcı girişi
- `POST /usersaccounts/Logout` - Kullanıcı çıkışı
- `POST /usersaccounts/Create` - Yeni kullanıcı kaydı

### Kitap İşlemleri
- `GET /Books/Index` - Kitap listesi
- `GET /Books/Details/{id}` - Kitap detayları
- `POST /Books/Create` - Yeni kitap ekleme
- `PUT /Books/Edit/{id}` - Kitap düzenleme
- `DELETE /Books/Delete/{id}` - Kitap silme

### Sipariş İşlemleri
- `GET /orders/Index` - Sipariş listesi
- `POST /orders/Create` - Yeni sipariş
- `GET /orders/customerOrders` - Müşteri siparişleri

## 🐳 Docker Desteği

### Docker ile Çalıştırma
```bash
# Docker Compose ile
docker-compose up -d

# Sadece Docker ile
docker build -t kutuphane-app .
docker run -p 8080:80 kutuphane-app
```

## 🔒 Güvenlik

- BCrypt ile şifre hashleme
- Session tabanlı kimlik doğrulama
- CSRF koruması
- XSS koruması
- SQL Injection koruması

## 📊 Performans

- Entity Framework Core ile optimize edilmiş sorgular
- Lazy loading desteği
- Connection pooling
- Caching stratejileri

## 🧪 Test

```bash
# Unit testleri çalıştır
dotnet test

# Coverage raporu
dotnet test --collect:"XPlat Code Coverage"
```


## 📞 İletişim

Proje ile ilgili sorularınız için issue açabilirsiniz.

---

**Not**: Bu uygulama eğitim amaçlı geliştirilmiştir ve production ortamında kullanılmadan önce ek güvenlik önlemleri alınmalıdır. 

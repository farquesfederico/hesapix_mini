# Hesapix API - Muhasebe Yönetim Sistemi

Modern, güvenli ve ölçeklenebilir muhasebe yönetim sistemi backend API'si.

## 🚀 Özellikler

- ✅ JWT tabanlı güvenli authentication
- ✅ BCrypt ile şifre hashleme
- ✅ Email doğrulama sistemi
- ✅ Şifre sıfırlama
- ✅ Rate limiting (brute force koruması)
- ✅ Global exception handling
- ✅ FluentValidation ile input validation
- ✅ AutoMapper ile object mapping
- ✅ Serilog ile structured logging
- ✅ Health checks
- ✅ Swagger API dokümantasyonu
- ✅ CORS yapılandırması
- ✅ PostgreSQL veritabanı
- ✅ Entity Framework Core

## 📋 Gereksinimler

- .NET 8.0 SDK
- PostgreSQL 14+
- SMTP Server (email için)

## 🔧 Kurulum

### 1. Repository'yi klonlayın

```bash
git clone https://github.com/yourusername/hesapix-mini.git
cd hesapix-mini
```

### 2. Bağımlılıkları yükleyin

```bash
dotnet restore
```

### 3. User Secrets yapılandırın

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=HesapixDB;Username=postgres;Password=yourpassword"
dotnet user-secrets set "Jwt:Key" "YourSuperSecretKeyHere-MinimumLength32Characters"
dotnet user-secrets set "Jwt:Issuer" "HesapixAPI"
dotnet user-secrets set "Jwt:Audience" "HesapixClient"
dotnet user-secrets set "Email:SmtpHost" "smtp.gmail.com"
dotnet user-secrets set "Email:SmtpPort" "587"
dotnet user-secrets set "Email:Username" "youremail@gmail.com"
dotnet user-secrets set "Email:Password" "yourapppassword"
```

### 4. Veritabanı Migration

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Uygulamayı çalıştırın

```bash
dotnet run
```

API şu adreste çalışacak: `https://localhost:5001` veya `http://localhost:5000`

Swagger UI: `https://localhost:5001` (Development mode)

## 📁 Proje Yapısı

```
Hesapix/
├── Controllers/           # API endpoints
├── Services/
│   ├── Interfaces/       # Service interfaces
│   └── Implementations/  # Service implementations
├── Models/
│   ├── Entities/         # Database entities
│   ├── DTOs/            # Data transfer objects
│   └── Common/          # Common models (ApiResponse, etc.)
├── Middleware/           # Custom middleware
├── Validators/           # FluentValidation validators
├── Mapping/             # AutoMapper profiles
├── Data/                # DbContext
├── Migrations/          # EF Core migrations
└── Program.cs           # Application entry point
```

## 🔐 Güvenlik Özellikleri

### 1. Şifre Güvenliği
- BCrypt ile hash (work factor: 12)
- Minimum 8 karakter, büyük/küçük harf ve rakam zorunluluğu
- Salt otomatik eklenir

### 2. Authentication
- JWT Bearer token (7 gün geçerli)
- Secure token generation
- Token expiration kontrolü

### 3. Brute Force Koruması
- 5 başarısız denemeden sonra 30 dakika hesap kilidi
- Rate limiting (IP bazlı)
  - Genel: 60 istek/dakika
  - Login: 5 deneme/dakika
  - Register: 3 kayıt/saat

### 4. CORS
- Development: Tüm originlere açık
- Production: Sadece tanımlı domainlere izin

### 5. Security Headers
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Referrer-Policy: no-referrer

## 📧 Email Servisi

### Gmail Kullanımı

1. Google hesabınızda 2FA açın
2. App Password oluşturun: https://myaccount.google.com/apppasswords
3. User secrets'a ekleyin:

```bash
dotnet user-secrets set "Email:SmtpHost" "smtp.gmail.com"
dotnet user-secrets set "Email:SmtpPort" "587"
dotnet user-secrets set "Email:Username" "youremail@gmail.com"
dotnet user-secrets set "Email:Password" "your-app-password"
```

## 🧪 API Endpoints

### Authentication

```
POST   /api/v1/auth/register              - Yeni kullanıcı kaydı
POST   /api/v1/auth/login                 - Kullanıcı girişi
POST   /api/v1/auth/verify-email          - Email doğrulama
POST   /api/v1/auth/request-password-reset - Şifre sıfırlama talebi
POST   /api/v1/auth/reset-password        - Şifre sıfırlama
GET    /api/v1/auth/me                    - Kullanıcı bilgileri
GET    /api/v1/auth/check-subscription    - Abonelik kontrolü
POST   /api/v1/auth/logout                - Çıkış
```

### Sales

```
GET    /api/v1/sale                       - Tüm satışları listele
GET    /api/v1/sale/{id}                  - ID'ye göre satış
GET    /api/v1/sale/by-number/{number}    - Satış numarasına göre
POST   /api/v1/sale                       - Yeni satış
POST   /api/v1/sale/{id}/cancel           - Satış iptal
GET    /api/v1/sale/pending-payments      - Bekleyen ödemeler
GET    /api/v1/sale/statistics            - Satış istatistikleri
```

### Stock

```
GET    /api/v1/stock                      - Tüm stokları listele
GET    /api/v1/stock/{id}                 - Stok detayı
POST   /api/v1/stock                      - Yeni stok
PUT    /api/v1/stock/{id}                 - Stok güncelle
DELETE /api/v1/stock/{id}                 - Stok sil (soft delete)
GET    /api/v1/stock/low-stock            - Düşük stok uyarısı
```

### Payments

```
GET    /api/v1/payment                    - Tüm ödemeleri listele
GET    /api/v1/payment/{id}               - Ödeme detayı
POST   /api/v1/payment                    - Yeni ödeme
DELETE /api/v1/payment/{id}               - Ödeme sil
GET    /api/v1/payment/by-sale/{saleId}   - Satışa göre ödemeler
```

### Reports

```
GET    /api/v1/report/dashboard           - Dashboard raporu
```

## 🏥 Health Checks

```
GET    /health                            - Genel health check
GET    /health/ready                      - Readiness probe
GET    /health/live                       - Liveness probe
GET    /health-ui                         - Health check UI (Development)
```

## 📝 Örnek Request/Response

### Register

**Request:**
```json
POST /api/v1/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "StrongPass123",
  "fullName": "John Doe",
  "phoneNumber": "5551234567",
  "companyName": "ACME Corp",
  "taxNumber": "1234567890"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Kayıt başarılı. Email adresinizi doğrulamayı unutmayın.",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": 1,
      "email": "user@example.com",
      "fullName": "John Doe",
      "emailVerified": false
    },
    "hasActiveSubscription": false
  },
  "errors": [],
  "timestamp": "2026-01-13T10:00:00Z"
}
```

## 🐳 Docker (Opsiyonel)

```dockerfile
# Dockerfile örneği
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Hesapix.csproj", "./"]
RUN dotnet restore "Hesapix.csproj"
COPY . .
RUN dotnet build "Hesapix.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hesapix.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hesapix.dll"]
```

## 🔍 Logging

Loglar `logs/` klasöründe günlük olarak saklanır:
- Console output
- File output (30 gün retention)
- Structured logging (Serilog)

## 📈 Monitoring

- Health checks: `/health`
- Health UI: `/health-ui` (Development)
- Application Insights entegrasyonu eklenebilir

## 🤝 Katkıda Bulunma

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit edin (`git commit -m 'feat: Add amazing feature'`)
4. Push edin (`git push origin feature/amazing-feature`)
5. Pull Request açın

## 📄 Lisans

Bu proje MIT lisansı altındadır.

## 👥 İletişim

Sorularınız için: support@hesapix.com

## 🎯 Roadmap

- [ ] Unit & Integration testleri
- [ ] Redis cache entegrasyonu
- [ ] Hangfire ile background jobs
- [ ] PDF fatura oluşturma
- [ ] Excel export
- [ ] Real-time notifications (SignalR)
- [ ] Multi-tenancy desteği
- [ ] Audit log
- [ ] Advanced reporting

## ⚠️ Önemli Notlar

1. **Production'a geçmeden önce:**
   - User Secrets yerine Azure Key Vault veya AWS Secrets Manager kullanın
   - HTTPS zorunlu olmalı
   - Rate limiting ayarlarını düzenleyin
   - CORS ayarlarını sıkılaştırın
   - Log seviyelerini ayarlayın
   - Health check endpoint'lerini güvenceye alın

2. **Güvenlik:**
   - JWT secret key minimum 32 karakter olmalı
   - Database şifreleri güçlü olmalı
   - SMTP credentials güvende tutulmalı
   - appsettings.json asla commit edilmemeli

3. **Performance:**
   - Database indexleri ekleyin
   - Query optimization yapın
   - Caching stratejisi belirleyin
   - Connection pooling ayarlayın

---

Made with ❤️ by Hesapix Team
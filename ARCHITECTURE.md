# Hệ thống Quản lý Sự kiện và Bán vé (Ticketing)

> Đồ án: Xây dựng máy chủ phía server (Backend) theo kiến trúc **Modular Monolith**, đóng gói **Docker Compose**, lưu trữ **Cloudflare R2**, triển khai **VPS + Cloudflare (domain `.id.vn`)** theo hướng **CI/CD**.

---

## Mục lục

1. [Tổng quan](#1-tổng-quan)
2. [Công nghệ sử dụng](#2-công-nghệ-sử-dụng)
3. [Kiến trúc hệ thống](#3-kiến-trúc-hệ-thống)
4. [Kiến trúc Modular Monolith](#4-kiến-trúc-modular-monolith)
5. [Cấu trúc thư mục](#5-cấu-trúc-thư-mục)
6. [Cấu hình môi trường](#6-cấu-hình-môi-trường)
7. [Triển khai và CI/CD](#7-triển-khai-và-cicd)
8. [Phân công công việc nhóm 5 người](#8-phân-công-công-việc-nhóm-5-người)
9. [Quy trình làm việc nhóm](#9-quy-trình-làm-việc-nhóm)
10. [Codebase tham khảo](#10-codebase-tham-khảo)
11. [Rủi ro và cách giảm thiểu](#11-rủi-ro-và-cách-giảm-thiểu)
12. [Checklist nghiệm thu](#12-checklist-nghiệm-thu)

---

## 1. Tổng quan

### 1.1 Mục tiêu

Xây dựng nền tảng cho phép ban tổ chức tạo và quản lý sự kiện, bán vé trực tuyến; khách hàng tìm sự kiện, mua vé, nhận vé điện tử (QR); nhân viên soát vé check-in tại cổng.

### 1.2 Chuẩn đầu ra CLO1

| Yêu cầu | Cách đáp ứng |
|---|---|
| Backend hoàn chỉnh, kiến trúc Modular Monolith | 6 module độc lập trong một process, có test kiểm tra ranh giới |
| Docker Compose | Compose cho dev (có MinIO) và prod (dùng R2) |
| Lưu trữ MinIO/S3/R2 | Interface `IFileStorage` dùng AWSSDK.S3; dev = MinIO, prod = R2 |
| Triển khai VPS có domain | Domain `.id.vn`, DNS chuyển sang Cloudflare |
| SSL/TLS qua Cloudflare | Browser → HTTPS (CA Cloudflare) → Edge → TLS (Origin CA) → VPS |
| Docker registry | GitHub Container Registry (GHCR) |
| CI/CD | GitHub Actions: build, test, push image, deploy qua SSH |

### 1.3 Vai trò người dùng

| Vai trò | Chức năng chính |
|---|---|
| Khách (Customer) | Xem sự kiện, mua vé, xem "Vé của tôi" |
| Ban tổ chức (Organizer) | Tạo sự kiện, cấu hình hạng vé, xem báo cáo doanh thu |
| Nhân viên soát vé (Staff) | Quét QR check-in |
| Quản trị (Admin) | Duyệt sự kiện, quản lý người dùng |

### 1.4 Phạm vi

**MVP (bắt buộc):** đăng ký/đăng nhập, CRUD sự kiện, hạng vé, giữ vé tạm thời, đặt hàng, thanh toán sandbox, vé QR qua email, check-in, báo cáo cơ bản.

**Mở rộng (nếu còn thời gian):** mã giảm giá, SignalR cập nhật số vé còn lại, waiting room, đa ngôn ngữ, hoàn vé.

### 1.5 Yêu cầu chức năng và phi chức năng

#### 1.5.1 Yêu cầu chức năng (hệ thống làm được gì)

Mức ưu tiên: **P1** = bắt buộc (MVP), **P2** = mở rộng nếu còn thời gian.

| Mã | Nhóm | Yêu cầu | Vai trò | Module | Ưu tiên |
|---|---|---|---|---|:-:|
| FR-01 | Tài khoản | Đăng ký, đăng nhập, đăng xuất bằng email và mật khẩu | Tất cả | Identity | P1 |
| FR-02 | Tài khoản | Xác thực email, quên và đặt lại mật khẩu | Tất cả | Identity | P2 |
| FR-03 | Tài khoản | Phân quyền theo vai trò: Customer, Organizer, Staff, Admin | Tất cả | Identity | P1 |
| FR-04 | Sự kiện | Xem danh sách sự kiện, tìm kiếm theo từ khóa | Customer | Events | P1 |
| FR-05 | Sự kiện | Lọc theo danh mục, ngày, thành phố, khoảng giá; sắp xếp; phân trang | Customer | Events | P1 |
| FR-06 | Sự kiện | Xem chi tiết sự kiện: mô tả, lịch trình, địa điểm, các hạng vé còn lại | Customer | Events | P1 |
| FR-07 | Sự kiện | Tạo, sửa, xóa sự kiện; upload ảnh bìa lên R2 | Organizer | Events | P1 |
| FR-08 | Sự kiện | Quản lý trạng thái: Nháp → Chờ duyệt → Đang bán → Đã kết thúc | Organizer | Events | P1 |
| FR-09 | Sự kiện | Duyệt hoặc từ chối sự kiện (kèm lý do) | Admin | Events | P1 |
| FR-10 | Vé | Tạo, sửa hạng vé: tên, giá, số lượng, thời gian mở bán, giới hạn mua | Organizer | Ticketing | P1 |
| FR-11 | Vé | Giữ vé tạm thời 10 phút khi bắt đầu mua; tự nhả vé khi hết hạn | Customer | Ticketing | P1 |
| FR-12 | Vé | Không bán vượt số lượng vé khi nhiều người mua cùng lúc | Hệ thống | Ticketing | P1 |
| FR-13 | Đơn hàng | Tạo đơn hàng từ vé đã giữ, xem lịch sử đơn hàng | Customer | Orders | P1 |
| FR-14 | Thanh toán | Thanh toán trực tuyến qua VNPay sandbox, xử lý kết quả thành công/thất bại | Customer | Orders | P1 |
| FR-15 | Thanh toán | Nhận webhook từ cổng thanh toán, xử lý lặp không tạo vé trùng | Hệ thống | Orders | P1 |
| FR-16 | Thanh toán | Áp mã giảm giá; thanh toán MoMo; hủy đơn và hoàn vé | Customer | Orders | P2 |
| FR-17 | Vé điện tử | Phát hành vé có mã QR ký số sau khi thanh toán | Hệ thống | Ticketing | P1 |
| FR-18 | Vé điện tử | Gửi email xác nhận và vé điện tử, tự thử lại khi gửi lỗi | Hệ thống | Notifications | P1 |
| FR-19 | Vé điện tử | Xem "Vé của tôi" (sắp tới, đã qua) và mã QR | Customer | Ticketing | P1 |
| FR-20 | Check-in | Quét QR, xác thực chữ ký, báo Hợp lệ / Đã sử dụng / Không hợp lệ | Staff | CheckIn | P1 |
| FR-21 | Check-in | Chặn quét lần hai; hiển thị bộ đếm "Đã vào / Tổng vé" | Staff | CheckIn | P1 |
| FR-22 | Quản lý bán hàng | Xem, lọc, tìm kiếm đơn hàng và người tham dự; xuất danh sách | Organizer | Orders | P1 |
| FR-23 | Báo cáo | Dashboard: doanh thu, vé đã bán, lượt check-in; biểu đồ theo ngày và theo hạng vé | Organizer | Events | P1 |
| FR-24 | Quản trị | Xem, khóa/mở khóa người dùng, gán vai trò | Admin | Identity | P1 |
| FR-25 | Realtime | Cập nhật số vé còn lại theo thời gian thực (SignalR) | Customer | Ticketing | P2 |

#### 1.5.2 Yêu cầu phi chức năng (hệ thống cần đáp ứng như thế nào)

> Các con số là **mục tiêu thiết kế** cho quy mô đồ án (một VPS 2GB RAM), cần đo lại bằng test tải (k6) trước khi nộp.

| Mã | Nhóm | Yêu cầu | Tiêu chí đo / cách kiểm chứng |
|---|---|---|---|
| NFR-01 | Hiệu năng | API đọc (danh sách, chi tiết sự kiện) phản hồi nhanh | p95 ≤ 500 ms với 100 người dùng đồng thời (k6) |
| NFR-02 | Hiệu năng | API ghi (giữ vé, tạo đơn) phản hồi chấp nhận được | p95 ≤ 1 s |
| NFR-03 | Hiệu năng | Trang web tải nhanh | Trang chủ tải ≤ 3 s trên mạng 4G; ảnh qua R2/CDN |
| NFR-04 | Hiệu năng | Danh sách sự kiện công khai được cache | Redis cache, giảm truy vấn DB lặp lại |
| NFR-05 | Tính đúng đắn | Không bán vượt số vé | Test đồng thời 100+ request mua cùng lúc: số vé bán ≤ tồn kho |
| NFR-06 | Tính đúng đắn | Thanh toán lặp không tạo vé trùng | Gọi webhook nhiều lần: chỉ một vé được phát hành |
| NFR-07 | Bảo mật | Xác thực và phân quyền | JWT ngắn hạn (15 phút) + refresh token; mỗi endpoint kiểm tra role/policy |
| NFR-08 | Bảo mật | Bảo vệ mật khẩu và secret | Mật khẩu băm (ASP.NET Identity); secret nằm trong `.env`, không commit lên git |
| NFR-09 | Bảo mật | Mã QR không thể làm giả | Ký HMAC-SHA256, xác thực phía server khi check-in |
| NFR-10 | Bảo mật | Mã hóa đường truyền | HTTPS toàn bộ: Browser → Cloudflare → Origin CA → VPS (Full strict) |
| NFR-11 | Bảo mật | Chống lạm dụng và tấn công phổ biến | Rate limit, CORS giới hạn domain, chống SQL injection (EF Core tham số hóa), kiểm tra loại và kích thước file upload |
| NFR-12 | Bảo mật | Giảm bề mặt tấn công hạ tầng | Postgres/Redis không mở ra ngoài; 80/443 chỉ nhận từ IP Cloudflare; SSH bằng key |
| NFR-13 | Tính dễ sử dụng | Giao diện tiếng Việt, nhất quán | Dùng chung hệ thống component; đúng bản thiết kế Figma |
| NFR-14 | Tính dễ sử dụng | Responsive | Hiển thị tốt ở 1440px và 390px; trang check-in ưu tiên điện thoại |
| NFR-15 | Tính dễ sử dụng | Mua vé đơn giản | Từ chi tiết sự kiện đến thanh toán trong tối đa 3 bước; thông báo lỗi rõ ràng bằng tiếng Việt |
| NFR-16 | Tính dễ sử dụng | Dễ tiếp cận | Độ tương phản đạt WCAG AA, có trạng thái focus, loading, trống, lỗi |
| NFR-17 | Độ tin cậy | Sẵn sàng và phục hồi | Container tự khởi động lại (`restart: unless-stopped`), có `/health`; backup DB hằng ngày lên R2, đã thử khôi phục |
| NFR-18 | Độ tin cậy | Không mất sự kiện liên module | Outbox pattern đảm bảo `OrderPaid` được xử lý ít nhất một lần |
| NFR-19 | Khả năng mở rộng | Có thể tách module thành dịch vụ sau này | Ranh giới module qua Contracts, schema DB riêng, kiểm tra bằng Architecture Test |
| NFR-20 | Khả năng bảo trì | Mã nguồn dễ đọc, dễ kiểm thử | Phân lớp Domain/Application/Infrastructure; analyzer, nullable, `strict` TypeScript; test cho nghiệp vụ chính |
| NFR-21 | Khả năng bảo trì | Có tài liệu API | OpenAPI + Scalar; frontend sinh type tự động |
| NFR-22 | Triển khai | Triển khai tự động, lặp lại được | CI/CD (GitHub Actions), image gắn tag commit SHA, rollback bằng cách đổi tag |
| NFR-23 | Triển khai | Môi trường nhất quán | Docker Compose cho dev và prod; dev dùng MinIO, prod dùng R2 cùng một interface |
| NFR-24 | Quan sát | Truy vết lỗi | Log có cấu trúc (Serilog), health check, theo dõi tác vụ nền (Hangfire dashboard) |
| NFR-25 | Tương thích | Trình duyệt hỗ trợ | Chrome, Edge, Firefox, Safari bản mới (2 phiên bản gần nhất) |
| NFR-26 | Dữ liệu | Thời gian và tiền tệ nhất quán | Lưu UTC, hiển thị giờ Việt Nam; tiền tệ VND dùng `decimal`, định dạng "1.200.000₫" |

---

## 2. Công nghệ sử dụng

| Tầng | Công nghệ |
|---|---|
| **Backend** | .NET 10 (C# 14), ASP.NET Core Minimal APIs, EF Core + Npgsql, FluentValidation |
| **Dữ liệu** | PostgreSQL 16 (mỗi module một schema), Redis 7 (cache, rate limit) |
| **Xác thực** | ASP.NET Core Identity + JWT + Refresh Token |
| **Tác vụ nền / Email / QR** | Hangfire, MailKit, QRCoder (ký HMAC-SHA256) |
| **Lưu file** | AWSSDK.S3: MinIO (dev), Cloudflare R2 (prod) |
| **Thanh toán** | VNPay sandbox (tùy chọn MoMo) |
| **Tài liệu API / Log** | OpenAPI + Scalar, Serilog, HealthChecks |
| **Frontend** | React 19 + TypeScript, Vite, React Router, Tailwind CSS + shadcn/ui |
| **Frontend (dữ liệu)** | TanStack Query + `openapi-typescript` (sinh type từ OpenAPI), React Hook Form + Zod, Zustand |
| **Frontend (khác)** | `html5-qrcode` (quét QR), Recharts (biểu đồ) |
| **Hạ tầng** | Docker Compose, Nginx, GitHub Actions, GHCR, VPS Ubuntu, Cloudflare (DNS, SSL Full strict, Origin CA) |
| **Kiểm thử** | xUnit, Testcontainers, NetArchTest (kiểm tra ranh giới module), Vitest, Playwright |

> Mở rộng nếu còn thời gian: SignalR (cập nhật số vé realtime), Uptime Kuma (giám sát).

---

## 3. Kiến trúc hệ thống

### 3.1 Sơ đồ triển khai

```
 Browser
   │  HTTPS (chứng chỉ do Cloudflare cấp)
   ▼
 Cloudflare Edge  (DNS, proxy, WAF, cache tĩnh)
   │  HTTPS (Origin CA Certificate) - chế độ Full (strict)
   ▼
 VPS (Ubuntu + Docker Compose)
   │
   └─ nginx :443
        ├─ tenmien.id.vn      ──▶ web  (React build, nginx)
        └─ api.tenmien.id.vn  ──▶ api  (ASP.NET Core :8080)
                                     │
                     ┌───────────────┼────────────────┐
                     ▼               ▼                ▼
                 PostgreSQL        Redis        Cloudflare R2
                 (volume)                       (S3 API, bên ngoài)
```

### 3.2 Luồng mua vé (luồng quan trọng nhất)

```
1. Khách chọn hạng vé + số lượng
2. Ticketing: tạo Reservation (giữ vé) có ExpiresAt = now + 10 phút
      └─ trừ tồn kho trong transaction, chống bán vượt bằng concurrency token
3. Orders: tạo Order (Pending) từ Reservation, tạo link thanh toán VNPay/MoMo
4. Khách thanh toán trên cổng thanh toán
5. Cổng gọi webhook về /api/orders/payments/{provider}/callback
      └─ xác thực chữ ký, xử lý idempotent (không tạo vé trùng)
6. Orders: Order → Paid, phát integration event OrderPaid (qua Outbox)
7. Ticketing: phát hành Ticket (mã QR ký HMAC)
8. Notifications: gửi email vé điện tử
9. Nếu quá hạn mà chưa thanh toán: job nền hủy Reservation, trả tồn kho
```

---

## 4. Kiến trúc Modular Monolith

Một solution, một process, một database, nhưng chia thành các module có ranh giới rõ ràng.

### 4.1 Danh sách module

| Module | Schema DB | Trách nhiệm | Phụ thuộc (qua Contracts) |
|---|---|---|---|
| **Identity** | `identity` | Đăng ký, đăng nhập, JWT, vai trò | không |
| **Events** | `events` | Sự kiện, địa điểm, danh mục, ảnh (R2) | Identity |
| **Ticketing** | `ticketing` | Hạng vé, tồn kho, Reservation, phát hành Ticket | Events |
| **Orders** | `orders` | Đơn hàng, thanh toán, webhook, mã giảm giá | Ticketing |
| **CheckIn** | `checkin` | Xác thực QR, ghi nhận vào cổng | Ticketing |
| **Notifications** | `notifications` | Email, mẫu thư, lịch sử gửi | lắng nghe event |

### 4.2 Cấu trúc bên trong mỗi module

```
Module.Domain          Entity, Value Object, Domain Event, quy tắc nghiệp vụ (không phụ thuộc gì)
Module.Application     Command/Query, handler, validator, DTO
Module.Infrastructure  DbContext, repository, migration, tích hợp ngoài
Module.Contracts       Interface công khai + Integration Event (module khác CHỈ được tham chiếu cái này)
Module.Api             Endpoint, đăng ký DI: AddXxxModule(), MapXxxEndpoints()
```

### 4.3 Quy tắc ranh giới (bắt buộc)

1. Module chỉ tham chiếu **`Contracts`** của module khác, **không** tham chiếu Domain/Application/Infrastructure.
2. Mỗi module sở hữu schema riêng; **cấm** join hoặc truy vấn bảng của module khác. Cần dữ liệu thì gọi interface trong Contracts.
3. Thay đổi trạng thái liên module dùng **Integration Event + Outbox** để đảm bảo nhất quán và không mất event.
4. Một transaction chỉ nên nằm trong một module.
5. Kiểm tra bằng **NetArchTest** trong CI; vi phạm là build đỏ.

```csharp
// Ví dụ test ranh giới
[Fact]
public void Events_Module_Should_Not_Depend_On_Ticketing_Internals()
{
    var result = Types.InAssembly(typeof(EventsModule).Assembly)
        .ShouldNot()
        .HaveDependencyOn("Ticketing.Domain")
        .GetResult();

    Assert.True(result.IsSuccessful);
}
```

### 4.4 Chống bán vượt vé

- Bảng `TicketTypes` có cột `Version` (concurrency token) và `QuantityAvailable`.
- Giảm tồn kho bằng một câu `UPDATE ... SET available = available - @qty WHERE id = @id AND available >= @qty` trong transaction, kiểm tra số dòng bị ảnh hưởng.
- Redis dùng cho rate limit và khóa ngắn hạn, **không** là nguồn sự thật duy nhất về tồn kho.

---

## 5. Cấu trúc thư mục

```
ticketing/
├── .github/
│   └── workflows/
│       ├── ci.yml                     # build + test + arch test + lint
│       └── cd.yml                     # build image, push GHCR, deploy SSH
├── backend/
│   ├── Ticketing.sln
│   ├── Directory.Build.props          # phiên bản .NET, analyzer chung
│   ├── Directory.Packages.props       # quản lý phiên bản NuGet tập trung
│   ├── Dockerfile
│   ├── src/
│   │   ├── Api/
│   │   │   └── Ticketing.Api/         # Host: Program.cs, middleware, đăng ký module
│   │   ├── BuildingBlocks/
│   │   │   ├── Ticketing.SharedKernel/        # Entity, Result, DomainEvent, IClock
│   │   │   └── Ticketing.Infrastructure.Core/ # Outbox, EventBus, IFileStorage, Hangfire
│   │   └── Modules/
│   │       ├── Identity/
│   │       │   ├── Identity.Domain/
│   │       │   ├── Identity.Application/
│   │       │   ├── Identity.Infrastructure/
│   │       │   ├── Identity.Contracts/
│   │       │   └── Identity.Api/
│   │       ├── Events/        (cấu trúc 5 project tương tự)
│   │       ├── Ticketing/
│   │       ├── Orders/
│   │       ├── CheckIn/
│   │       └── Notifications/
│   └── tests/
│       ├── Architecture.Tests/        # NetArchTest - kiểm tra ranh giới module
│       ├── Identity.Tests/
│       ├── Events.Tests/
│       ├── Ticketing.Tests/           # gồm test đồng thời chống bán vượt
│       ├── Orders.Tests/
│       └── Integration.Tests/         # Testcontainers (Postgres, Redis, MinIO)
├── frontend/
│   ├── package.json
│   ├── vite.config.ts
│   ├── Dockerfile                     # build Vite → nginx
│   ├── nginx.conf
│   └── src/
│       ├── api/                       # client sinh từ OpenAPI (schema.d.ts)
│       ├── app/                       # router, providers, layout
│       ├── features/
│       │   ├── auth/
│       │   ├── events/                # danh sách, chi tiết
│       │   ├── checkout/              # giỏ vé, đếm ngược giữ vé, thanh toán
│       │   ├── my-tickets/
│       │   ├── organizer/             # dashboard, quản lý sự kiện, báo cáo
│       │   └── checkin/               # trang quét QR
│       ├── components/                # UI dùng chung (shadcn/ui)
│       ├── hooks/
│       ├── lib/
│       └── store/                     # Zustand
├── deploy/
│   ├── docker-compose.prod.yml
│   ├── nginx/
│   │   └── default.conf
│   ├── scripts/
│   │   ├── deploy.sh
│   │   ├── rollback.sh
│   │   └── backup-db.sh               # pg_dump → R2
│   └── .env.prod.example
├── docs/
│   ├── architecture.md
│   ├── adr/                           # Architecture Decision Records
│   ├── erd/                           # sơ đồ DB từng module
│   └── api-flow/                      # sơ đồ luồng mua vé, thanh toán
├── docker-compose.yml                 # môi trường dev (Postgres, Redis, MinIO, api, web)
├── .env.example
├── .gitignore
└── README.md
```

---

## 6. Cấu hình môi trường

### 6.1 Ba môi trường

| Môi trường | Mục đích | Lưu trữ file | Cấu hình |
|---|---|---|---|
| Development | Chạy local | MinIO (container) | `.env` (từ `.env.example`) |
| CI | Chạy test | Testcontainers | Biến môi trường trong workflow |
| Production | VPS | Cloudflare R2 | `/opt/ticketing/.env` trên VPS (chmod 600) |

### 6.2 `.env.example` (dev)

```dotenv
# ---------- App ----------
ASPNETCORE_ENVIRONMENT=Development
TAG=dev
GH_USER=your-github-username

# ---------- PostgreSQL ----------
POSTGRES_USER=ticketing
POSTGRES_PASSWORD=change_me
POSTGRES_DB=ticketing
ConnectionStrings__Default=Host=postgres;Port=5432;Database=ticketing;Username=ticketing;Password=change_me

# ---------- Redis ----------
ConnectionStrings__Redis=redis:6379

# ---------- JWT ----------
Jwt__Issuer=ticketing-api
Jwt__Audience=ticketing-web
Jwt__Key=thay-bang-chuoi-ngau-nhien-toi-thieu-32-ky-tu
Jwt__AccessTokenMinutes=15
Jwt__RefreshTokenDays=7

# ---------- Storage (dev: MinIO) ----------
Storage__Provider=S3
Storage__ServiceUrl=http://minio:9000
Storage__AccessKey=minioadmin
Storage__SecretKey=minioadmin
Storage__Bucket=ticketing-dev
Storage__ForcePathStyle=true
Storage__PublicBaseUrl=http://localhost:9000/ticketing-dev

# ---------- Email ----------
Smtp__Host=smtp.example.com
Smtp__Port=587
Smtp__User=
Smtp__Password=
Smtp__From=no-reply@tenmien.id.vn

# ---------- Thanh toán (sandbox) ----------
Payment__VnPay__TmnCode=
Payment__VnPay__HashSecret=
Payment__VnPay__BaseUrl=https://sandbox.vnpayment.vn/paymentv2/vpcpay.html
Payment__VnPay__ReturnUrl=http://localhost:5173/payment/result

# ---------- Vé / QR ----------
Ticket__QrSigningKey=thay-bang-chuoi-ngau-nhien-khac
Reservation__HoldMinutes=10

# ---------- CORS ----------
Cors__AllowedOrigins=http://localhost:5173
```

> Định dạng `A__B` là cách ASP.NET Core ánh xạ biến môi trường sang `A:B` trong `IConfiguration`.

### 6.3 Khác biệt của Production (`.env` trên VPS)

```dotenv
ASPNETCORE_ENVIRONMENT=Production
TAG=latest

# Storage: Cloudflare R2
Storage__ServiceUrl=https://<ACCOUNT_ID>.r2.cloudflarestorage.com
Storage__AccessKey=<R2_ACCESS_KEY>
Storage__SecretKey=<R2_SECRET_KEY>
Storage__Bucket=ticketing-prod
Storage__ForcePathStyle=true
Storage__PublicBaseUrl=https://cdn.tenmien.id.vn

# Thanh toán
Payment__VnPay__ReturnUrl=https://tenmien.id.vn/payment/result

# CORS
Cors__AllowedOrigins=https://tenmien.id.vn

# Mật khẩu, JWT key, QR key: sinh mới, KHÔNG dùng lại giá trị dev
```

### 6.4 Quản lý secret

| Nguyên tắc | Chi tiết |
|---|---|
| Không commit secret | `.env`, `origin.key`, `origin.pem` nằm trong `.gitignore` |
| GitHub Secrets | Chỉ chứa `VPS_HOST`, `VPS_SSH_KEY` (và `VPS_PORT` nếu đổi cổng SSH) |
| Secret ứng dụng | Nằm trong `/opt/ticketing/.env` trên VPS |
| Xoay vòng | Đổi JWT key, mật khẩu DB, R2 key định kỳ hoặc khi lộ |

### 6.5 Cấu hình Cloudflare, VPS

| Hạng mục | Cấu hình |
|---|---|
| Nameserver | Đổi sang 2 NS Cloudflare cấp tại nhà đăng ký `.id.vn` |
| DNS | `A @` và `A api` trỏ IP VPS, bật Proxied |
| SSL/TLS | Chế độ **Full (strict)**, Always Use HTTPS |
| Origin CA | Tạo cert `*.tenmien.id.vn` + `tenmien.id.vn`, lưu `/opt/ticketing/certs/` |
| R2 | Bucket `ticketing-prod`, API token Object Read & Write, tùy chọn custom domain `cdn.` |
| VPS | User `deploy`, SSH key, tắt đăng nhập root/password, swap 2GB |
| Firewall (`ufw`) | Mở 22; 80/443 chỉ nhận từ [dải IP Cloudflare](https://www.cloudflare.com/ips/) |
| Cổng nội bộ | Không publish Postgres/Redis ra host |
| Forwarded headers | Bật `UseForwardedHeaders` trong ASP.NET Core để lấy IP thật (`CF-Connecting-IP`) |

### 6.6 Chạy dự án ở local

```bash
cp .env.example .env
docker compose up -d --build
# Web:        http://localhost:5173
# API:        http://localhost:8080  (Scalar: /scalar)
# MinIO UI:   http://localhost:9001
```

---

## 7. Triển khai và CI/CD

### 7.1 Luồng

```
feature branch ──PR──▶ CI (build, test, arch test, lint)
                            │ merge vào main
                            ▼
                       CD: build image → push GHCR (tag = commit SHA + latest)
                            │
                            ▼
                       SSH vào VPS: compose pull → up -d → healthcheck
```

### 7.2 CI (mỗi Pull Request)

1. `dotnet restore`, `dotnet build`, `dotnet test` (kể cả integration test dùng Testcontainers).
2. Chạy `Architecture.Tests` để kiểm tra ranh giới module.
3. Frontend: `npm ci`, `npm run lint`, `tsc --noEmit`, `npm run build`.
4. Build thử Docker image (không push).

### 7.3 CD (merge vào `main`)

1. Build và push `ticketing-api`, `ticketing-web` lên `ghcr.io/<owner>/...` với tag SHA và `latest`.
2. SSH vào VPS, chạy `docker compose -f docker-compose.prod.yml pull && up -d --remove-orphans`.
3. Gọi `curl https://api.tenmien.id.vn/health`; lỗi thì job fail.
4. Rollback: đặt `TAG=<sha cũ>` rồi `up -d`.

### 7.4 Migration

Dùng EF Core migration bundle cho từng module, chạy trước khi `up`. Với đồ án có thể tự migrate khi khởi động (chỉ bật ở môi trường được kiểm soát).

### 7.5 Backup

Cron `pg_dump` hằng ngày, đẩy lên bucket R2 riêng, giữ 7 bản gần nhất. Kiểm thử khôi phục ít nhất một lần.

---

## 8. Phân công công việc nhóm 5 người

> Giả định thời lượng **12 tuần**. Điều chỉnh theo lịch thực tế của môn học.

### 8.1 Phân vai

| Thành viên | Vai trò | Phụ trách chính |
|---|---|---|
| **TV1** | Tech Lead + DevOps | BuildingBlocks (SharedKernel, Outbox, EventBus), Docker, compose dev/prod, CI/CD, VPS, Cloudflare, R2, Architecture Tests, review PR |
| **TV2** | Backend - Identity & Notifications | Module Identity (đăng ký, đăng nhập, JWT, refresh, role), module Notifications (email, QR trong thư), Hangfire |
| **TV3** | Backend - Events & Storage | Module Events (CRUD, danh mục, địa điểm), `IFileStorage` + R2/MinIO, presigned upload, tìm kiếm/lọc, module báo cáo |
| **TV4** | Backend - Ticketing & Orders | Module Ticketing (tồn kho, Reservation, nhả vé), module Orders (đơn hàng, VNPay/MoMo, webhook idempotent), mã giảm giá, **phần khó nhất** |
| **TV5** | Frontend Lead + CheckIn | Khung React, sinh client từ OpenAPI, trang công khai, checkout, "Vé của tôi", module CheckIn (backend nhỏ + trang quét QR) |

Quy ước cho frontend: TV5 dựng nền tảng và các trang khách hàng; **TV2, TV3, TV4 tự làm màn hình quản trị của module mình** sau khi backend ổn định (từ tuần 6 trở đi) để cân tải.

### 8.2 Kế hoạch theo tuần

| Tuần | TV1 | TV2 | TV3 | TV4 | TV5 |
|---|---|---|---|---|---|
| **1** | Khởi tạo repo, solution, quy ước code, compose dev | Học Identity, thiết kế ERD Identity | Thiết kế ERD Events | Thiết kế ERD Ticketing/Orders | Dựng Vite + Tailwind + shadcn, layout |
| **2** | SharedKernel, Outbox, EventBus, skeleton module | Đăng ký/đăng nhập, JWT | CRUD sự kiện, danh mục | Hạng vé, tồn kho | Trang login/register, route guard |
| **3** | CI (build + test + arch test) | Refresh token, role/policy | `IFileStorage`, MinIO, presigned upload | Reservation + job nhả vé | Danh sách và chi tiết sự kiện |
| **4** | **Mua domain, Cloudflare, VPS, Origin CA, R2** | Test Identity | Test Events, tìm kiếm/lọc | Test đồng thời chống bán vượt | Client OpenAPI tự sinh, TanStack Query |
| **5** | Dockerfile prod, compose prod, push GHCR, **deploy tay lần đầu** | Hangfire, hạ tầng email | Upload ảnh lên R2 | Order + tích hợp VNPay sandbox | Giỏ vé, đếm ngược giữ vé |
| **6** | CD tự động, healthcheck, rollback | Module Notifications (email vé) | Báo cáo doanh thu (API) | Webhook idempotent, Order → Paid | Trang checkout, kết quả thanh toán |
| **7** | Backup DB, Serilog, giám sát | Sự kiện `OrderPaid` → email QR | UI quản lý sự kiện (organizer) | Phát hành Ticket, QR ký HMAC | "Vé của tôi", hiển thị QR |
| **8** | Rate limit, bảo mật (header, CORS) | UI quản lý người dùng, phân quyền | UI dashboard báo cáo | UI quản lý hạng vé, đơn hàng | Module CheckIn + trang quét QR |
| **9** | Test tải (k6) | Hoàn thiện Notifications | Tối ưu truy vấn, cache | Mã giảm giá | Responsive, xử lý lỗi, loading |
| **10** | Mở rộng: SignalR (nếu còn thời gian) | Test tích hợp | Test tích hợp | Test tích hợp luồng mua vé | Playwright e2e luồng chính |
| **11** | Tài liệu kiến trúc, ADR, sơ đồ | Tài liệu module | Tài liệu module | Tài liệu luồng thanh toán | Tài liệu frontend, kịch bản demo |
| **12** | Đóng băng code, demo thử, sửa lỗi | Sửa lỗi | Sửa lỗi | Sửa lỗi | Sửa lỗi, chuẩn bị slide |

**Mốc kiểm soát:**

| Mốc | Điều kiện |
|---|---|
| Cuối tuần 3 | Có endpoint `/health`, CI chạy xanh |
| Cuối tuần 5 | **App chạy trên domain thật, có HTTPS**, deploy tay thành công |
| Cuối tuần 6 | Push lên `main` tự deploy |
| Cuối tuần 8 | Luồng mua vé end-to-end chạy được trên môi trường prod |
| Cuối tuần 10 | Đóng băng tính năng |

### 8.3 Ma trận trách nhiệm (R = chịu trách nhiệm chính, C = tham gia ý kiến)

| Hạng mục | TV1 | TV2 | TV3 | TV4 | TV5 |
|---|:-:|:-:|:-:|:-:|:-:|
| Kiến trúc, ranh giới module | R | C | C | C | C |
| Identity | C | R | | | C |
| Events, Storage | C | | R | | C |
| Ticketing, Orders, thanh toán | C | | | R | C |
| Notifications | | R | | C | |
| CheckIn | C | | | C | R |
| Frontend nền tảng | | | | | R |
| Docker, CI/CD, VPS, Cloudflare | R | | | | |
| Tài liệu, báo cáo | R | C | C | C | C |

---

## 9. Quy trình làm việc nhóm

### 9.1 Git

- Nhánh: `main` (luôn deploy được), `feature/<module>-<mô-tả>`, `fix/<mô-tả>`.
- **Cấm push trực tiếp vào `main`**; bật branch protection: cần 1 review + CI xanh.
- Commit theo [Conventional Commits](https://www.conventionalcommits.org/): `feat(ticketing): add reservation expiry job`.
- Pull Request nhỏ, một mục đích; TV1 hoặc chủ module review.

### 9.2 Quy ước code

| Hạng mục | Quy ước |
|---|---|
| C# | `.editorconfig` chung, bật nullable, analyzer, `TreatWarningsAsErrors` |
| TypeScript | `strict: true`, ESLint + Prettier, không dùng `any` |
| Đặt tên endpoint | `/api/{module}/{resource}` (ví dụ `/api/events/{id}`) |
| Lỗi API | Chuẩn `ProblemDetails` (RFC 9457) |
| Tiền tệ | `decimal` + lưu đơn vị nhỏ nhất, đi kèm mã tiền tệ |
| Thời gian | Lưu UTC, hiển thị theo múi giờ người dùng |

### 9.3 Definition of Done

- [ ] Code qua review, CI xanh (build, test, arch test, lint)
- [ ] Có unit test cho logic nghiệp vụ, integration test cho luồng chính
- [ ] Endpoint mới xuất hiện trong OpenAPI, frontend sinh lại được type
- [ ] Migration đi kèm, không phá dữ liệu cũ
- [ ] Cập nhật tài liệu nếu thay đổi hành vi hoặc cấu hình

### 9.4 Giao tiếp

- Họp đứng 15 phút, 2 lần/tuần; demo nội bộ cuối mỗi tuần.
- Dùng GitHub Projects (Kanban) để theo dõi task; mỗi task gắn nhãn module.
- Quyết định kiến trúc quan trọng ghi thành ADR trong `docs/adr/`.

---

## 10. Codebase tham khảo

> Chỉ **đọc để học ý tưởng thiết kế**, không sao chép code. Các dự án này dùng giấy phép khác nhau (GPL, AGPL, Apache...). Sao chép code có thể buộc dự án của bạn phải theo cùng giấy phép và vi phạm quy định đạo văn của môn học. Hãy kiểm tra file `LICENSE` của từng repo.

| Dự án | Công nghệ | Nên học gì |
|---|---|---|
| **Alf.io** (Java) | Java, Spring Boot, PostgreSQL | Cách xử lý **giữ chỗ/đặt vé và hết hạn đặt chỗ**, mô hình dữ liệu vé và hạng vé, quy trình check-in bằng QR, cách xử lý đồng thời khi nhiều người mua cùng lúc |
| **Pretix** (Python + JS) | Django, Celery, PostgreSQL | Mô hình **kiến trúc plugin** (gần với tư duy module), phân tách Event/Organizer/Order/Item, cách xử lý quota và giỏ hàng, API và webhook, nhật ký hoạt động (audit log) |
| **Hi.Events** (PHP + React) | Laravel, React, TypeScript | Cách tổ chức **frontend React + TypeScript tách riêng**, giao diện tạo sự kiện và dashboard cho ban tổ chức, mô hình đơn hàng và vé |
| **Attendize** (PHP) | Laravel, MySQL | Luồng tạo sự kiện đơn giản, trang mua vé công khai, quản lý người tham dự và check-in |

### 10.1 Cách đọc hiệu quả

Đừng đọc từ đầu đến cuối. Với mỗi repo, tìm trả lời các câu hỏi sau rồi ghi vào `docs/references.md`:

1. **Mô hình dữ liệu:** các bảng Event, Ticket/Item, Order, Reservation/Cart liên hệ với nhau thế nào?
2. **Chống bán vượt:** họ khóa hoặc kiểm tra tồn kho ở đâu và bằng cơ chế gì?
3. **Hết hạn giữ vé:** job nền hay kiểm tra lúc truy cập?
4. **Webhook thanh toán:** xử lý trùng lặp và thất bại ra sao?
5. **Check-in:** mã QR chứa gì, xác thực thế nào, chặn quét hai lần ra sao?
6. **Phân quyền:** ban tổ chức nhiều tài khoản được phân vai thế nào?

### 10.2 Chia việc cho từng thành viên

| Thành viên | Vai trò | Việc chính | Bàn giao |
|---|---|---|---|
| **TV1** | Tech Lead + DevOps | Khởi tạo repo và BuildingBlocks (Outbox, EventBus); Architecture Test; Docker Compose; domain, Cloudflare, VPS, R2; CI/CD, backup; review PR | Hệ thống online có HTTPS, push `main` tự deploy |
| **TV2** | Backend Identity + Notifications | Đăng ký/đăng nhập, JWT, refresh token, phân quyền; Hangfire; gửi email vé QR khi `OrderPaid`; UI quản lý người dùng | Đăng nhập/phân quyền toàn hệ thống, email vé tự động |
| **TV3** | Backend Events + Storage | CRUD sự kiện, duyệt sự kiện; tìm kiếm/lọc; `IFileStorage` (MinIO/R2), upload presigned; API báo cáo; UI tạo sự kiện, dashboard | Tạo sự kiện có ảnh trên R2, có báo cáo |
| **TV4** | Backend Ticketing + Orders | Hạng vé, tồn kho chống bán vượt; Reservation và job nhả vé; Order, VNPay sandbox, webhook idempotent; phát hành vé QR; UI quản lý vé, đơn hàng | Luồng giữ vé → thanh toán → phát vé chạy đúng |
| **TV5** | Frontend Lead + CheckIn | Khung React + TS, client sinh từ OpenAPI; trang khách hàng (sự kiện, checkout, vé của tôi); module CheckIn và trang quét QR | Đi hết luồng mua vé; check-in quét QR được |

**Việc chung:** mỗi người viết ERD, test và tài liệu cho module mình; review chéo PR; cùng chuẩn bị demo và báo cáo.

---

## 11. Rủi ro và cách giảm thiểu

| Rủi ro | Mức độ | Giảm thiểu |
|---|---|---|
| Nameserver `.id.vn` chuyển sang Cloudflare chậm | Trung bình | Làm sớm từ tuần 4, có thể mất vài giờ đến 1-2 ngày |
| R2 yêu cầu thêm phương thức thanh toán để kích hoạt | Trung bình | Kiểm tra sớm; dự phòng bằng MinIO trên VPS hoặc bucket S3 khác |
| Bán vượt vé khi nhiều người mua cùng lúc | Cao | Test đồng thời bắt buộc, dùng điều kiện `WHERE available >= qty` |
| Webhook thanh toán bị gọi lặp | Cao | Khóa idempotency theo mã giao dịch, ràng buộc unique |
| Dồn rủi ro hạ tầng về cuối kỳ | Cao | Mốc cứng: chạy trên domain thật vào cuối tuần 5 |
| VPS hết RAM (build, Postgres, .NET) | Trung bình | Build image trên GitHub Actions (không build trên VPS), thêm swap, VPS tối thiểu 2GB |
| Module phụ thuộc chéo, vỡ kiến trúc | Trung bình | Architecture Test trong CI, review kỹ PR |
| Lộ secret lên git | Cao | `.gitignore`, bật secret scanning của GitHub, xoay key nếu lỡ commit |
| Cloudflare giới hạn upload 100MB/request | Thấp | Upload thẳng lên R2 bằng presigned URL |
| Xung đột merge giữa các thành viên | Trung bình | Mỗi người sở hữu module riêng, PR nhỏ, rebase thường xuyên |

---

## 12. Checklist nghiệm thu

**Kiến trúc**
- [ ] Có 6 module, mỗi module có đủ Domain/Application/Infrastructure/Contracts/Api
- [ ] Architecture Test chạy trong CI và đang xanh
- [ ] Mỗi module có schema DB riêng, không truy vấn chéo
- [ ] Giao tiếp liên module qua Contracts và Integration Event (Outbox)

**Docker và lưu trữ**
- [ ] `docker compose up` chạy được toàn bộ hệ thống ở local
- [ ] Dev dùng MinIO, prod dùng R2, cùng một interface `IFileStorage`
- [ ] Ảnh sự kiện thực sự nằm trên R2

**Triển khai**
- [ ] `https://tenmien.id.vn` hiển thị giao diện, chứng chỉ hợp lệ
- [ ] `https://api.tenmien.id.vn` truy cập được, `/health` trả về OK
- [ ] Cloudflare ở chế độ Full (strict), dùng Origin CA
- [ ] Cổng Postgres/Redis không mở ra ngoài, 80/443 chỉ nhận từ Cloudflare
- [ ] Image lưu trên GHCR, tag theo commit SHA

**CI/CD**
- [ ] PR chạy CI (build, test, arch test, lint)
- [ ] Merge vào `main` tự động deploy
- [ ] Demo được rollback về bản trước
- [ ] Có backup DB lên R2

**Nghiệp vụ**
- [ ] Mua vé đầu-cuối: chọn vé → giữ vé → thanh toán sandbox → nhận email QR
- [ ] Giữ vé hết hạn thì tồn kho được trả lại
- [ ] Webhook gọi lặp không tạo vé trùng
- [ ] Check-in quét QR hợp lệ, quét lần hai bị từ chối
- [ ] Ban tổ chức xem được báo cáo doanh thu

**Tài liệu**
- [ ] README hướng dẫn chạy local và deploy
- [ ] Sơ đồ kiến trúc, ERD từng module, sơ đồ luồng mua vé
- [ ] ADR cho các quyết định chính (Modular Monolith, Outbox, R2...)
- [ ] `docs/references.md` ghi lại những gì học được từ Alf.io, Pretix, Hi.Events, Attendize

# Website Bán Hàng Lưu Niệm - Shop Lưu Niệm Banh Cute

## Giới thiệu

Đây là dự án **Website Bán Hàng Lưu Niệm** được phát triển bằng C# (ASP.NET Core) trong khuôn khổ bài tập lớn môn học. Website cho phép người dùng xem, tìm kiếm, và đặt mua các sản phẩm lưu niệm dễ thương từ **Shop Lưu Niệm Bánh Cute**. Ngoài ra, website cung cấp giao diện quản trị cho admin để quản lý sản phẩm, đơn hàng, và người dùng.

- **Mã số sinh viên**: 2180607089
- **Tên sinh viên**: Trương Văn Bảo Anh

## Tính năng chính

### Dành cho người dùng
- **Xem và tìm kiếm sản phẩm**: Xem danh sách sản phẩm, chi tiết sản phẩm, và tìm kiếm sản phẩm theo tên hoặc danh mục.
- **Đặt hàng trực tuyến**: Thêm sản phẩm vào giỏ hàng, nhập thông tin giao hàng, và thanh toán qua mã QR.
- **Quản lý đơn hàng**: Xem lịch sử đơn hàng và trạng thái đơn hàng (Đang xử lý, Đã thanh toán, Đã hủy, v.v.).
- **Đăng ký và đăng nhập**: Đăng ký tài khoản để trở thành thành viên, đăng nhập để quản lý đơn hàng.
- **Hỗ trợ đa ngôn ngữ**: Hiển thị nội dung bằng Tiếng Việt và Tiếng Anh (đang phát triển).

### Dành cho quản trị viên
- **Quản lý sản phẩm**: Thêm, sửa, xóa sản phẩm và danh mục sản phẩm.
- **Quản lý đơn hàng**: Xem danh sách đơn hàng, tìm kiếm theo trạng thái, xác nhận đơn hàng, và chỉnh sửa thông tin.
- **Thống kê**: Xem số lượng sản phẩm đã bán, tổng doanh thu, và tình hình đặt hàng theo danh mục.
- **Quản lý người dùng**: Xem danh sách người dùng, chỉnh sửa hoặc xóa tài khoản.

### Lưu trữ và quản lý dữ liệu
- Sử dụng **SQL Server** để lưu trữ thông tin sản phẩm, đơn hàng, và người dùng.
- Hỗ trợ migration để quản lý cấu trúc database.

## Công nghệ sử dụng

- **Backend**: C# (ASP.NET Core)
- **Frontend**: HTML, CSS, JavaScript
- **Database**: SQL Server
- **Công cụ quản lý mã nguồn**: Git, GitHub
- **Thư viện hỗ trợ**:
  - Entity Framework Core (cho database)
  - Bootstrap (cho giao diện)
  - jQuery (cho JavaScript)
  - SweetAlert2 (cho thông báo)

## Hướng dẫn cài đặt và chạy dự án

### Yêu cầu
- **.NET SDK**: Phiên bản 6.0 hoặc cao hơn
- **SQL Server**: SQL Server 2019 hoặc cao hơn
- **SQL Server Management Studio (SSMS)**: Để quản lý database
- **Visual Studio**: Phiên bản 2022 (hoặc các IDE hỗ trợ C# như Rider)

### Các bước cài đặt
1. **Clone repository**:
   ```bash
   git clone https://github.com/BanhCute/Website_Ban_Hang_C-.git
   cd Website_Ban_Hang_C-

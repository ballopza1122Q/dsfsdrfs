# Realtime Database mới · R14.2

Menu dùng `https://dsfdsfsdf-13cbb-default-rtdb.firebaseio.com`, đường dẫn
`licensesV4/<SHA256-key>.json`, và `/binding.json` cho lần gắn thiết bị đầu.
Không nhúng apiKey/appId/measurementId của config web vì luồng REST này không dùng chúng.

Đã kiểm tra đọc một hash giả toàn số 0: HTTP 200 và `null`. Không liệt kê dữ liệu,
không thử key thật và không ghi vào Firebase. Đã đổi trang quản trị V4 trong
`iOSViet/firebase/index.html`, bản sao `firebase/index.html` của dự án proxy, cùng
cấu hình trang quản trị proxy/bot sang project mới. Chưa chuyển các key hoặc chỉnh
Rules; record key hợp lệ phải có trên project mới.
Các record vẫn theo protocol 4 với enabled, expiresAt và binding như trước.

Trước khi gửi và khi xử lý phản hồi, client kiểm tra URL đúng HTTPS origin, key hiện
tại và đúng một trong hai đường dẫn. Origin có fingerprint riêng để việc chỉ đổi
literal endpoint không tự vượt kiểm tra. Đây là ràng buộc phía client, không phải
TLS certificate pinning hoặc xác minh chữ ký phản hồi. Proxy giả nội dung ở cùng URL,
hoặc người sửa cả verifier/fingerprint/nhánh cấp quyền, vẫn có thể vượt.

URL và fingerprint được làm rối trong bảng chuỗi bytes; phải giải mã khi chạy nên
không thể bảo đảm giữ bí mật trước runtime dump hay trình phân tích. Metadata native
và câu thông báo bản quyền theo yêu cầu vẫn đọc được. Source và debug map giữ riêng.

Theo tài liệu Firebase, quyền truy cập được kiểm soát bằng Security Rules; giữ kín
web config/API key không thay thế cơ chế đó:
https://firebase.google.com/docs/projects/api-keys
https://firebase.google.com/docs/database/security

Muốn chống giả nội dung phản hồi cần thêm thiết kế cấp quyền có chữ ký và triển khai
phía server. R14.2 chưa triển khai thay đổi đó và không tuyên bố chống crack tuyệt đối.

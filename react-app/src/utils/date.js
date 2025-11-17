// ==============================
// 📅 Tạo lưới lịch tháng
// ==============================
export function getMonthGrid(date = new Date()) {
  const year = date.getFullYear();
  const month = date.getMonth(); // 0-based
  const currentDate = date.getDate();
  const monthName = date.toLocaleString("default", { month: "long" });

  const firstDay = new Date(year, month, 1).getDay();
  const daysInMonth = new Date(year, month + 1, 0).getDate();

  const startOffset = firstDay === 0 ? 6 : firstDay - 1;

  const days = [
    ...Array.from({ length: startOffset }, () => null),
    ...Array.from({ length: daysInMonth }, (_, i) => i + 1),
  ];

  const weeks = [];
  for (let i = 0; i < days.length; i += 7) {
    weeks.push(days.slice(i, i + 7));
  }

  return { year, month, monthName, currentDate, weeks };
}

// ==============================
// ==============================
export function formatTimeVietnam(dateInput) {
  if (!dateInput) return "";

  // Xử lý giống như formatFullDateVietnam để đảm bảo consistency
  let dateStr = String(dateInput).trim();
  // Nếu không có ký tự Z hoặc offset, ép coi là UTC
  if (!/[zZ]|[+\-]\d{2}:?\d{2}$/.test(dateStr)) {
    dateStr += "Z";
  }

  const date = new Date(dateStr);
  if (isNaN(date.getTime())) return "";

  const now = new Date();
  const diffInMinutes = Math.floor((now - date) / (1000 * 60));

  if (diffInMinutes < 1) return "Just now";
  if (diffInMinutes < 60) return `${diffInMinutes} min ago`;

  const diffInHours = Math.floor(diffInMinutes / 60);
  if (diffInHours < 24)
    return `${diffInHours} hour${diffInHours > 1 ? "s" : ""} ago`;

  const diffInDays = Math.floor(diffInHours / 24);
  if (diffInDays < 7)
    return `${diffInDays} day${diffInDays > 1 ? "s" : ""} ago`;

  // Hiển thị ngày, giờ đúng theo múi giờ Việt Nam
  return date.toLocaleString("en-US", {
    year: "numeric",
    month: "long",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    timeZone: "Asia/Ho_Chi_Minh",
  });
}

// ==============================
// 📆 Format đầy đủ ngày giờ Việt Nam
// ==============================
export function formatFullDateVietnam(dateInput) {
  if (!dateInput) return "";

  let dateStr = String(dateInput).trim();
  // Nếu không có ký tự Z hoặc offset, ép coi là UTC
  if (!/[zZ]|[+\-]\d{2}:?\d{2}$/.test(dateStr)) {
    dateStr += "Z";
  }

  const date = new Date(dateStr);
  if (isNaN(date.getTime())) return "";

  return date.toLocaleString("en-US", {
    year: "numeric",
    month: "long",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    timeZone: "Asia/Ho_Chi_Minh",
  });
}

// ==============================
// 🔍 Kiểm tra ngày đã submit chưa
// ==============================


export function getTodayVietnamISO() {
  const now = new Date();
  // Lấy đúng ngày hiện tại theo local machine (máy người dùng)
  const yyyy = now.getFullYear();
  const mm = String(now.getMonth() + 1).padStart(2, "0");
  const dd = String(now.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
}
export function calculateDuration(startedAt, submittedAt) {
  if (!startedAt || !submittedAt) return "N/A";

  const start = new Date(startedAt);
  const end = new Date(submittedAt);
  if (isNaN(start.getTime()) || isNaN(end.getTime())) return "N/A";

  const diffMs = end - start; // milliseconds
  if (diffMs < 0) return "N/A"; // phòng lỗi dữ liệu

  const totalSeconds = Math.floor(diffMs / 1000);
  const mins = Math.floor(totalSeconds / 60);
  const secs = totalSeconds % 60;

  // format đẹp
  if (mins === 0) return `${secs}s`;
  if (mins < 60) return `${mins}m ${secs}s`;

  const hours = Math.floor(mins / 60);
  const remainMins = mins % 60;
  return `${hours}h ${remainMins}m ${secs}s`;
}
// ==============================
// 🕒 Hiển thị thời gian tương đối (ví dụ: "2 hours ago")
// ==============================
export function formatRelativeTime(dateInput) {
  if (!dateInput) return "";

  let dateStr = String(dateInput).trim();
  if (!/[zZ]|[+\-]\d{2}:?\d{2}$/.test(dateStr)) {
    dateStr += "Z"; // ép kiểu UTC nếu không có timezone
  }

  const date = new Date(dateStr);
  if (isNaN(date.getTime())) return "";

  const now = new Date();
  const diffMs = now - date;

  const diffSeconds = Math.floor(diffMs / 1000);
  const diffMinutes = Math.floor(diffSeconds / 60);
  const diffHours = Math.floor(diffMinutes / 60);
  const diffDays = Math.floor(diffHours / 24);
  const diffWeeks = Math.floor(diffDays / 7);

  if (diffSeconds < 60) return "Just now";
  if (diffMinutes < 60)
    return `${diffMinutes} minute${diffMinutes > 1 ? "s" : ""} ago`;
  if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? "s" : ""} ago`;
  if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? "s" : ""} ago`;
  if (diffWeeks < 5) return `${diffWeeks} week${diffWeeks > 1 ? "s" : ""} ago`;

  // Nếu quá xa thì hiển thị ngày chuẩn Việt Nam
  return date.toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

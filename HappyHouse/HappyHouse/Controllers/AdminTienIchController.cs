using HappyHouse.Models;
using PagedList;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class AdminTienIchController : AdminBaseController
    {
        private readonly TienIchBusiness _bus = new TienIchBusiness();

        private void LoadDropdown(string coTinhTien = null,
                                  string trangThai = null)
        {
            ViewBag.DsCoTinhTien = new SelectList(new[]
            {
                new { Value = "",  Text = "--- Tất cả ---"      },
                new { Value = "0", Text = "Hiển thị (miễn phí)" },
                new { Value = "1", Text = "Tính tiền hàng tháng" },
            }, "Value", "Text", coTinhTien);

            ViewBag.DsTrangThai = new SelectList(new[]
            {
                new { Value = "",  Text = "--- Tất cả ---" },
                new { Value = "1", Text = "Đang hoạt động" },
                new { Value = "0", Text = "Đã tắt"         },
            }, "Value", "Text", trangThai);
        }

        // ── DANH SÁCH ────────────────────────────────────────────────

        [HttpGet]
        public ActionResult DanhSach(int page = 1,
                                     string tuKhoa = null,
                                     string coTinhTien = null,
                                     string trangThai = null)
        {
            return HienThiDanhSach(page, tuKhoa, coTinhTien, trangThai);
        }

        [HttpPost]
        public ActionResult DanhSach(string tuKhoa,
                                     string coTinhTien,
                                     string trangThai,
                                     int page = 1)
        {
            return HienThiDanhSach(page, tuKhoa, coTinhTien, trangThai);
        }

        private ActionResult HienThiDanhSach(int page,
                                              string tuKhoa,
                                              string coTinhTien,
                                              string trangThai)
        {
            var lst = _bus.LayDanhSach(tuKhoa, coTinhTien, trangThai);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.CoTinhTien = coTinhTien;
            ViewBag.TrangThai = trangThai;
            LoadDropdown(coTinhTien, trangThai);

            return View(lst.ToPagedList(page, 15));
        }

        // ── THÊM MỚI ─────────────────────────────────────────────────

        [HttpGet]
        public ActionResult ThemMoi()
        {
            return View(new TienIch { CoTinhTien = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemMoi(TienIch obj)
        {
            if (string.IsNullOrWhiteSpace(obj.TenTienIch))
                ModelState.AddModelError("TenTienIch",
                    "Tên tiện ích không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.BieuTuong))
                ModelState.AddModelError("BieuTuong",
                    "Vui lòng nhập class icon Font Awesome!");

            if (!ModelState.IsValid)
                return View(obj);

            bool kq = _bus.ThemMoi(obj);

            if (kq)
            {
                TempData["Success"] = "Thêm tiện ích thành công!";
                return RedirectToAction("DanhSach");
            }

            ModelState.AddModelError("TenTienIch",
                "Tên tiện ích này đã tồn tại!");
            return View(obj);
        }

        // ── SỬA THÔNG TIN ─────────────────────────────────────────────

        [HttpGet]
        public ActionResult SuaThongTin(string maTienIch)
        {
            var obj = _bus.LayChiTiet(maTienIch);
            if (obj == null) return HttpNotFound();

            ViewBag.SoPhongDung = _bus.DemSoPhongDung(maTienIch);
            ViewBag.SoDichVuDung = _bus.DemSoDichVuDung(maTienIch);
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaThongTin(TienIch obj)
        {
            if (string.IsNullOrWhiteSpace(obj.TenTienIch))
                ModelState.AddModelError("TenTienIch",
                    "Tên tiện ích không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.BieuTuong))
                ModelState.AddModelError("BieuTuong",
                    "Vui lòng nhập class icon Font Awesome!");

            if (!ModelState.IsValid)
            {
                ViewBag.SoPhongDung = _bus.DemSoPhongDung(obj.MaTienIch);
                ViewBag.SoDichVuDung = _bus.DemSoDichVuDung(obj.MaTienIch);
                return View(obj);
            }

            bool kq = _bus.CapNhat(obj);

            if (kq)
            {
                TempData["Success"] = "Cập nhật tiện ích thành công!";
                return RedirectToAction("DanhSach");
            }

            ModelState.AddModelError("TenTienIch",
                "Tên này đã được dùng bởi tiện ích khác!");
            ViewBag.SoPhongDung = _bus.DemSoPhongDung(obj.MaTienIch);
            ViewBag.SoDichVuDung = _bus.DemSoDichVuDung(obj.MaTienIch);
            return View(obj);
        }

        // ── ĐỔI TRẠNG THÁI ───────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoiTrangThai(string maTienIch)
        {
            bool kq = _bus.DoiTrangThai(maTienIch);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Cập nhật trạng thái thành công."
                : "Thao tác thất bại!";

            return RedirectToAction("DanhSach");
        }

        // ── XÓA ──────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Xoa(string maTienIch)
        {
            bool kq = _bus.Xoa(maTienIch);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã xóa tiện ích."
                : "Không thể xóa! Tiện ích đang được sử dụng ở phòng hoặc dịch vụ.";

            return RedirectToAction("DanhSach");
        }
    }
}
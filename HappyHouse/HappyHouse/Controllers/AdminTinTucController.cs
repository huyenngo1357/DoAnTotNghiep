using HappyHouse.Models;
using PagedList;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;

namespace HappyHouse.Controllers
{
    public class AdminTinTucController : AdminBaseController
    {
        private readonly TinTucBusiness _bus =
            new TinTucBusiness();
        private readonly ChuDeTinTucBusiness _chuDeBus =
            new ChuDeTinTucBusiness();

        private void LoadDropdown(string maChuDe = null,
                                   string trangThaiDang = null)
        {
            ViewBag.DsChuDe = new SelectList(
                _chuDeBus.LayDanhSachHoatDong(),
                "MaChuDe", "TenChuDe", maChuDe);

            var dsTrangThai = new[]
            {
                new { Value = "",
                      Text  = "--- Tất cả ---"  },
                new { Value = "ChuaDang",
                      Text  = "Chưa đăng"       },
                new { Value = "DaDang",
                      Text  = "Đã đăng"         },
                new { Value = "TamAn",
                      Text  = "Tạm ẩn"          },
            };
            ViewBag.DsTrangThai = new SelectList(
                dsTrangThai, "Value", "Text",
                trangThaiDang);
        }

        // Helper: convert plain-text input into safe HTML OR accept already-HTML input as-is.
        // - If input contains HTML tags => assume author used rich editor → return trimmed HTML (no re-encoding).
        // - Otherwise HTML-encode and convert newlines to paragraphs/<br />.
        private string ConvertPlainTextToHtml(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // If input already contains HTML tags, treat it as HTML (do NOT HtmlEncode).
            if (Regex.IsMatch(input, "<[^>]+>"))
            {
                return input.Trim();
            }

            // Encode HTML special chars for plain text, then convert newlines
            var encoded = HttpUtility.HtmlEncode(input);

            // Normalize new lines to '\n'
            encoded = encoded.Replace("\r\n", "\n").Replace("\r", "\n");

            // Split paragraphs on empty line(s) (double newline)
            var paragraphs = encoded
                .Split(new[] { "\n\n" }, StringSplitOptions.None)
                .Select(p => "<p>" + p.Replace("\n", "<br />") + "</p>");

            return string.Join("", paragraphs);
        }

        // DANH SÁCH

        [HttpGet]
        public ActionResult DanhSach(int page = 1,
                                      string tuKhoa = null,
                                      string maChuDe = null,
                                      string trangThaiDang = null)
        {
            return HienThiDanhSach(
                page, tuKhoa, maChuDe, trangThaiDang);
        }

        [HttpPost]
        public ActionResult DanhSach(string tuKhoa,
                                      string maChuDe,
                                      string trangThaiDang,
                                      int page = 1)
        {
            return HienThiDanhSach(
                page, tuKhoa, maChuDe, trangThaiDang);
        }

        private ActionResult HienThiDanhSach(
            int page,
            string tuKhoa,
            string maChuDe,
            string trangThaiDang)
        {
            var lst = _bus.LayDanhSach(
                tuKhoa, maChuDe, trangThaiDang);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaChuDe = maChuDe;
            ViewBag.TrangThaiDang = trangThaiDang;
            LoadDropdown(maChuDe, trangThaiDang);

            return View(lst.ToPagedList(page, 10));
        }

        // THÊM MỚI

        [HttpGet]
        public ActionResult ThemMoi()
        {
            LoadDropdown();
            return View(new TinTuc());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]  // Cho phép HTML trong NoiDung (we will encode/transform)
        public ActionResult ThemMoi(
            TinTuc obj,
            HttpPostedFileBase anhDaiDien)
        {
            if (string.IsNullOrWhiteSpace(obj.TieuDe))
                ModelState.AddModelError("TieuDe",
                    "Tiêu đề không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.MaChuDe))
                ModelState.AddModelError("MaChuDe",
                    "Vui lòng chọn chủ đề!");

            if (string.IsNullOrWhiteSpace(obj.NoiDung))
                ModelState.AddModelError("NoiDung",
                    "Nội dung không được để trống!");

            if (!ModelState.IsValid)
            {
                LoadDropdown(obj.MaChuDe);
                return View(obj);
            }

            var admin = GetUserOnline();
            obj.MaNguoiDang = admin.MaNguoiDung;

            // Convert plain text to safe HTML preserving user formatting
            obj.NoiDung = ConvertPlainTextToHtml(obj.NoiDung);

            bool kq = _bus.ThemMoi(obj, anhDaiDien);
            if (kq)
            {
                TempData["Success"] =
                    "Thêm tin tức thành công!";
                return RedirectToAction("DanhSach");
            }

            TempData["Error"] =
                "Thêm thất bại, vui lòng thử lại!";
            LoadDropdown(obj.MaChuDe);
            return View(obj);
        }

        // SỬA

        [HttpGet]
        public ActionResult SuaThongTin(string maTinTuc)
        {
            var obj = _bus.LayChiTiet(maTinTuc);
            if (obj == null) return HttpNotFound();

            LoadDropdown(obj.MaChuDe);
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult SuaThongTin(
            TinTuc obj,
            HttpPostedFileBase anhDaiDien)
        {
            if (string.IsNullOrWhiteSpace(obj.TieuDe))
                ModelState.AddModelError("TieuDe",
                    "Tiêu đề không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.MaChuDe))
                ModelState.AddModelError("MaChuDe",
                    "Vui lòng chọn chủ đề!");

            if (string.IsNullOrWhiteSpace(obj.NoiDung))
                ModelState.AddModelError("NoiDung",
                    "Nội dung không được để trống!");

            if (!ModelState.IsValid)
            {
                LoadDropdown(obj.MaChuDe);
                return View(_bus.LayChiTiet(obj.MaTinTuc));
            }

            // Convert plain text to safe HTML preserving user formatting
            obj.NoiDung = ConvertPlainTextToHtml(obj.NoiDung);

            bool kq = _bus.CapNhat(obj, anhDaiDien);
            if (kq)
            {
                TempData["Success"] =
                    "Cập nhật tin tức thành công!";
                return RedirectToAction("DanhSach");
            }

            TempData["Error"] = "Cập nhật thất bại!";
            LoadDropdown(obj.MaChuDe);
            return View(_bus.LayChiTiet(obj.MaTinTuc));
        }

        // ĐĂNG BÀI 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangBai(string maTinTuc)
        {
            bool kq = _bus.DangBai(maTinTuc);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã đăng bài thành công."
                : "Thao tác thất bại hoặc bài đã đăng.";

            return RedirectToAction("DanhSach");
        }

        // TẠM ẨN 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TamAn(string maTinTuc)
        {
            bool kq = _bus.TamAn(maTinTuc);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã tạm ẩn bài viết."
                : "Thao tác thất bại!";

            return RedirectToAction("DanhSach");
        }

        // XÓA

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Xoa(string maTinTuc)
        {
            bool kq = _bus.Xoa(maTinTuc);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã xóa tin tức."
                : "Xóa thất bại!";

            return RedirectToAction("DanhSach");
        }
    }
}
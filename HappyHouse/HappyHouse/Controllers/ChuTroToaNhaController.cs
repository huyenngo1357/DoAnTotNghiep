using HappyHouse.Models;
using PagedList;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class ChuTroToaNhaController : ChuTroBaseController
    {
        private readonly ToaNhaBusiness _bus = new ToaNhaBusiness();

        private void LoadDropdown(string trangThaiDuyet = null)
        {
            var lst = new[]
            {
                new { Value = "",          Text = "--- Tất cả ---" },
                new { Value = "ChoDuyet",  Text = "Chờ duyệt"     },
                new { Value = "DaDuyet",   Text = "Đã duyệt"      },
                new { Value = "TuChoi",    Text = "Từ chối"        },
                new { Value = "TamNgung",  Text = "Tạm ngưng"     },
            };
            ViewBag.DsTrangThai = new SelectList(lst, "Value", "Text", trangThaiDuyet);
        }

        // ── DANH SÁCH ────────────────────────────────────────────────

        [HttpGet]
        public ActionResult DanhSach(int page = 1,
                                     string tuKhoa = null,
                                     string trangThaiDuyet = null)
        {
            return HienThiDanhSach(page, tuKhoa, trangThaiDuyet);
        }

        [HttpPost]
        public ActionResult DanhSach(string tuKhoa,
                                     string trangThaiDuyet,
                                     int page = 1)
        {
            return HienThiDanhSach(page, tuKhoa, trangThaiDuyet);
        }

        private ActionResult HienThiDanhSach(int page,
                                              string tuKhoa,
                                              string trangThaiDuyet)
        {
            var user = GetUserOnline();
            var lst = _bus.LayDanhSachChuTro(user.MaNguoiDung, tuKhoa, trangThaiDuyet);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.TrangThaiDuyet = trangThaiDuyet;
            LoadDropdown(trangThaiDuyet);

            return View(lst.ToPagedList(page, 10));
        }

        // ── CHI TIẾT ─────────────────────────────────────────────────

        public ActionResult ChiTiet(string maToaNha)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maToaNha);

            if (obj == null) return HttpNotFound();
            if (obj.MaChuTro != user.MaNguoiDung) return HttpNotFound();

            return View(obj);
        }

        // ── THÊM MỚI ─────────────────────────────────────────────────

        [HttpGet]
        public ActionResult ThemMoi()
        {
            return View(new ToaNha());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemMoi(ToaNha obj,
                                    List<HttpPostedFileBase> dsHinhAnh)
        {
            if (string.IsNullOrWhiteSpace(obj.TenToaNha))
                ModelState.AddModelError("TenToaNha", "Tên tòa nhà không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.DiaChi))
                ModelState.AddModelError("DiaChi", "Địa chỉ không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.TinhThanh))
                ModelState.AddModelError("TinhThanh", "Tỉnh/Thành không được để trống!");

            if (!ModelState.IsValid)
                return View(obj);

            var user = GetUserOnline();
            obj.MaToaNha = _bus.SinhMa();
            obj.MaChuTro = user.MaNguoiDung;

            _bus.ThemMoi(obj, dsHinhAnh);

            TempData["Success"] = "Thêm tòa nhà thành công! Vui lòng chờ admin duyệt.";
            return RedirectToAction("DanhSach");
        }

        // ── SỬA THÔNG TIN ─────────────────────────────────────────────

        [HttpGet]
        public ActionResult SuaThongTin(string maToaNha)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maToaNha);

            if (obj == null) return HttpNotFound();
            if (obj.MaChuTro != user.MaNguoiDung) return HttpNotFound();

            // Chỉ cho sửa khi TuChoi hoặc ChoDuyet
            if (obj.TrangThaiDuyet == "DaDuyet" || obj.TrangThaiDuyet == "TamNgung")
            {
                TempData["Error"] = "Không thể chỉnh sửa tòa nhà đang hoạt động. " +
                                    "Vui lòng liên hệ admin để tạm ngưng trước.";
                return RedirectToAction("DanhSach");
            }

            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaThongTin(ToaNha obj,
                                        List<HttpPostedFileBase> dsHinhAnh)
        {
            var user = GetUserOnline();
            var objDb = _bus.LayChiTiet(obj.MaToaNha);

            if (objDb == null) return HttpNotFound();
            if (objDb.MaChuTro != user.MaNguoiDung) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(obj.TenToaNha))
                ModelState.AddModelError("TenToaNha", "Tên tòa nhà không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.DiaChi))
                ModelState.AddModelError("DiaChi", "Địa chỉ không được để trống!");

            if (string.IsNullOrWhiteSpace(obj.TinhThanh))
                ModelState.AddModelError("TinhThanh", "Tỉnh/Thành không được để trống!");

            if (!ModelState.IsValid)
                return View(obj);

            bool kq = _bus.CapNhat(obj, dsHinhAnh);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Cập nhật thành công! Tòa nhà đang chờ admin duyệt lại."
                : "Cập nhật thất bại, vui lòng thử lại.";

            return RedirectToAction("DanhSach");
        }

        // ── XÓA ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuiYeuCauXoa(string maToaNha)
        {
            var user = GetUserOnline();
            bool kq = _bus.GuiYeuCauXoa(maToaNha, user.MaNguoiDung);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã gửi yêu cầu xóa đến admin. Vui lòng chờ xác nhận."
                : "Gửi yêu cầu thất bại! Tòa nhà đang có phòng cho thuê.";

            return RedirectToAction("DanhSach");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HuyYeuCauXoa(string maToaNha)
        {
            var user = GetUserOnline();
            bool kq = _bus.HuyYeuCauXoa(maToaNha, user.MaNguoiDung);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã hủy yêu cầu xóa."
                : "Thao tác thất bại!";

            return RedirectToAction("DanhSach");
        }

        // ── XÓA ẢNH (AJAX) ───────────────────────────────────────────

        [HttpPost]
        public ActionResult XoaHinhAnh(int maHinhAnh)
        {
            bool kq = _bus.XoaHinhAnh(maHinhAnh);
            return Json(new { success = kq });
        }
    }
}
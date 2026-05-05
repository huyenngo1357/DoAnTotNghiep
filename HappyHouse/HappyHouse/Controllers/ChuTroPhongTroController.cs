using HappyHouse.Models;
using PagedList;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class ChuTroPhongTroController : ChuTroBaseController
    {
        private readonly PhongTroBusiness _bus = new PhongTroBusiness();

        private void LoadDropdown(string maChuTro,
                                  string maToaNha = null,
                                  string trangThaiPhong = null)
        {
            ViewBag.DsToaNha = new SelectList(
                _bus.LayToaNhaCuaChuTro(maChuTro),
                "MaToaNha", "TenToaNha", maToaNha);

            ViewBag.DsTienIch = _bus.LayDanhSachTienIch();

            var dsTrangThai = new[]
            {
                new { Value = "",         Text = "--- Tất cả ---" },
                new { Value = "Trong",    Text = "Còn trống"      },
                new { Value = "DaThue",   Text = "Đã thuê"        },
                new { Value = "BaoTri",   Text = "Bảo trì"        },
                new { Value = "ChoDuyet", Text = "Chờ duyệt"      },
            };
            ViewBag.DsTrangThai = new SelectList(
                dsTrangThai, "Value", "Text", trangThaiPhong);
        }

        // ── DANH SÁCH ────────────────────────────────────────────────

        [HttpGet]
        public ActionResult DanhSach(int page = 1,
                                     string tuKhoa = null,
                                     string maToaNha = null,
                                     string trangThaiPhong = null)
        {
            return HienThiDanhSach(page, tuKhoa, maToaNha, trangThaiPhong);
        }

        [HttpPost]
        public ActionResult DanhSach(string tuKhoa,
                                     string maToaNha,
                                     string trangThaiPhong,
                                     int page = 1)
        {
            return HienThiDanhSach(page, tuKhoa, maToaNha, trangThaiPhong);
        }

        private ActionResult HienThiDanhSach(int page,
                                              string tuKhoa,
                                              string maToaNha,
                                              string trangThaiPhong)
        {
            var user = GetUserOnline();
            var lst = _bus.LayDanhSachChuTro(
                           user.MaNguoiDung, tuKhoa, maToaNha, trangThaiPhong);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaToaNha = maToaNha;
            ViewBag.TrangThaiPhong = trangThaiPhong;
            LoadDropdown(user.MaNguoiDung, maToaNha, trangThaiPhong);

            return View(lst.ToPagedList(page, 10));
        }

        // ── CHI TIẾT ─────────────────────────────────────────────────

        public ActionResult ChiTiet(string maPhong)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maPhong);

            if (obj == null) return HttpNotFound();
            if (obj.ToaNha.MaChuTro != user.MaNguoiDung) return HttpNotFound();

            return View(obj);
        }

        // ── THÊM MỚI ─────────────────────────────────────────────────

        [HttpGet]
        public ActionResult ThemMoi()
        {
            var user = GetUserOnline();
            var dsToaNha = _bus.LayToaNhaCuaChuTro(user.MaNguoiDung);

            if (dsToaNha.Count == 0)
            {
                TempData["Error"] = "Bạn chưa có tòa nhà nào được duyệt. " +
                                    "Vui lòng thêm tòa nhà và chờ admin duyệt trước!";
                return RedirectToAction("DanhSach");
            }

            LoadDropdown(user.MaNguoiDung);
            return View(new PhongTro());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemMoi(PhongTro obj,
                                    List<HttpPostedFileBase> dsHinhAnh,
                                    List<string> dsTienIch)
        {
            var user = GetUserOnline();

            if (string.IsNullOrWhiteSpace(obj.SoPhong))
                ModelState.AddModelError("SoPhong", "Số phòng không được để trống!");

            if (string.IsNullOrEmpty(obj.MaToaNha))
                ModelState.AddModelError("MaToaNha", "Vui lòng chọn tòa nhà!");

            if (obj.GiaThue <= 0)
                ModelState.AddModelError("GiaThue", "Giá thuê phải lớn hơn 0!");

            if (obj.SoNguoiToiDa <= 0)
                ModelState.AddModelError("SoNguoiToiDa", "Số người phải lớn hơn 0!");

            if (!ModelState.IsValid)
            {
                LoadDropdown(user.MaNguoiDung, obj.MaToaNha);
                return View(obj);
            }

            bool kq = _bus.ThemMoi(obj, dsHinhAnh, dsTienIch);

            if (kq)
            {
                TempData["Success"] = "Thêm phòng thành công!";
                return RedirectToAction("DanhSach");
            }

            ModelState.AddModelError("SoPhong",
                "Số phòng này đã tồn tại trong tòa nhà!");
            LoadDropdown(user.MaNguoiDung, obj.MaToaNha);
            return View(obj);
        }

        // ── SỬA THÔNG TIN ─────────────────────────────────────────────

        [HttpGet]
        public ActionResult SuaThongTin(string maPhong)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maPhong);

            if (obj == null) return HttpNotFound();
            if (obj.ToaNha.MaChuTro != user.MaNguoiDung) return HttpNotFound();

            if (obj.TrangThaiPhong == "DaThue")
            {
                TempData["Error"] = "Không thể sửa phòng đang có người thuê!";
                return RedirectToAction("DanhSach");
            }

            LoadDropdown(user.MaNguoiDung, obj.MaToaNha);
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaThongTin(PhongTro obj,
                                        List<HttpPostedFileBase> dsHinhAnh,
                                        List<string> dsTienIch)
        {
            var user = GetUserOnline();
            var objDb = _bus.LayChiTiet(obj.MaPhong);

            if (objDb == null) return HttpNotFound();
            if (objDb.ToaNha.MaChuTro != user.MaNguoiDung) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(obj.SoPhong))
                ModelState.AddModelError("SoPhong", "Số phòng không được để trống!");

            if (obj.GiaThue <= 0)
                ModelState.AddModelError("GiaThue", "Giá thuê phải lớn hơn 0!");

            if (obj.SoNguoiToiDa <= 0)
                ModelState.AddModelError("SoNguoiToiDa", "Số người phải lớn hơn 0!");

            if (!ModelState.IsValid)
            {
                LoadDropdown(user.MaNguoiDung, objDb.MaToaNha);
                return View(obj);
            }

            bool kq = _bus.CapNhat(obj, dsHinhAnh, dsTienIch);

            if (kq)
            {
                TempData["Success"] = "Cập nhật phòng thành công!";
                return RedirectToAction("DanhSach");
            }

            ModelState.AddModelError("SoPhong",
                "Số phòng này đã tồn tại trong tòa nhà!");
            LoadDropdown(user.MaNguoiDung, objDb.MaToaNha);
            return View(obj);
        }

        // ── XÓA ──────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Xoa(string maPhong)
        {
            var user = GetUserOnline();
            bool kq = _bus.Xoa(maPhong, user.MaNguoiDung);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã xóa phòng thành công."
                : "Xóa thất bại! Phòng đang có hợp đồng thuê.";

            return RedirectToAction("DanhSach");
        }

        // ── XÓA ẢNH AJAX ─────────────────────────────────────────────

        [HttpPost]
        public ActionResult XoaHinhAnh(int maHinhAnh)
        {
            bool kq = _bus.XoaHinhAnh(maHinhAnh);
            return Json(new { success = kq });
        }
    }
}
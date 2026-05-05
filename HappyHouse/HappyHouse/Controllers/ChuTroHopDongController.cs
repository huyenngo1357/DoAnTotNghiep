using HappyHouse.Models;
using PagedList;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class ChuTroHopDongController : ChuTroBaseController
    {
        private readonly HopDongBusiness _bus = new HopDongBusiness();

        private void LoadDropdown(string maChuTro,
                                  string maPhong = null,
                                  string trangThai = null)
        {
            // Tất cả phòng của chủ trọ (để filter)
            var dsPhong = DataProvider.Entities.PhongTroes
                              .Include("ToaNha")
                              .Where(x => x.ToaNha.MaChuTro == maChuTro
                                       && x.TrangThai == true)
                              .OrderBy(x => x.ToaNha.TenToaNha)
                              .ThenBy(x => x.SoPhong)
                              .ToList()
                              .Select(p => new
                              {
                                  MaPhong = p.MaPhong,
                                  TenHienThi = p.SoPhong + " — " + p.ToaNha.TenToaNha
                              }).ToList();

            ViewBag.DsPhong = new SelectList(dsPhong, "MaPhong", "TenHienThi", maPhong);

            var dsTrangThai = new[]
            {
                new { Value = "",         Text = "--- Tất cả ---" },
                new { Value = "ChoKy",    Text = "Chờ ký"         },
                new { Value = "DangThue", Text = "Đang thuê"      },
                new { Value = "HetHan",   Text = "Hết hạn"        },
                new { Value = "DaHuy",    Text = "Đã hủy"         },
            };
            ViewBag.DsTrangThai = new SelectList(
                dsTrangThai, "Value", "Text", trangThai);

            // Tòa nhà cho form ThemMoi
            ViewBag.DsToaNha = new SelectList(
                _bus.LayToaNhaCuaChuTro(maChuTro),
                "MaToaNha", "TenToaNha");
        }

        // ── DANH SÁCH ────────────────────────────────────────────────

        [HttpGet]
        public ActionResult DanhSach(int page = 1,
                                     string tuKhoa = null,
                                     string maPhong = null,
                                     string trangThai = null)
        {
            return HienThiDanhSach(page, tuKhoa, maPhong, trangThai);
        }

        [HttpPost]
        public ActionResult DanhSach(string tuKhoa,
                                     string maPhong,
                                     string trangThai,
                                     int page = 1)
        {
            return HienThiDanhSach(page, tuKhoa, maPhong, trangThai);
        }

        private ActionResult HienThiDanhSach(int page,
                                              string tuKhoa,
                                              string maPhong,
                                              string trangThai)
        {
            var user = GetUserOnline();
            var lst = _bus.LayDanhSachChuTro(
                           user.MaNguoiDung, tuKhoa, maPhong, trangThai);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaPhong = maPhong;
            ViewBag.TrangThai = trangThai;
            LoadDropdown(user.MaNguoiDung, maPhong, trangThai);

            return View(lst.ToPagedList(page, 10));
        }

        // ── CHI TIẾT ─────────────────────────────────────────────────

        public ActionResult ChiTiet(string maHopDong)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maHopDong);
            if (obj == null) return HttpNotFound();
            if (obj.MaChuTro != user.MaNguoiDung) return HttpNotFound();
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
                TempData["Error"] = "Bạn chưa có tòa nhà nào được duyệt!";
                return RedirectToAction("DanhSach");
            }

            ViewBag.DsToaNha = new SelectList(
                dsToaNha, "MaToaNha", "TenToaNha");

            return View(new HopDong
            {
                NgayBatDau = System.DateTime.Today,
                NgayKetThuc = System.DateTime.Today.AddYears(1),
                NgayThanhToanHangThang = 5
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemMoi(HopDong obj,
                                    List<string> dsMaGiaDichVu,
                                    List<int> dsSoLuong)
        {
            var user = GetUserOnline();

            if (string.IsNullOrEmpty(obj.MaPhong))
                ModelState.AddModelError("MaPhong", "Vui lòng chọn phòng!");
            if (string.IsNullOrEmpty(obj.MaKhachHang))
                ModelState.AddModelError("MaKhachHang", "Vui lòng chọn khách hàng!");
            if (obj.NgayKetThuc <= obj.NgayBatDau)
                ModelState.AddModelError("NgayKetThuc",
                    "Ngày kết thúc phải sau ngày bắt đầu!");
            if (obj.GiaThueThang <= 0)
                ModelState.AddModelError("GiaThueThang",
                    "Giá thuê phải lớn hơn 0!");

            if (!ModelState.IsValid)
            {
                var dsToaNha = _bus.LayToaNhaCuaChuTro(user.MaNguoiDung);
                ViewBag.DsToaNha = new SelectList(
                    dsToaNha, "MaToaNha", "TenToaNha");
                return View(obj);
            }

            obj.MaChuTro = user.MaNguoiDung;
            bool kq = _bus.ThemMoi(obj, dsMaGiaDichVu, dsSoLuong);

            if (kq)
            {
                TempData["Success"] = "Tạo hợp đồng thành công!";
                return RedirectToAction("DanhSach");
            }

            ModelState.AddModelError("MaPhong",
                "Phòng này đang có hợp đồng active!");
            var ds = _bus.LayToaNhaCuaChuTro(user.MaNguoiDung);
            ViewBag.DsToaNha = new SelectList(ds, "MaToaNha", "TenToaNha");
            return View(obj);
        }

        // ── SỬA ──────────────────────────────────────────────────────

        [HttpGet]
        public ActionResult SuaThongTin(string maHopDong)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maHopDong);
            if (obj == null) return HttpNotFound();
            if (obj.MaChuTro != user.MaNguoiDung) return HttpNotFound();
            if (obj.TrangThaiHopDong != "ChoKy")
            {
                TempData["Error"] = "Chỉ sửa được hợp đồng ở trạng thái Chờ ký!";
                return RedirectToAction("DanhSach");
            }

            ViewBag.DsDichVuToaNha = _bus.LayDichVuToaNha(
                obj.PhongTro.MaToaNha);
            ViewBag.DsDichVuHopDong = _bus.LayDichVuHopDong(maHopDong);

            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaThongTin(HopDong obj,
                                        List<string> dsMaGiaDichVu,
                                        List<int> dsSoLuong)
        {
            var user = GetUserOnline();
            var objDb = _bus.LayChiTiet(obj.MaHopDong);
            if (objDb == null) return HttpNotFound();
            if (objDb.MaChuTro != user.MaNguoiDung) return HttpNotFound();

            if (obj.NgayKetThuc <= obj.NgayBatDau)
                ModelState.AddModelError("NgayKetThuc",
                    "Ngày kết thúc phải sau ngày bắt đầu!");
            if (obj.GiaThueThang <= 0)
                ModelState.AddModelError("GiaThueThang",
                    "Giá thuê phải lớn hơn 0!");

            if (!ModelState.IsValid)
            {
                ViewBag.DsDichVuToaNha = _bus.LayDichVuToaNha(
                    objDb.PhongTro.MaToaNha);
                ViewBag.DsDichVuHopDong = _bus.LayDichVuHopDong(obj.MaHopDong);
                return View(obj);
            }

            bool kq = _bus.CapNhat(obj, dsMaGiaDichVu, dsSoLuong);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Cập nhật hợp đồng thành công!"
                : "Cập nhật thất bại!";
            return RedirectToAction("DanhSach");
        }

        // ── KÝ / HỦY / KẾT THÚC ─────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult KyHopDong(string maHopDong)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maHopDong);
            if (obj == null || obj.MaChuTro != user.MaNguoiDung)
                return HttpNotFound();

            bool kq = _bus.KyHopDong(maHopDong);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Ký hợp đồng thành công! Phòng chuyển sang Đang thuê."
                : "Thao tác thất bại.";
            return RedirectToAction("DanhSach");
        }

        [HttpGet]
        public ActionResult Huy(string maHopDong)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maHopDong);
            if (obj == null || obj.MaChuTro != user.MaNguoiDung)
                return HttpNotFound();
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Huy(string maHopDong, string lyDoHuy)
        {
            if (string.IsNullOrWhiteSpace(lyDoHuy))
            {
                TempData["Error"] = "Vui lòng nhập lý do hủy.";
                return RedirectToAction("Huy", new { maHopDong });
            }
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maHopDong);
            if (obj == null || obj.MaChuTro != user.MaNguoiDung)
                return HttpNotFound();

            bool kq = _bus.HuyHopDong(maHopDong, lyDoHuy);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã hủy hợp đồng."
                : "Thao tác thất bại.";
            return RedirectToAction("DanhSach");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult KetThuc(string maHopDong)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maHopDong);
            if (obj == null || obj.MaChuTro != user.MaNguoiDung)
                return HttpNotFound();

            bool kq = _bus.KetThucHopDong(maHopDong);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã kết thúc hợp đồng."
                : "Thao tác thất bại.";
            return RedirectToAction("DanhSach");
        }

        // ── AJAX ─────────────────────────────────────────────────────

        // Load phòng trống theo tòa nhà
        public ActionResult LayPhongTrong(string maToaNha)
        {
            var lst = _bus.LayPhongTrongTheoToaNha(maToaNha)
                          .Select(p => new
                          {
                              maPhong = p.MaPhong,
                              tenHienThi = "Phòng " + p.SoPhong
                                         + " - Tầng " + p.Tang
                                         + " - " + string.Format("{0:N0}", p.GiaThue) + "đ",
                              giaThue = p.GiaThue
                          }).ToList();

            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        // Tìm kiếm khách hàng realtime
        public ActionResult TimKiemKhachHang(string tuKhoa)
        {
            var lst = _bus.TimKiemKhachHang(tuKhoa)
                          .Select(x => new
                          {
                              maNguoiDung = x.MaNguoiDung,
                              hoTen = x.HoTen,
                              soDienThoai = x.SoDienThoai,
                              email = x.Email,
                              tenHienThi = x.HoTen
                                          + " — " + x.SoDienThoai
                                          + " (" + x.Email + ")"
                          }).ToList();

            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        // Load dịch vụ theo tòa nhà
        public ActionResult LayDichVuToaNha(string maToaNha)
        {
            var lst = _bus.LayDichVuToaNha(maToaNha)
                          .Select(g => new
                          {
                              maGiaDichVu = g.MaGiaDichVu,
                              tenDichVu = g.TienIch != null
                                            ? g.TienIch.TenTienIch
                                            : g.TenDichVu,
                              bieuTuong = g.TienIch?.BieuTuong ?? "fa-cog",
                              donGia = g.DonGia,
                              donVi = g.DonVi,
                              tenHienThi = (g.TienIch != null
                                             ? g.TienIch.TenTienIch
                                             : g.TenDichVu)
                                          + " - " + string.Format("{0:N0}", g.DonGia)
                                          + "đ/" + g.DonVi
                          }).ToList();

            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        // Thêm vào ChuTroHopDongController
        public ActionResult XuatPdf(string maHopDong)
        {
            var chuTro = GetUserOnline();

            var hopDong = DataProvider.Entities.HopDongs
                             .Include("PhongTro")
                             .Include("PhongTro.ToaNha")
                             .Include("NguoiDung1")
                             .Include("HopDong_DichVu")
                             .Include("HopDong_DichVu.GiaDichVu")
                             .Include("HopDong_DichVu.GiaDichVu.TienIch")
                             .FirstOrDefault(x => x.MaHopDong == maHopDong
                                               && x.MaChuTro
                                                  == chuTro.MaNguoiDung);

            if (hopDong == null) return HttpNotFound();

            byte[] pdf = HopDongPdfHelper.TaoHopDongPdf(hopDong);

            string tenFile = "HopDong_" + maHopDong + ".pdf";
            return File(pdf,
                        "application/pdf",
                        tenFile);
        }
    }
}
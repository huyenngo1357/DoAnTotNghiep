using HappyHouse.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class HopDongController : Controller
    {
        private readonly HopDongBusiness _bus = new HopDongBusiness();

        private NguoiDung KiemTraDangNhap()
        {
            var user = Session["UserOnline"] as NguoiDung;
            if (user == null || user.MaVaiTro != "KHACHHANG")
                return null;
            return user;
        }

        // ── DANH SÁCH HỢP ĐỒNG CỦA KHÁCH ──────────────────────────
        public ActionResult DanhSach()
        {
            var user = KiemTraDangNhap();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan",
                    new { returnUrl = "/HopDong/DanhSach" });

            var lst = DataProvider.Entities.HopDongs
                          .Include("PhongTro")
                          .Include("PhongTro.ToaNha")
                          .Include("PhongTro.HinhAnhPhongs")
                          .Where(x => x.MaKhachHang == user.MaNguoiDung)
                          .OrderByDescending(x => x.NgayTao)
                          .ToList();

            var dsPhongDaDG = DataProvider.Entities.DanhGiaPhongs
                                  .Where(x => x.MaKhachHang == user.MaNguoiDung)
                                  .Select(x => x.MaPhong)
                                  .ToList();

            ViewBag.DsPhongDaDanhGia = dsPhongDaDG;
            return View(lst);
        }

        // ── CHI TIẾT HỢP ĐỒNG ──────────────────────────────────────
        public ActionResult ChiTiet(string maHopDong)
        {
            var user = KiemTraDangNhap();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var hopDong = DataProvider.Entities.HopDongs
                              .Include("PhongTro")
                              .Include("PhongTro.ToaNha")
                              .Include("PhongTro.ToaNha.NguoiDung")
                              .Include("PhongTro.HinhAnhPhongs")
                              .Include("PhongTro.TienIches")
                              .Include("HopDong_DichVu")
                              .Include("HopDong_DichVu.GiaDichVu")
                              .Include("HopDong_DichVu.GiaDichVu.TienIch")
                              .FirstOrDefault(x => x.MaHopDong == maHopDong
                                                && x.MaKhachHang == user.MaNguoiDung);

            if (hopDong == null) return HttpNotFound();

            bool daDanhGia = DataProvider.Entities.DanhGiaPhongs
                                 .Any(x => x.MaPhong == hopDong.MaPhong
                                        && x.MaKhachHang == user.MaNguoiDung);

            ViewBag.DaDanhGia = daDanhGia;
            return View(hopDong);
        }

        // ══════════════════════════════════════════════════════════════
        //  YÊU CẦU ĐĂNG KÝ HỢP ĐỒNG (Khách hàng đặt phòng)
        //  Flow: Khách xem phòng → click Đăng ký → điền thông tin
        //        → tạo HopDong trạng thái "ChoKy"
        //        → Chủ trọ duyệt và ký → "DangThue"
        // ══════════════════════════════════════════════════════════════

        [HttpGet]
        public ActionResult YeuCauDangKy(string maPhong)
        {
            var user = KiemTraDangNhap();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan",
                    new { returnUrl = "/HopDong/YeuCauDangKy?maPhong=" + maPhong });

            // Lấy thông tin phòng
            var phong = DataProvider.Entities.PhongTroes
                            .Include("ToaNha")
                            .Include("ToaNha.NguoiDung")
                            .Include("HinhAnhPhongs")
                            .Include("TienIches")
                            .FirstOrDefault(x => x.MaPhong == maPhong
                                              && x.TrangThai == true);

            if (phong == null) return HttpNotFound();

            // Phòng phải đang trống mới cho đăng ký
            if (phong.TrangThaiPhong != "Trong")
            {
                TempData["Error"] = "Phòng này hiện không còn trống. "
                                  + "Vui lòng chọn phòng khác!";
                return RedirectToAction("ChiTiet", "PhongTro",
                    new { maPhong });
            }

            // Kiểm tra khách đã có hợp đồng ChoKy cho phòng này chưa
            bool daYeuCau = DataProvider.Entities.HopDongs
                                .Any(x => x.MaPhong == maPhong
                                       && x.MaKhachHang == user.MaNguoiDung
                                       && x.TrangThaiHopDong == "ChoKy"
                                       && x.TrangThai == true);
            if (daYeuCau)
            {
                TempData["Error"] = "Bạn đã có yêu cầu đăng ký cho phòng này "
                                  + "đang chờ chủ trọ xác nhận!";
                return RedirectToAction("DanhSach");
            }

            ViewBag.Phong = phong;
            ViewBag.DsDichVu = LayDichVuTheoToaNha(phong.MaToaNha);

            // Giá trị mặc định cho form
            var hopDongMau = new HopDong
            {
                MaPhong = maPhong,
                GiaThueThang = phong.GiaThue,
                TienCoc = phong.TienCoc,
                NgayBatDau = DateTime.Today,
                NgayKetThuc = DateTime.Today.AddMonths(12),
                NgayThanhToanHangThang = 5
            };

            return View(hopDongMau);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult YeuCauDangKy(HopDong obj,
                                          List<string> dsMaGiaDichVu,
                                          List<int> dsSoLuong)
        {
            var user = KiemTraDangNhap();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            // Lấy lại thông tin phòng để xác thực
            var phong = DataProvider.Entities.PhongTroes
                            .Include("ToaNha")
                            .Include("ToaNha.NguoiDung")
                            .Include("HinhAnhPhongs")
                            .Include("TienIches")
                            .FirstOrDefault(x => x.MaPhong == obj.MaPhong
                                              && x.TrangThai == true);

            if (phong == null) return HttpNotFound();

            // Validate
            if (phong.TrangThaiPhong != "Trong")
            {
                TempData["Error"] = "Rất tiếc! Phòng này vừa có người đặt. "
                                  + "Vui lòng chọn phòng khác.";
                return RedirectToAction("DanhSach", "PhongTro");
            }

            if (obj.NgayKetThuc <= obj.NgayBatDau)
            {
                TempData["Error"] = "Ngày kết thúc phải sau ngày bắt đầu!";
                ViewBag.Phong = phong;
                ViewBag.DsDichVu = LayDichVuTheoToaNha(phong.MaToaNha);
                return View(obj);
            }

            if (obj.NgayBatDau < DateTime.Today)
            {
                TempData["Error"] = "Ngày bắt đầu không được ở quá khứ!";
                ViewBag.Phong = phong;
                ViewBag.DsDichVu = LayDichVuTheoToaNha(phong.MaToaNha);
                return View(obj);
            }

            if (obj.NgayThanhToanHangThang < 1 || obj.NgayThanhToanHangThang > 28)
                obj.NgayThanhToanHangThang = 5;

            // Gán thông tin từ phòng & session
            obj.MaKhachHang = user.MaNguoiDung;
            obj.MaChuTro = phong.ToaNha.MaChuTro;
            obj.GiaThueThang = phong.GiaThue;   // Giữ nguyên giá phòng
            obj.TienCoc = phong.TienCoc;

            // Tạo hợp đồng qua Business (tự sinh mã, set ChoKy)
            bool kq = _bus.ThemMoi(obj, dsMaGiaDichVu, dsSoLuong);

            if (!kq)
            {
                TempData["Error"] = "Không thể tạo yêu cầu. "
                                  + "Phòng này đang có yêu cầu khác hoặc đã được đặt!";
                ViewBag.Phong = phong;
                ViewBag.DsDichVu = LayDichVuTheoToaNha(phong.MaToaNha);
                return View(obj);
            }

            // Gửi thông báo cho chủ trọ
            GuiThongBaoChuTro(phong.ToaNha.MaChuTro,
                phong.SoPhong,
                phong.ToaNha.TenToaNha,
                user.HoTen);

            TempData["Success"] =
                "Yêu cầu đăng ký hợp đồng đã được gửi thành công! "
              + "Vui lòng chờ chủ trọ xem xét và xác nhận. "
              + "Bạn sẽ nhận được thông báo khi hợp đồng được ký.";

            return RedirectToAction("DanhSach");
        }

        // ── AJAX: lấy dịch vụ tòa nhà cho form đăng ký ─────────────
        public ActionResult LayDichVuToaNha(string maToaNha)
        {
            var lst = LayDichVuTheoToaNha(maToaNha);

            var result = lst.Select(dv => new
            {
                maGiaDichVu = dv.MaGiaDichVu,
                tenDichVu = dv.TienIch != null
                              ? dv.TienIch.TenTienIch
                              : dv.TenDichVu ?? "",
                bieuTuong = dv.TienIch?.BieuTuong ?? "fa-cog",
                donGia = dv.DonGia,
                donVi = dv.DonVi ?? "tháng"
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult XuatPdf(string maHopDong)
        {
            var user = Session["UserOnline"] as NguoiDung;
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan",
                    new { returnUrl = "/HopDong/XuatPdf?maHopDong=" + maHopDong });

            var hopDong = DataProvider.Entities.HopDongs
                             .Include("PhongTro")
                             .Include("PhongTro.ToaNha")
                             .Include("PhongTro.ToaNha.NguoiDung")
                             .Include("NguoiDung")
                             .Include("NguoiDung1")
                             .Include("HopDong_DichVu")
                             .Include("HopDong_DichVu.GiaDichVu")
                             .Include("HopDong_DichVu.GiaDichVu.TienIch")
                             .FirstOrDefault(x => x.MaHopDong == maHopDong
                                               && x.MaKhachHang == user.MaNguoiDung);

            if (hopDong == null) return HttpNotFound();

            byte[] pdf = HopDongPdfHelper.TaoHopDongPdf(hopDong);
            return File(pdf, "application/pdf", "HopDong_" + maHopDong + ".pdf");
        }

        // ── PRIVATE ─────────────────────────────────────────────────

        private List<GiaDichVu> LayDichVuTheoToaNha(string maToaNha)
        {
            return DataProvider.Entities.GiaDichVus
                       .Include("TienIch")
                       .Where(x => x.MaToaNha == maToaNha
                                && x.TrangThai == true
                                && x.NgayApDung <= DateTime.Today
                                && (x.NgayKetThuc == null
                                    || x.NgayKetThuc >= DateTime.Today))
                       .OrderBy(x => x.TienIch.TenTienIch)
                       .ToList();
        }

        private void GuiThongBaoChuTro(string maChuTro,
                                        string soPhong,
                                        string tenToaNha,
                                        string tenKhach)
        {
            try
            {
                var db = DataProvider.Entities;
                db.ThongBaos.Add(new ThongBao
                {
                    MaNguoiDung = maChuTro,
                    TieuDe = "Yêu cầu đăng ký thuê phòng mới",
                    NoiDung = $"Khách hàng {tenKhach} muốn đăng ký "
                                 + $"thuê phòng {soPhong} — {tenToaNha}. "
                                 + "Vui lòng xem xét và ký hợp đồng.",
                    LoaiThongBao = "HopDong",
                    DuongDan = "/ChuTroHopDong/DanhSach?trangThai=ChoKy",
                    DaDoc = false,
                    NgayTao = DateTime.Now
                });
                db.Configuration.ValidateOnSaveEnabled = false;
                try { db.SaveChanges(); }
                finally { db.Configuration.ValidateOnSaveEnabled = true; }
            }
            catch { /* Không để lỗi thông báo ảnh hưởng luồng chính */ }
        }
    }
}
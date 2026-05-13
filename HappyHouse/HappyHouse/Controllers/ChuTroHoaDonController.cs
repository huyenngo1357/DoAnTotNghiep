using HappyHouse.Models;
using PagedList;
using System;
using System.Linq;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class ChuTroHoaDonController : ChuTroBaseController
    {
        private readonly HoaDonBusiness _bus = new HoaDonBusiness();

        private void LoadDropdown(string maChuTro,
                                   string maHopDong = null,
                                   string trangThai = null)
        {
            var dsHopDong =
                _bus.LayHopDongDangThueCuaChuTro(maChuTro);

            var dsHienThi = dsHopDong.Select(h => new
            {
                MaHopDong = h.MaHopDong,
                // NguoiDung1 = KhachHang
                TenHienThi =
                    (h.PhongTro != null
                        ? "Phong " + h.PhongTro.SoPhong
                          + " - " + h.PhongTro.ToaNha.TenToaNha
                        : h.MaHopDong)
                    + (h.NguoiDung1 != null
                        ? " (" + h.NguoiDung1.HoTen + ")"
                        : "")
            }).ToList();

            ViewBag.DsHopDong = new SelectList(
                dsHienThi,
                "MaHopDong",
                "TenHienThi",
                maHopDong);

            var dsTrangThai = new[]
            {
                new { Value = "",
                      Text  = "--- Tất cả ---"        },
                new { Value = "ChuaThanhToan",
                      Text  = "Chưa thanh toán"       },
                new { Value = "ChoDuyet",
                      Text  = "Chờ duyệt biên lai"    },
                new { Value = "DaThanhToan",
                      Text  = "Đã thanh toán"         },
                new { Value = "QuaHan",
                      Text  = "Quá hạn"               },
            };
            ViewBag.DsTrangThai = new SelectList(
                dsTrangThai, "Value", "Text", trangThai);
        }

        // DANH SÁCH

        [HttpGet]
        public ActionResult DanhSach(int page = 1,
                                      string tuKhoa = null,
                                      string maHopDong = null,
                                      string trangThai = null)
        {
            return HienThiDanhSach(
                page, tuKhoa, maHopDong, trangThai);
        }

        [HttpPost]
        public ActionResult DanhSach(string tuKhoa,
                                      string maHopDong,
                                      string trangThai,
                                      int page = 1)
        {
            return HienThiDanhSach(
                page, tuKhoa, maHopDong, trangThai);
        }

        private ActionResult HienThiDanhSach(
            int page,
            string tuKhoa,
            string maHopDong,
            string trangThai)
        {
            var user = GetUserOnline();
            var lst = _bus.LayDanhSachChuTro(
                user.MaNguoiDung,
                tuKhoa, maHopDong, trangThai);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaHopDong = maHopDong;
            ViewBag.TrangThai = trangThai;
            ViewBag.SoChuaThanhToan =
                _bus.DemChuaThanhToan(user.MaNguoiDung);

            LoadDropdown(
                user.MaNguoiDung, maHopDong, trangThai);

            return View(lst.ToPagedList(page, 10));
        }

        // CHI TIẾT

        public ActionResult ChiTiet(string maHoaDon)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maHoaDon);

            if (obj == null)
                return HttpNotFound();
            if (obj.HopDong.MaChuTro != user.MaNguoiDung)
                return HttpNotFound();

            return View(obj);
        }

        // THÊM MỚI

        [HttpGet]
        public ActionResult ThemMoi()
        {
            var user = GetUserOnline();
            var dsHopDong =
                _bus.LayHopDongDangThueCuaChuTro(
                    user.MaNguoiDung);

            if (dsHopDong.Count == 0)
            {
                TempData["Error"] =
                    "Không có hợp đồng đang thuê nào!";
                return RedirectToAction("DanhSach");
            }

            LoadDropdown(user.MaNguoiDung);
            ViewBag.ThangHoaDon =
                DateTime.Today.ToString("yyyy-MM");

            return View(new HoaDon
            {
                HanThanhToan = DateTime.Today.AddDays(10)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemMoi(HoaDon obj,
                                     string thangHoaDon)
        {
            var user = GetUserOnline();

            DateTime thang;
            if (!DateTime.TryParse(
                    thangHoaDon + "-01", out thang))
                ModelState.AddModelError("",
                    "Tháng hóa đơn không hợp lệ!");
            else
                obj.ThangHoaDon = thang;

            if (string.IsNullOrEmpty(obj.MaHopDong))
                ModelState.AddModelError("MaHopDong",
                    "Vui lòng chọn hợp đồng!");

            if (obj.TienPhong < 0)
                ModelState.AddModelError("TienPhong",
                    "Tiền phòng không hợp lệ!");
            if (!ModelState.IsValid)
            {
                LoadDropdown(
                    user.MaNguoiDung, obj.MaHopDong);
                ViewBag.ThangHoaDon = thangHoaDon;
                return View(obj);
            }

            bool kq = _bus.ThemMoi(obj);
            if (kq)
            {
                TempData["Success"] =
                    "Tạo hóa đơn thành công!";
                return RedirectToAction("DanhSach");
            }

            ModelState.AddModelError("",
                "Hóa đơn tháng này đã tồn tại!");
            LoadDropdown(
                user.MaNguoiDung, obj.MaHopDong);
            ViewBag.ThangHoaDon = thangHoaDon;
            return View(obj);
        }

        // SỬA

        [HttpGet]
        public ActionResult SuaThongTin(string maHoaDon)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maHoaDon);

            if (obj == null)
                return HttpNotFound();
            if (obj.HopDong.MaChuTro != user.MaNguoiDung)
                return HttpNotFound();
            if (obj.TrangThaiHoaDon == "DaThanhToan")
            {
                TempData["Error"] =
                    "Không thể sửa hóa đơn đã thanh toán!";
                return RedirectToAction("DanhSach");
            }

            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaThongTin(HoaDon obj)
        {
            var user = GetUserOnline();
            var objDb = _bus.LayChiTiet(obj.MaHoaDon);

            if (objDb == null)
                return HttpNotFound();
            if (objDb.HopDong.MaChuTro != user.MaNguoiDung)
                return HttpNotFound();

            if (obj.TienPhong < 0)
                ModelState.AddModelError("TienPhong",
                    "Tiền phòng không hợp lệ!");

            if (!ModelState.IsValid)
                return View(obj);

            bool kq = _bus.CapNhat(obj);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Cập nhật hóa đơn thành công!"
                : "Cập nhật thất bại!";

            return RedirectToAction("DanhSach");
        }

        // XÁC NHẬN THANH TOÁN

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacNhanThanhToan(string maHoaDon)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maHoaDon);

            if (obj == null)
                return HttpNotFound();
            if (obj.HopDong.MaChuTro != user.MaNguoiDung)
                return HttpNotFound();

            bool kq = _bus.XacNhanThanhToan(maHoaDon);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Xác nhận thanh toán thành công."
                : "Thao tác thất bại.";

            return RedirectToAction("DanhSach");
        }

        // ĐÁNH DẤU QUÁ HẠN

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DanhDauQuaHan(string maHoaDon)
        {
            var user = GetUserOnline();
            var obj = _bus.LayChiTiet(maHoaDon);

            if (obj == null)
                return HttpNotFound();
            if (obj.HopDong.MaChuTro != user.MaNguoiDung)
                return HttpNotFound();

            bool kq = _bus.DanhDauQuaHan(maHoaDon);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã đánh dấu quá hạn."
                : "Thao tác thất bại.";

            return RedirectToAction("DanhSach");
        }

        // Thêm vào ChuTroHoaDonController
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TuChoiBienLai(string maThanhToan,
                                           string lyDo)
        {
            var user = GetUserOnline();

            // Tìm ThanhToan
            var tt = DataProvider.Entities.ThanhToans
                         .FirstOrDefault(x =>
                             x.MaThanhToan == maThanhToan
                          && x.TrangThai == true);

            if (tt == null) return HttpNotFound();

            // Kiểm tra thuộc chủ trọ này
            var hoaDon = DataProvider.Entities.HoaDons
                             .Include("HopDong")
                             .FirstOrDefault(x =>
                                 x.MaHoaDon == tt.MaHoaDon
                              && x.HopDong.MaChuTro
                                     == user.MaNguoiDung);

            if (hoaDon == null) return HttpNotFound();

            // Từ chối
            tt.TrangThaiXacNhan = "TuChoi";
            tt.LyDoTuChoi = lyDo;
            tt.NguoiXacNhanId = user.MaNguoiDung;
            tt.NgayCapNhat = DateTime.Now;

            // Trả HoaDon về ChuaThanhToan
            hoaDon.TrangThaiHoaDon = "ChuaThanhToan";
            hoaDon.NgayCapNhat = DateTime.Now;

            DataProvider.Entities.Configuration
                .ValidateOnSaveEnabled = false;
            try
            {
                DataProvider.Entities.SaveChanges();
                TempData["Success"] =
                    "Đã từ chối biên lai. Hóa đơn trả về chưa thanh toán.";
            }
            catch
            {
                TempData["Error"] = "Có lỗi xảy ra!";
            }
            finally
            {
                DataProvider.Entities.Configuration
                    .ValidateOnSaveEnabled = true;
            }

            return RedirectToAction("ChiTiet", new { maHoaDon = tt.MaHoaDon });
        }

        // AJAX 

        public ActionResult LayThongTinHopDong(string maHopDong, string thangHoaDon)
        {
            var dto = _bus.LayThongTinTaoHoaDon(maHopDong, thangHoaDon);

            if (dto == null)
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,
                tienPhong = dto.TienPhong,
                tienDien = dto.TienDien,
                maChiSoDien = dto.MaChiSoDien,
                soTieuThuDien = dto.SoTieuThuDien,
                donGiaDien = dto.DonGiaDien,
                coDien = dto.CoDien,
                tienNuoc = dto.TienNuoc,
                maChiSoNuoc = dto.MaChiSoNuoc,
                soTieuThuNuoc = dto.SoTieuThuNuoc,
                donGiaNuoc = dto.DonGiaNuoc,
                coNuoc = dto.CoNuoc,
                dsDichVu = dto.DsDichVu.Select(dv => new
                {
                    maGiaDichVu = dv.MaGiaDichVu,
                    tenDichVu = dv.TenDichVu,
                    bieuTuong = dv.BieuTuong,
                    donGia = dv.DonGia,
                    donVi = dv.DonVi,
                    soLuong = dv.SoLuong,
                    thanhTien = dv.ThanhTien
                }),
                tongDichVu = dto.TongDichVu,
                hanThanhToan = dto.HanThanhToan
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
using HappyHouse.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class AdminDashboardController : AdminBaseController
    {
        public ActionResult Index()
        {
            var db = DataProvider.Entities;
            int nam = DateTime.Now.Year;

            // Thống kê tổng quan
            ViewBag.TongToaNha = db.ToaNhas
                .Count(x => x.TrangThai == true);
            ViewBag.ToaNhaChoDuyet = db.ToaNhas
                .Count(x => x.TrangThaiDuyet == "ChoDuyet" && x.TrangThai == true);
            ViewBag.ToaNhaYeuCauXoa = db.ToaNhas
                .Count(x => x.TrangThaiDuyet == "YeuCauXoa" && x.TrangThai == true);

            ViewBag.TongPhongTrong = db.PhongTroes
                .Count(x => x.TrangThaiPhong == "Trong" && x.TrangThai == true && x.ToaNha.TrangThaiDuyet == "DaDuyet");
            ViewBag.TongPhongDaThue = db.PhongTroes
                .Count(x => x.TrangThaiPhong == "DaThue" && x.TrangThai == true && x.ToaNha.TrangThaiDuyet == "DaDuyet");

            ViewBag.TongKhachHang = db.NguoiDungs
                .Count(x => x.MaVaiTro == "KHACHHANG" && x.TrangThai == true);
            ViewBag.TongChuTro = db.NguoiDungs
                .Count(x => x.MaVaiTro == "CHUTRO" && x.TrangThai == true);

            ViewBag.TongHopDongDangThue = db.HopDongs
                .Count(x => x.TrangThaiHopDong == "DangThue" && x.TrangThai == true);

            // Hóa đơn chưa thanh toán toàn hệ thống
            ViewBag.TongHoaDonChuaTT = db.HoaDons
                .Count(x => (x.TrangThaiHoaDon == "ChuaThanhToan" || x.TrangThaiHoaDon == "QuaHan") && x.TrangThai == true);

            // Thanh toán chờ xác nhận toàn hệ thống
            ViewBag.TongChoXacNhan = db.ThanhToans
                .Count(x => x.TrangThaiXacNhan == "ChoXacNhan" && x.TrangThai == true);

            // Tin tức chờ đăng
            ViewBag.TongTinChuaDang = db.TinTucs
                .Count(x => x.TrangThaiDang == "ChuaDang" && x.TrangThai == true);

            // Doanh thu (theo tháng trong năm) 
            var hopDongNam = db.HopDongs
                .Where(x => x.TrangThaiHopDong == "DangThue" && x.TrangThai == true && x.NgayBatDau.Year == nam)
                .ToList();

            var theoThang = hopDongNam
                .GroupBy(x => x.NgayBatDau.Month)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.GiaThueThang));

            ViewBag.ThangLabels = string.Join(",", Enumerable.Range(1, 12).Select(m => "\"T" + m + "\""));
            ViewBag.ThangRevenue = string.Join(",", Enumerable.Range(1, 12).Select(m => theoThang.ContainsKey(m) ? theoThang[m] : 0));

            // Theo quý 
            ViewBag.QuyLabels = string.Join(",",
                Enumerable.Range(1, 4).Select(q => "\"Q" + q + "/" + nam + "\""));
            ViewBag.QuyRevenue = string.Join(",", Enumerable.Range(1, 4).Select(q =>
                {
                    var months = Enumerable.Range((q - 1) * 3 + 1, 3).ToList();
                    return months.Sum(m => theoThang.ContainsKey(m) ? theoThang[m] : 0);
                }));

            // Theo năm (5 năm) 
            var namList = Enumerable.Range(nam - 4, 5).ToList();
            var allHD = db.HopDongs
                .Where(x => x.TrangThaiHopDong == "DangThue"
                          && x.TrangThai == true)
                .ToList();
            var theoNam = allHD
                .GroupBy(x => x.NgayBatDau.Year)
                .ToDictionary(g => g.Key,
                              g => g.Sum(x => x.GiaThueThang));

            ViewBag.NamLabels = string.Join(",",
                namList.Select(n => "\"" + n + "\""));
            ViewBag.NamRevenue = string.Join(",",
                namList.Select(n => theoNam.ContainsKey(n)
                                    ? theoNam[n] : 0));

            ViewBag.NamHienTai = nam;

            // Đăng ký mới theo tháng (năm hiện tại) 
            var khachMoi = db.NguoiDungs
                .Where(x => x.MaVaiTro == "KHACHHANG"
                          && x.TrangThai == true
                          && x.NgayTao.HasValue
                          && x.NgayTao.Value.Year == nam)
                .ToList()
                .GroupBy(x => x.NgayTao.Value.Month)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.KhachMoiRevenue = string.Join(",",
                Enumerable.Range(1, 12)
                    .Select(m => khachMoi.ContainsKey(m)
                                 ? khachMoi[m] : 0));

            // Tòa nhà mới nhất 
            ViewBag.DsToaNhaMoi = db.ToaNhas
                .Include("NguoiDung")
                .Where(x => x.TrangThai == true)
                .OrderByDescending(x => x.NgayTao)
                .Take(5)
                .ToList();

            // Hợp đồng sắp hết hạn (30 ngày) 
            var ngayToi = DateTime.Today.AddDays(30);
            ViewBag.DsHopDongSapHet = db.HopDongs
                .Include("PhongTro")
                .Include("PhongTro.ToaNha")
                .Include("NguoiDung1")
                .Where(x => x.TrangThaiHopDong == "DangThue"
                          && x.TrangThai == true
                          && x.NgayKetThuc <= ngayToi)
                .OrderBy(x => x.NgayKetThuc)
                .Take(5)
                .ToList();

            return View();
        }
    }
}
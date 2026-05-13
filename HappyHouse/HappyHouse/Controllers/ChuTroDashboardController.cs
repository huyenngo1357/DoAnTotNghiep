using HappyHouse.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class ChuTroDashboardController : ChuTroBaseController
    {
        public ActionResult Index()
        {
            var db = DataProvider.Entities;
            var chuTro = GetUserOnline();
            string maId = chuTro.MaNguoiDung;
            int nam = DateTime.Now.Year;

            // Tòa nhà
            ViewBag.TongToaNha = db.ToaNhas
                .Count(x => x.MaChuTro == maId
                          && x.TrangThai == true);
            ViewBag.ToaNhaDaDuyet = db.ToaNhas
                .Count(x => x.MaChuTro == maId
                          && x.TrangThaiDuyet == "DaDuyet"
                          && x.TrangThai == true);
            ViewBag.ToaNhaChoDuyet = db.ToaNhas
                .Count(x => x.MaChuTro == maId
                          && x.TrangThaiDuyet == "ChoDuyet"
                          && x.TrangThai == true);

            // Phòng
            ViewBag.TongPhong = db.PhongTroes
                .Count(x => x.ToaNha.MaChuTro == maId
                          && x.TrangThai == true);
            ViewBag.PhongTrong = db.PhongTroes
                .Count(x => x.ToaNha.MaChuTro == maId
                          && x.TrangThaiPhong == "Trong"
                          && x.TrangThai == true);
            ViewBag.PhongDaThue = db.PhongTroes
                .Count(x => x.ToaNha.MaChuTro == maId
                          && x.TrangThaiPhong == "DaThue"
                          && x.TrangThai == true);

            // Tỷ lệ lấp đầy
            int tongPhong = (int)ViewBag.TongPhong;
            int daThue = (int)ViewBag.PhongDaThue;
            ViewBag.TyLeLapDay = tongPhong > 0
                ? Math.Round((double)daThue / tongPhong * 100, 1)
                : 0;

            // Hợp đồng
            ViewBag.HdDangThue = db.HopDongs
                .Count(x => x.MaChuTro == maId
                          && x.TrangThaiHopDong == "DangThue"
                          && x.TrangThai == true);
            ViewBag.HdChoKy = db.HopDongs
                .Count(x => x.MaChuTro == maId
                          && x.TrangThaiHopDong == "ChoKy"
                          && x.TrangThai == true);

            // Hợp đồng sắp hết hạn (30 ngày tới)
            var ngayToi = DateTime.Today.AddDays(30);
            ViewBag.HdSapHet = db.HopDongs
                .Count(x => x.MaChuTro == maId
                          && x.TrangThaiHopDong == "DangThue"
                          && x.NgayKetThuc <= ngayToi
                          && x.TrangThai == true);

            // Hóa đơn
            ViewBag.HdChuaTT = db.HoaDons
                .Count(x => x.HopDong.MaChuTro == maId
                          && (x.TrangThaiHoaDon == "ChuaThanhToan"
                           || x.TrangThaiHoaDon == "QuaHan")
                          && x.TrangThai == true);
            ViewBag.ChoXacNhan = db.ThanhToans
                .Count(x => x.HoaDon.HopDong.MaChuTro == maId
                          && x.TrangThaiXacNhan == "ChoXacNhan"
                          && x.TrangThai == true);

            // Doanh thu tháng này
            ViewBag.DoanhThuThangNay = db.HoaDons
                .Where(x => x.HopDong.MaChuTro == maId
                          && x.TrangThaiHoaDon == "DaThanhToan"
                          && x.TrangThai == true
                          && x.ThangHoaDon.Year == nam
                          && x.ThangHoaDon.Month == DateTime.Now.Month)
                .Sum(x => (decimal?)x.TongTien) ?? 0;

            // Doanh thu theo tháng (năm hiện tại)
            var hdThang = db.HoaDons
                .Where(x => x.HopDong.MaChuTro == maId
                          && x.TrangThaiHoaDon == "DaThanhToan"
                          && x.TrangThai == true
                          && x.ThangHoaDon.Year == nam)
                .ToList()
                .GroupBy(x => x.ThangHoaDon.Month)
                .ToDictionary(g => g.Key,
                              g => g.Sum(x => x.TongTien));

            ViewBag.ThangLabels = string.Join(",",
                Enumerable.Range(1, 12)
                    .Select(m => "\"T" + m + "\""));
            ViewBag.ThangRevenue = string.Join(",",
                Enumerable.Range(1, 12)
                    .Select(m => hdThang.ContainsKey(m)
                                 ? hdThang[m] : 0));

            // Hợp đồng sắp hết hạn (danh sách) 
            ViewBag.DsHopDongSapHet = db.HopDongs
                .Include("PhongTro")
                .Include("PhongTro.ToaNha")
                .Include("NguoiDung1")
                .Where(x => x.MaChuTro == maId
                          && x.TrangThaiHopDong == "DangThue"
                          && x.NgayKetThuc <= ngayToi
                          && x.TrangThai == true)
                .OrderBy(x => x.NgayKetThuc)
                .Take(5)
                .ToList();

            //  Thanh toán chờ xác nhận (danh sách)
            ViewBag.DsChoXacNhan = db.ThanhToans
                .Include("HoaDon")
                .Include("HoaDon.HopDong")
                .Include("HoaDon.HopDong.PhongTro")
                .Where(x => x.HoaDon.HopDong.MaChuTro == maId
                          && x.TrangThaiXacNhan == "ChoXacNhan"
                          && x.TrangThai == true)
                .OrderByDescending(x => x.NgayTao)
                .Take(5)
                .ToList();

            ViewBag.NamHienTai = nam;
            return View();
        }
    }
}
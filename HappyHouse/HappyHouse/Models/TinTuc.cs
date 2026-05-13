using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace HappyHouse.Models
{
    public partial class TinTuc
    {
        public string MaTinTuc { get; set; }
        public string MaChuDe { get; set; }
        public string MaNguoiDang { get; set; }

        public string TieuDe { get; set; }

        [AllowHtml]
        public string NoiDung { get; set; }

        public string AnhDaiDien { get; set; }
        public string TomTat { get; set; }
        public int LuotXem { get; set; }
        public string TrangThaiDang { get; set; }
        public Nullable<System.DateTime> NgayDang { get; set; }
        public bool TrangThai { get; set; }
        public Nullable<System.DateTime> NgayTao { get; set; }
        public Nullable<System.DateTime> NgayCapNhat { get; set; }

        public virtual ChuDeTinTuc ChuDeTinTuc { get; set; }
        public virtual NguoiDung NguoiDung { get; set; }
    }
}

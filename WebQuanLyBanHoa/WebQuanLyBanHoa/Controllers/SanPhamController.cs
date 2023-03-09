using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebQuanLyBanHoa.Models;
using PagedList;

namespace WebQuanLyBanHoa.Controllers
{
    public class SanPhamController : Controller
    {
        // GET: SanPham
        QuanLyBanHoaDataContext db = new QuanLyBanHoaDataContext();
        [ChildActionOnly]
        public ActionResult SanPhamStyle1Partial()
        {
            return PartialView();
        }
        [ChildActionOnly]
        public ActionResult SanPhamStyle2Partial()
        {
            return PartialView();
        }
        public ActionResult SanPham(int? moi, int? page)
        {
            if(moi == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            /*load sản phẩm dựa theo tiêu chí mới, ưa chuộng và bán chạy*/
            var lstSP = db.SanPhams.Where(x => x.Moi == moi);
            if (lstSP.Count() == 0)
            {
                //Thông báo nếu như không có sản phẩm đó
                return HttpNotFound();
            }
            //if(Request.HttpMethod!="GET")
            //Thực hiện chức năng phân trang
            //Tạo biến số sản phẩm trong trang
            int PageSize = 6;
            //Tạo biến số trang hiện tại
            int PageNumber = (page ?? 1);
            ViewBag.Moi = moi;

            return View(lstSP.OrderBy(x=>x.MaSP).ToPagedList(PageNumber,PageSize));
        }
        public ActionResult XemChiTiet(int? id, string tensp)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            /*load sản phẩm dựa theo mã sp*/
            SanPham sp = db.SanPhams.SingleOrDefault(x=>x.MaSP == id && x.DaXoa == false);
            if (sp == null)
            {
                //Thông báo nếu như không có sản phẩm đó
                return HttpNotFound();
            }
            decimal gia = 200000;
            var lst = db.SanPhams.Where(x => x.DonGia <= gia && x.DaXoa == false);
            ViewBag.listC = lst;
            return View(sp);
        }
        public ActionResult TatCa(int? page)
        {
            var ls = db.SanPhams.Where(x => x.DaXoa == false);
            //Thực hiện chức năng phân trang
            //Tạo biến số sản phẩm trong trang
            int PageSize = 6;
            //Tạo biến số trang hiện tại
            int PageNumber = (page ?? 1);
            return View(ls.OrderBy(x=>x.MaSP).ToPagedList(PageNumber,PageSize));
        }
        //Giải phóng biến cho vùng nhớ
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (db != null)
                    db.Dispose();
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
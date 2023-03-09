using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebQuanLyBanHoa.Models;
using CaptchaMvc.HtmlHelpers;
using CaptchaMvc;
using System.Web.Security;
using Facebook;
using System.Configuration;


namespace WebQuanLyBanHoa.Controllers
{
    
    public class HomeController : Controller
    {
        // GET: Home
        QuanLyBanHoaDataContext db = new QuanLyBanHoaDataContext();
        public ActionResult Index()
        {
            
            //tạo viewbag lấy list sản phẩm mới
            //var lstMoi = db.SanPhams.Where(x => x.Moi == 1 && x.DaXoa == false);
            var lstMoi = (from x in db.SanPhams orderby x.MaSP descending select x).Take(8);
            
            ViewBag.lsMoi = lstMoi;
            //tạo viewbag list sản phẩm bán chạy
            
            //var lstBanChay = db.SanPhams.Where(x => x.Moi == 2 && x.DaXoa == false);

            var lstBanChay = (from sp in db.SanPhams
                         let tongSl = (from ctddh in db.ChiTietDonDatHangs
                                       join ddh in db.DonDatHangs on ctddh.MaDDH equals ddh.MaDDH
                                       where ctddh.MaSP == sp.MaSP
                                       select ctddh.SoLuong
                                       ).Sum()
                         where tongSl > 0
                         orderby tongSl descending
                         select sp ).Take(8);

            ViewBag.lsBC = lstBanChay;
            return View();
        }
        public ActionResult MenuPartial()
        {
            var lstSP = db.SanPhams;
            return PartialView(lstSP);
        }
        [HttpGet]
        public ActionResult DangKy()
        {
            return View();
        }
        [HttpPost]
        public ActionResult DangKy(ThanhVien tv)
        {
            //Kiểm tra captcha hợp lệ
            if(this.IsCaptchaValid("Captcha is not valid"))
            {
                if (ModelState.IsValid) { 
                ViewBag.ThongBao = "Đăng kí thành công";
                 //thêm khách hàng vào csdl
                 tv.MaLoaiTV = 4;
                db.ThanhViens.InsertOnSubmit(tv);
                db.SubmitChanges();
                }
                else
                {
                    ViewBag.ThongBao = "Đăng kí thất bại";
                }
                return View();
            }
            ViewBag.ThongBao = "Sai mã Captcha";

            return View();
        }

        [HttpPost]
        public ActionResult DangNhap(FormCollection f)
        {
            //string sTaiKhoan = f["txtTenDangNhap"].ToString();
            //string sMatKhau = f["txtMatKhau"].ToString();

            //ThanhVien tv = db.ThanhViens.SingleOrDefault(x => x.TaiKhoan == sTaiKhoan && x.MatKhau == sMatKhau);
            //if(tv != null)
            //{
            //    Session["TaiKhoan"] = tv;
            //    return Content("<script>window.location.reload();</script>");
            //}

            //return Content("Tài khoản hoặc mật khẩu không đúng!");
            string taikhoan = f["txtTenDangNhap"].ToString();
            string matkhau = f["txtMatKhau"].ToString();
            //Truy vấn kiểm tra đăng nhập lấy thông tin thành viên
            ThanhVien tv = db.ThanhViens.SingleOrDefault(x => x.TaiKhoan == taikhoan && x.MatKhau == matkhau);
            if (tv != null)
            {
                Session["TaiKhoan"] = tv;
                //lấy ra list quyền thành viên tương ứng với loại
                var lstQuyen = db.LoaiThanhVien_Quyens.Where(x=>x.MaLoaiTV==tv.MaLoaiTV);
                //Duyệt list quyền
                string Quyen = "";
                foreach(var item in lstQuyen)
                {
                    Quyen += item.Quyen.MaQuyen + ",";
                }
                Quyen = Quyen.Substring(0, Quyen.Length - 1);//Cắt đi dấu phẩy
                PhanQuyen(tv.TaiKhoan.ToString(), Quyen);
                
                return Content("<script>window.location.reload();</script>");
            }
            return Content("Tài khoản hoặc mật khẩu không đúng!");
        }
        private Uri RedirectUri
        {
            get
            {
                var uriBuilder = new UriBuilder(Request.Url);
                uriBuilder.Query = null;
                uriBuilder.Fragment = null;
                uriBuilder.Path = Url.Action("FacebookCallback");
                return uriBuilder.Uri;          
            }
        }
        public ActionResult LoginFacebook()
        {
            var fb = new FacebookClient();
            var loginUrl = fb.GetLoginUrl(new
            {
                client_id = ConfigurationManager.AppSettings["FbAppId"],
                client_secret = ConfigurationManager.AppSettings["FbAppSecret"],
                redirect_uri = RedirectUri.AbsoluteUri,
                response_type = "code",
                scope = "email",
            });

            return Redirect(loginUrl.AbsoluteUri);
           
        }

        public ActionResult FacebookCallback(string code)
        {
            var fb = new FacebookClient();
            dynamic result = fb.Post("oauth/access_token", new
            {
                client_id = ConfigurationManager.AppSettings["FbAppId"],
                client_secret = ConfigurationManager.AppSettings["FbAppSecret"],
                redirect_uri = RedirectUri.AbsoluteUri,
                code = code
            });
            var accessToken = result.access_token;
            if (!string.IsNullOrEmpty(accessToken))
            {
                fb.AccessToken = accessToken;
                // Get the user's information, like email, first name, middle name etc
                dynamic me =
               fb.Get("me?fields=first_name,middle_name,last_name,id,email");
                string email = me.email;
                string userName = me.email;
                string firstname = me.first_name;
                string middlename = me.middle_name;
                string lastname = me.last_name;

                var user = new ThanhVien();
                user.Email = email;
                user.TaiKhoan = email;

                user.HoTen = firstname + " " + middlename + " " + lastname;
                user.MaLoaiTV = 4;
                var resultInsert = new HomeController().InsertForFacebook(user);
                if (resultInsert > 0)
                {
                    var userSession = new ThanhVien();
                    userSession.TaiKhoan = user.TaiKhoan;
                    userSession.MaThanhVien = user.MaThanhVien;
                    userSession.HoTen = user.HoTen;
                    Session["TaiKhoan"] = userSession;

                }
               

            }
           
           return Redirect("/");
        }           

        public long InsertForFacebook(ThanhVien entity)
        {
            var user = db.ThanhViens.SingleOrDefault(x => x.TaiKhoan == entity.TaiKhoan);
            if (user == null)
            {
                db.ThanhViens.InsertOnSubmit(entity);
                db.SubmitChanges();
                return entity.MaThanhVien;
            }
            else
            {
                return user.MaThanhVien;
            }
        }

        public void PhanQuyen(string TaiKhoan, string Quyen)
        {
            FormsAuthentication.Initialize();
            var ticket = new FormsAuthenticationTicket(1,
                                TaiKhoan,//user
                                DateTime.Now,//begin
                                DateTime.Now.AddHours(3),//time out
                                false,//remember?
                                Quyen,
                                FormsAuthentication.FormsCookiePath);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName,
                                FormsAuthentication.Encrypt(ticket));
            if (ticket.IsPersistent) cookie.Expires = ticket.Expiration;

            Response.Cookies.Add(cookie);
        }
        //Tạo trang ngăn chặn quyền truy cập
        public ActionResult LoiPhanQuyen()
        {
            return View();
        }
        public ActionResult DangXuat()
        {
            Session["TaiKhoan"] = null;
            FormsAuthentication.SignOut();
            return RedirectToAction("Index");
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
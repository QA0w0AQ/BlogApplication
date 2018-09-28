using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using BlogApplication.Helpers;
using BlogApplication.Models;
using Microsoft.AspNet.Identity;
using PagedList;
using PagedList.Mvc;

namespace BlogApplication.Controllers
{
    //[RequireHttps]
    public class BlogPostsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: BlogPosts
        public ActionResult Index(int? page, string searchStr)
        {

            //var emailService = new PersonalEmailService();
            //var mailMessage = new MailMessage(
            //    WebConfigurationManager.AppSettings["username"],
            //    WebConfigurationManager.AppSettings["emailto"]);
            //mailMessage.Body = "This is a test e-mail.";
            //mailMessage.Subject = "Test e-mail";
            //emailService.Send(mailMessage);


            ViewBag.Search = searchStr;

            int pageSize = 2; // display three blog posts at a time on this page
            int pageNumber = (page ?? 1);

            var PostQuery = db.Posts.OrderBy(p => p.Created).AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchStr))
            {
                PostQuery = PostQuery
                    .Where(p => p.Title.Contains(searchStr) || 
                                p.Body.Contains(searchStr)  ||
                                p.Slug.Contains(searchStr)  ||
                                p.Comments.Any(b=>b.Body.Contains(searchStr)  || 
                                b.Author.Email.Contains(searchStr)  ||
                                b.Author.DisplayName.Contains(searchStr)  ||
                                b.Author.FirstName.Contains(searchStr)  ||
                                b.Author.LastName.Contains(searchStr))                                
                                );                              
            }

            var PostList = PostQuery.ToPagedList(pageNumber, pageSize);
            return View(PostList);
        }

        // GET: BlogPosts/Details/5
        public ActionResult Details(string Slug)
        {
            if (Slug == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.Posts
                .Include(p => p.Comments.Select(t => t.Author))
                .Where(p => p.Slug == Slug)
                .FirstOrDefault();

            if (blogPost == null)
            {
                return HttpNotFound();
            }
            return View("Details",blogPost);
        }

        [HttpPost]
        public ActionResult Details(string Slug, string body)
        {
            if (Slug == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var blogPost = db.Posts
                .Where(p => p.Slug == Slug)
                .FirstOrDefault();

            if (blogPost == null)
            {
                return HttpNotFound();
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                ViewBag.ErrorMessage = "Comment is empty!";
                return View("Details", blogPost);
            }

            var comment = new Comment();
            comment.AuthorId = User.Identity.GetUserId();
            comment.PostId = blogPost.Id;
            comment.Created = DateTime.Now;
            comment.Body = body;

            db.Comments.Add(comment);
            db.SaveChanges();

            return RedirectToAction("DetailsSlug", new {  Slug });
        }
        // GET: BlogPosts/Create
        [Authorize(Roles = "Admin,Moderator")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: BlogPosts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Moderator")]
        public ActionResult Create([Bind(Include = "Id,Title,Body,MediaURL,Published")] BlogPost blogPost, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                var Slug = StringUtilities.URLFriendly(blogPost.Title);

                if (String.IsNullOrWhiteSpace(Slug))
                {   
                    ModelState.AddModelError("Title", "Invalid title");
                    return View(blogPost);
                }
                if (db.Posts.Any(p => p.Title == blogPost.Title))
                {
                    ModelState.AddModelError("Title", "This Title already exist");
                    return View(blogPost);
                }
                if (ImageUploadValidator.IsWebFriendlyImage(image))
                {
                    var fileName = Path.GetFileName(image.FileName);
                    image.SaveAs(Path.Combine(Server.MapPath("~/Image/"), fileName));
                    blogPost.MediaURL = "/Image/" + fileName;
                }

                blogPost.Slug = Slug;
                blogPost.Created = DateTimeOffset.Now;

                db.Posts.Add(blogPost);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(blogPost);
        }

        // GET: BlogPosts/Edit/5

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.Posts.Find(id);
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            return View(blogPost);
        }

        // POST: BlogPosts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        
        public ActionResult Edit([Bind(Include = "Id,Title,Slug,Body,MediaURL,Published")] BlogPost blogPost, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                var blog = db.Posts.Where(p => p.Id == blogPost.Id).FirstOrDefault();

                blog.Body = blogPost.Body;                
                blog.Published = blogPost.Published;
                blog.Slug = blogPost.Slug;
                blog.Title = blogPost.Title;
                blog.Updated = DateTime.Now;
                db.SaveChanges();
                if (ImageUploadValidator.IsWebFriendlyImage(image))
                {
                    var fileName2 = Path.GetFileName(image.FileName);
                    image.SaveAs(Path.Combine(Server.MapPath("~/Image/"), fileName2));
                    blogPost.MediaURL = "/Image/" + fileName2;
                }
                blog.MediaURL = blogPost.MediaURL;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(blogPost);
        }

        // GET: BlogPosts/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.Posts.Find(id);
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            return View(blogPost);
        }

        // POST: BlogPosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            BlogPost blogPost = db.Posts.Find(id);
            db.Posts.Remove(blogPost);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize]
        public ActionResult CreateComment(string slug, string body)
            {
            if (slug == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var blogComment = db.Posts
               .Where(p => p.Slug == slug)
               .FirstOrDefault();


            if (blogComment == null)
            {
                return HttpNotFound();
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                TempData["ErrorMessage"] = "Comment is empty!";
                return View("Details", new { slug });
            }
            var comment = new Comment();
            comment.AuthorId = User.Identity.GetUserId();
            comment.PostId = blogComment.Id;
            comment.Created = DateTime.Now;
            comment.Body = body;

            db.Comments.Add(comment);
            db.SaveChanges();
            return RedirectToAction("Details", new { slug });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

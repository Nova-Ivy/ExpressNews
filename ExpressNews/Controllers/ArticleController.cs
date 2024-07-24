﻿using Azure.Storage.Blobs.Models;
using ExpressNews.Models.Database;
using ExpressNews.Models.ViewModel;
using ExpressNews.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ExpressNews.Models;

namespace ExpressNews.Controllers
{
    public class ArticleController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly IConfiguration _configuration;

        public ArticleController(IArticleService articleService, IConfiguration configuration)
        {
            _articleService = articleService;
            _configuration = configuration;
        }
        public IActionResult Index()
        {

            return View(_articleService.GetArticles());
        }

        public IActionResult SingleArticle(int id)
        {
            ArticleVM obj = new ArticleVM();
            obj.ArticleObj = _articleService.GetArticleById(id);

            return View(obj);
        }
        //public IActionResult AddArticle()
        //{
        //    return View();
        //}


        //[HttpPost]
        //public IActionResult AddArticle(Article newArticle)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _articleService.AddArticle(newArticle, newArticle.FormImages);
        //        return RedirectToAction("Index");
        //    }
        //    return View(newArticle);


        //}

        public IActionResult ArticleAdd()
        {
            return View();
        }


        [HttpPost]
        public IActionResult ArticleAdd(Article newArticle)
        {
            if (newArticle.FormImages.Count > 0)
            {
                _articleService.UploadFilesToContainer(newArticle);
            }
            _articleService.AddArticle(newArticle, newArticle.FormImages);

            return RedirectToAction("Index");
        }

        public IActionResult ArticleEdit(int id)
        {
            var newArticle = _articleService.GetArticleById(id);

            return View(newArticle);
        }

        [HttpPost]
        public async Task<IActionResult> ArticleEdit(int id, Article article)
        {
            article.DateStamp = DateTime.Now;

            if (id != article.Id)
            {
                return NotFound();
            }

            if (article.FormImages.Count > 0)
            {
                _articleService.UploadFilesToContainer(article);
            }

            _articleService.UpdateArticle(article);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Submit(int id)
        {
            var article = _articleService.GetArticleById(id);
            if (article == null)
            {
                return NotFound();
            }

            article.Status = "Submitted";
            _articleService.SubmitArticle(article);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult UpdateViewCount(int id, int viewCount)
        {
            var article = _articleService.GetArticleById(id);
            if (article == null)
            {
                return NotFound();
            }
            int currentCount = 0;
            if (article.Views != null)
                currentCount = Convert.ToInt32(article.Views);

            article.Views = currentCount + viewCount;
            var newArticle = _articleService.UpdateArticleValues(article);

            //HttpContext.Session.SetInt32("ViewCount", Convert.ToInt32(newArticle.Views));

            return Json(new { success = true });
        }

        public IActionResult UpdateLikeDislineCount(int id, int count, string type)
        {
            var article = _articleService.GetArticleById(id);
            if (article == null)
            {
                return NotFound();
            }

            if (type == "Like")
            {
               article.Likes = count;
            }
            else
            {
                article.DisLikes = count;
            }
            

            var newArticle = _articleService.UpdateArticleValues(article);

            //HttpContext.Session.SetInt32("ViewCount", Convert.ToInt32(newArticle.Views));

            return Json(new { success = true });
        }


        public IActionResult Approve(int id)
        {
            var article = _articleService.GetArticleById(id);
            if (article == null)
            {
                return NotFound();
            }

            article.Status = "Approved";
            _articleService.ApproveArticle(article);
            return RedirectToAction(nameof(Index));

        }

        public IActionResult Reject(int id)
        {
            var article = _articleService.GetArticleById(id);
            if (article == null)
            {
                return NotFound();
            }

            article.Status = "Rejected";
            _articleService.RejectArticle(article);
            return RedirectToAction(nameof(Index));

        }

        public IActionResult Details(int id)
        {
            try
            {
                var article = _articleService.GetArticleDetails(id);
                return View(article);
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }

        public IActionResult Delete(int id)
        {
            try
            {
                var article = _articleService.GetArticleDetails(id);
                return View(article);
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                _articleService.DeleteArticle(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            { 
                return BadRequest();
            }
            

        }

        public IActionResult Search(string query)
        {
            var results = _articleService.SearchArticles(query);
            return View(results);
        }

        public IActionResult ArchiveNews()
        {
            var archivenews = _articleService.ArchiveArticles();
            return View(archivenews);
        }

    }
}

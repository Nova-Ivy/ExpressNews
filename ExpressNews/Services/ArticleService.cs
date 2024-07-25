﻿using ExpressNews.Data;
using ExpressNews.Models.Database;
using Microsoft.Identity.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using ExpressNews.Models;
using Microsoft.AspNetCore.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using ExpressNews.ViewComponents;
using System.Linq;
using ExpressNews.Models;

namespace ExpressNews.Services
{
    public class ArticleService : IArticleService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManagement;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ArticleService(ApplicationDbContext db, IConfiguration configuration, UserManager<User> userManagement, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _configuration = configuration;
            _userManagement = userManagement;
            _httpContextAccessor = httpContextAccessor;
        }



        public void AddArticle(Article newArticle, List<IFormFile> formImages)
        {
            
                newArticle.DateStamp = DateTime.Now;
                string userName = _httpContextAccessor.HttpContext.Session.GetString("UserName");
                newArticle.Status = "Draft";
                newArticle.UserName = userName;
                

                //newArticle.ImageLink = "https://dummyimage.com/600x400/000/fff";

            
            _db.Articles.Add(newArticle);
            _db.SaveChanges();


        }

        private string SaveImageAndGetLink(IFormFile formImage)
        {

            var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Image");
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }


            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(formImage.FileName);
            var filePath = Path.Combine(imagesPath, uniqueFileName);


            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                formImage.CopyTo(stream);
            }


            return "/Image/" + uniqueFileName;
        }


        public List<Article> GetArticles()
        {

            string role = _httpContextAccessor.HttpContext.Session.GetString("Role");
            string userName = _httpContextAccessor.HttpContext.Session.GetString("UserName");

            if (role == "Editor")
            {
                return _db.Articles.Where(a => a.Status == "Submitted").OrderByDescending(a => a.DateStamp).ToList();
            }
            else if (role == "Journalist")
            {
                return _db.Articles.Where(a => (a.Status == "Draft" || a.Status == "Rejected") && a.UserName == userName).OrderByDescending(a => a.DateStamp).ToList();
            }
            else
            {
                return _db.Articles.Where(a => a.Status == "Approved").OrderByDescending(a => a.DateStamp).ToList();
            }

        }

        public Article UploadFilesToContainer(Article article)
        {
            BlobContainerClient blobServiceClient = new BlobServiceClient(
                                   _configuration["AzureWebJobsStorage"]).GetBlobContainerClient("newscontainer");
            foreach (var file in article.FormImages)
            {
                BlobClient blobClient = blobServiceClient.GetBlobClient(file.FileName);
                using (var stream = file.OpenReadStream())
                {
                    blobClient.Upload(stream);
                }
                article.ImageLink = blobClient.Uri.AbsoluteUri;
                article.FileName = file.FileName;
            }
            return article;


        }

        public void UpdateArticle(Article article)
        {
            var oldArticle = _db.Articles.AsNoTracking().FirstOrDefault(a => a.Id == article.Id);

            if (article.ImageLink == null)
            {
                article.ImageLink = oldArticle.ImageLink;
                article.FileName = oldArticle.FileName;
            }

            article.DateStamp = DateTime.Now;
            string userName = _httpContextAccessor.HttpContext.Session.GetString("UserName");
            string role = _httpContextAccessor.HttpContext.Session.GetString("Role");
             if (role == "Editor")
                article.Status = "Submitted";
            else
                article.Status = "Draft"; 
            article.UserName = userName;

            

            _db.Update(article);
            _db.SaveChanges();
        }

        public Article GetArticleById(int id)
        {

            var article = _db.Articles.FirstOrDefault(a => a.Id == id);
            return article;
        }

        public Article GetBreakingNews()
        {
            Article article = new Article();
            article = _db.Articles.Where(a => a.Status == "Approved").FirstOrDefault(a => a.IsBreaking == true);
            return article;
        }
        public Article GetArticleForFrontPage()
        {
            Article article = new Article();
            article = _db.Articles.Where(a => a.Status == "Approved").FirstOrDefault(a => a.IsBreaking == false);
            return article;
        }



        public void SubmitArticle(Article article)
        {
            article.DateStamp = DateTime.Now;

            //string userFirstName = _httpContextAccessor.HttpContext.Session.GetString("UserFirstName");
            //string userLastName = _httpContextAccessor.HttpContext.Session.GetString("UserLastName");
            //article.UserName = userFirstName + " " + userLastName;

            ////article.ImageLink = "https://dummyimage.com/600x400/000/fff";
            _db.Update(article);
            _db.SaveChanges();
        }

        public void ApproveArticle(Article article)
        {
            article.DateStamp = DateTime.Now;

            //string userFirstName = _httpContextAccessor.HttpContext.Session.GetString("UserFirstName");
            //string userLastName = _httpContextAccessor.HttpContext.Session.GetString("UserLastName");
            //article.UserName = userFirstName + " " + userLastName;

            ////article.ImageLink = "https://dummyimage.com/600x400/000/fff";
            _db.Update(article);
            _db.SaveChanges();
        }

        public void RejectArticle(Article article)
        {
            article.DateStamp = DateTime.Now;

           // string userFirstName = _httpContextAccessor.HttpContext.Session.GetString("UserFirstName");
           // string userLastName = _httpContextAccessor.HttpContext.Session.GetString("UserLastName");
           // article.UserName = userFirstName + " " + userLastName;

           //// article.ImageLink = "https://dummyimage.com/600x400/000/fff";
            _db.Update(article);
            _db.SaveChanges();
        }
        public Article UpdateArticleValues(Article article)
        {
            _db.Update(article);
            _db.SaveChanges();

            return article;

        }

        public Article GetArticleDetails(int id)
        {
            var article = _db.Articles
                        .FirstOrDefault(a => a.Id == id);

            if (article == null)
            {
                throw new Exception("Article not found");
            }

            return article;
        }

        public void DeleteArticle(int id)
        {
            var article = _db.Articles.Find(id);
            if (article != null)
            {
                _db.Articles.Remove(article);
                _db.SaveChanges();
            }
            else
            {
                throw new Exception("Article not found");
            }
        }

        public List<Article> GetLatestArticles(int count)
        {
            var LatestArticles = _db.Articles.Where(a => a.Status != "Archive" && a.Status == "Approved").OrderByDescending(a => a.DateStamp).Take(count).ToList();
            return LatestArticles;

        }

        public List<Article> GetPopularArticles(int count)
        {
            var popularArticles = _db.Articles.Where(a => a.Status != "Archive" && a.Status == "Approved").OrderByDescending(a => a.Views).Take(count).ToList();
            return popularArticles;

        }

        public List<Article> GetArticleByCategory(string category)
        {

            var article = _db.Articles.Where(a => ( a.Category1 == category || a.Category2 == category|| a.Category3 == category) && a.Status != "Archive" && a.Status == "Approved").OrderByDescending(a => a.DateStamp).ToList();
            return article;
        }

        public List<Article> SearchArticles(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return new List<Article>();
            }

            return _db.Articles
                           .Where(a => a.HeadLine.Contains(query)
                                    || a.Content.Contains(query)
                                    || a.Category1.Contains(query)
                                    || a.Category2.Contains(query)
                                    || a.Category3.Contains(query))
                           .ToList();
        }
        public List<Article> EditorsChoiceArticles(int count)
        {
            var editorsChoice = _db.Articles.Where(a => a.IsEditorChoice == true).Take(count).ToList();
            return editorsChoice;
        }

        public Dictionary<string, int> GetArticleCategoryCounts()
        {
            var articles = _db.Articles.ToList();

            var categoryCounts = new Dictionary<string, int>();

            foreach (var article in articles)
            {
                if (!string.IsNullOrEmpty(article.Category1) && article.Category1 != "N/A")
                {
                    if (categoryCounts.ContainsKey(article.Category1))
                        categoryCounts[article.Category1]++;
                    else
                        categoryCounts[article.Category1] = 1;
                }

                if (!string.IsNullOrEmpty(article.Category2) && article.Category2 != "N/A")
                {
                    if (categoryCounts.ContainsKey(article.Category2))
                        categoryCounts[article.Category2]++;
                    else
                        categoryCounts[article.Category2] = 1;
                }

                if (!string.IsNullOrEmpty(article.Category3) && article.Category3 != "N/A")
                {
                    if (categoryCounts.ContainsKey(article.Category3))
                        categoryCounts[article.Category3]++;
                    else
                        categoryCounts[article.Category3] = 1;
                }
            }

            return categoryCounts;
        }

        public List<Article> MostViewedArticles()
        {
            var mostViewdeArticles = _db.Articles.OrderByDescending(a => a.Views).Take(5).ToList();
            return mostViewdeArticles;

        }
        public List<Article> MostLikedArticles()
        {
            var mostLikedArticles = _db.Articles.OrderByDescending(a => a.Likes).Take(5).ToList();
            return mostLikedArticles;

        }
        public List<Article> MostDisLikedArticles()
        {
            var mostDislikedArticles = _db.Articles.OrderByDescending(a => a.DisLikes).Take(5).ToList();
            return mostDislikedArticles;

        }


        public List<OldArticle> ArchiveArticles()
        {
            var archiveArticle = _db.Articles
                .Where(a => a.Status == "Archive")
                .OrderByDescending(a => a.DateStamp)
                //.Take(5)
                .Select(a => new OldArticle { Id = a.Id, HeadLine = a.HeadLine, ContentSummary = a.ContentSummary, ImageLink = a.ImageLink, DateStamp = a.DateStamp })
                .ToList();

            return archiveArticle;
        }
    }

    
}

using System.Linq;
using System.Text.RegularExpressions;
using Conduit.Domain;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Articles
{
    public static class ArticleExtensions
    {
        public static IQueryable<Article> GetAllData(this DbSet<Article> articles)
        {
            return articles
                .Include(x => x.Author)
                .Include(x => x.ArticleFavorites)
                .Include(x => x.ArticleTags)
                .AsNoTracking();
        }

        // https://stackoverflow.com/questions/2920744/url-slugify-algorithm-in-c
        public static string? GenerateSlug(this Article article)
        {
            var phrase = article.Title;
            if (phrase is null)
            {
                return null;
            }

            var str = phrase.ToLowerInvariant();
            // invalid chars
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // convert multiple spaces into one space
            str = Regex.Replace(str, @"\s+", " ").Trim();
            // cut and trim
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
            str = Regex.Replace(str, @"\s", "-"); // hyphens
            return str + "-" + article.ArticleId;
        }

        public static void AddIsFavoriteToggleInPlace(this Article article, Person? currentPerson)
        {
            article.Favorited =
                article.ArticleFavorites.Any(favorite => favorite.PersonId == currentPerson?.PersonId);
        }

        public static void AddIsFollowingAuthorInPlace(this Article article, bool following)
        {
            article.Author!.Following = following;
        }
    }
}

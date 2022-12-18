using System.Collections.Generic;
using System.Linq;
using Conduit.Domain;
using Conduit.Extensions;
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

        public static void AddIsFavoriteToggleInPlace(this Article article, Person currentPerson)
        {
            article.Favorited =
                article.ArticleFavorites.Any(favorite => favorite.PersonId == currentPerson.PersonId);
        }
    }
}

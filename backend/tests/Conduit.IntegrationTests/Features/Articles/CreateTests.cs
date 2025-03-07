using System.Linq;
using System.Threading.Tasks;
using Conduit.Features.Articles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Conduit.IntegrationTests.Features.Articles
{
    public class CreateTests : SliceFixture
    {
        [Fact]
        public async Task Expect_Create_Article()
        {
            var command = new Create.Command(new Create.ArticleData()
            {
                Title = "Test article dsergiu77",
                Description = "Description of the test article",
                Body = "Body of the test article",
                TagList = new string[] { "tag1", "tag2" }
            });

            var article = await ArticleHelpers.CreateArticle(this, command);

            Assert.NotNull(article);
            Assert.Equal(article.Title, command.Article.Title);
            Assert.Equal(article.TagList.Count(), command.Article.TagList!.Count());

            var dbArticleTags = await ExecuteDbContextAsync(
                db => db.ArticleTags.Where(a => a.ArticleId == article.ArticleId)
                    .ToListAsync()
            );
            Assert.True(dbArticleTags.Count == 2);

            var dbTags = await ExecuteDbContextAsync(
                db => db.Tags.Where(t => article.TagList.Contains(t.TagId))
                    .ToListAsync()
            );
            Assert.True(dbTags.Count == 2);
        }
    }
}

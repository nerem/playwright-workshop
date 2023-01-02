using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Features.Tags;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Articles
{
    public class Edit
    {
        public class ArticleData
        {
            public string? Title { get; set; }

            public string? Description { get; set; }

            public string? Body { get; set; }

            public string[]? TagList { get; set; }
        }
        public class ArticleDataValidator : AbstractValidator<ArticleData>
        {
            public ArticleDataValidator()
            {
                RuleFor(x => x.Title).NotNull().NotEmpty();
                RuleFor(x => x.Description).NotNull().NotEmpty();
                RuleFor(x => x.Body).NotNull().NotEmpty();
            }
        }

        public record Command(Model Model, string Slug) : IRequest<ArticleEnvelope>;

        public record Model(ArticleData Article);

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator() => RuleFor(x => x.Model.Article).NotNull().SetValidator(new ArticleDataValidator());
        }

        public class ModelValidator : AbstractValidator<Model>
        {
            public ModelValidator() => RuleFor(x => x.Article).NotNull().SetValidator(new ArticleDataValidator());
        }

        public class Handler : IRequestHandler<Command, ArticleEnvelope>
        {
            private readonly ConduitContext _context;
            private readonly TagsCleanup _tagsCleanup;

            public Handler(ConduitContext context, TagsCleanup tagsService)
            {
                _context = context;
                _tagsCleanup = tagsService;
            }

            public async Task<ArticleEnvelope> Handle(Command message, CancellationToken cancellationToken)
            {
                var article = await _context.Articles
                    .Include(x => x.ArticleTags) // include also the article tags since they also need to be updated
                    .Where(x => x.Slug == message.Slug)
                    .FirstOrDefaultAsync(cancellationToken);

                if (article == null)
                {
                    throw new RestException(HttpStatusCode.NotFound, new { Article = Constants.NOT_FOUND });
                }

                article.Description = message.Model.Article.Description ?? article.Description;
                article.Body = message.Model.Article.Body ?? article.Body;
                article.Title = message.Model.Article.Title ?? article.Title;
                article.Slug = article.GenerateSlug();

                // list of currently saved article tags for the given article
                var articleTagList = (message.Model.Article.TagList ?? Enumerable.Empty<string>());

                var articleTagsToCreate = GetArticleTagsToCreate(article, articleTagList);
                var articleTagsToDelete = GetArticleTagsToDelete(article, articleTagList);

                if (_context.ChangeTracker.Entries().First(x => x.Entity == article).State == EntityState.Modified
                    || articleTagsToCreate.Any() || articleTagsToDelete.Any())
                {
                    article.UpdatedAt = DateTime.UtcNow;
                }

                // ensure context is tracking any already existing tags that are about to be created due to adding an ArticleTag:
                // if the ArticleTag as Join Table references a Tag that already exists before, the Tag would be created
                // anew (PK violation) without attaching the Tags
                var allTagsInDb = await _context.Tags.AsNoTracking().ToListAsync(cancellationToken);
                var tagsThatAlreadyExistInDbAndAreAddedToArticleTags = articleTagsToCreate.Where(x => x.Tag is not null)
                    .Where(x => allTagsInDb.Any(tagInDb => tagInDb.TagId == x.TagId)).Select(a => a.Tag!).ToArray();
                _context.Tags.AttachRange(tagsThatAlreadyExistInDbAndAreAddedToArticleTags);

                // add the new article tags
                await _context.ArticleTags.AddRangeAsync(articleTagsToCreate, cancellationToken);

                // delete the tags that do not exist anymore
                _context.ArticleTags.RemoveRange(articleTagsToDelete);

                await _context.SaveChangesAsync(cancellationToken);

                await _tagsCleanup.RemoveAllTagsThatAreNotUsedInAnyArticle();

                return new ArticleEnvelope(await _context.Articles.GetAllData()
                    .Where(x => x.Slug == article.Slug)
                    .SingleAsync(cancellationToken));
            }

            /// <summary>
            /// check which article tags need to be added
            /// </summary>
            static List<ArticleTag> GetArticleTagsToCreate(Article article, IEnumerable<string> articleTagList)
            {
                var articleTagsToCreate = new List<ArticleTag>();
                foreach (var tag in articleTagList)
                {
                    var at = article.ArticleTags?.FirstOrDefault(t => t.TagId == tag);
                    if (at == null)
                    {
                        at = new ArticleTag()
                        {
                            Article = article,
                            ArticleId = article.ArticleId,
                            Tag = new Tag() { TagId = tag },
                            TagId = tag
                        };
                        articleTagsToCreate.Add(at);
                    }
                }

                return articleTagsToCreate;
            }

            /// <summary>
            /// check which article tags need to be deleted
            /// </summary>
            static List<ArticleTag> GetArticleTagsToDelete(Article article, IEnumerable<string> articleTagList)
            {
                var articleTagsToDelete = new List<ArticleTag>();
                foreach (var tag in article.ArticleTags)
                {
                    var at = articleTagList.FirstOrDefault(t => t == tag.TagId);
                    if (at == null)
                    {
                        articleTagsToDelete.Add(tag);
                    }
                }

                return articleTagsToDelete;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Features.Tags;
using Conduit.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Articles
{
    public class Create
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

        public record Command(ArticleData Article) : IRequest<ArticleEnvelope>;

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Article).NotNull().SetValidator(new ArticleDataValidator());
            }
        }

        public class Handler : IRequestHandler<Command, ArticleEnvelope>
        {
            private readonly ConduitContext _context;
            private readonly ICurrentUserAccessor _currentUserAccessor;
            private readonly TagsCleanup _tagsCleanup;

            public Handler(ConduitContext context, ICurrentUserAccessor currentUserAccessor, TagsCleanup tagsCleanup)
            {
                _context = context;
                _currentUserAccessor = currentUserAccessor;
                _tagsCleanup = tagsCleanup;
            }

            public async Task<ArticleEnvelope> Handle(Command message, CancellationToken cancellationToken)
            {
                var author = await _context.Persons.FirstAsync(x => x.Username == _currentUserAccessor.GetCurrentUsername(), cancellationToken);
                var tags = new List<Tag>();
                foreach (var tag in (message.Article.TagList ?? Enumerable.Empty<string>()))
                {
                    var t = await _context.Tags.FindAsync(tag);
                    if (t == null)
                    {
                        t = new Tag()
                        {
                            TagId = tag
                        };
                        await _context.Tags.AddAsync(t, cancellationToken);
                        //save immediately for reuse
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                    tags.Add(t);
                }

                var article = new Article()
                {
                    Author = author,
                    Body = message.Article.Body,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Description = message.Article.Description,
                    Title = message.Article.Title,
                };
                await _context.Articles.AddAsync(article, cancellationToken);

                await _context.ArticleTags.AddRangeAsync(tags.Select(x => new ArticleTag()
                {
                    Article = article,
                    Tag = x
                }), cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                // the article Id of a new article is handled by the db and only known after it is inserted already
                article.Slug = article.GenerateSlug();

                await _context.SaveChangesAsync(cancellationToken);

                // not strictly necessary when creating a new article, but one redundant clean up does not hurt
                await _tagsCleanup.RemoveAllTagsThatAreNotUsedInAnyArticle();

                return new ArticleEnvelope(article);
            }
        }
    }
}

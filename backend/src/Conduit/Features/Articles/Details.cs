using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Features.Profiles;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Articles
{
    public class Details
    {
        public record Query(string Slug) : IRequest<ArticleEnvelope>;

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.Slug).NotNull().NotEmpty();
            }
        }

        public class QueryHandler : IRequestHandler<Query, ArticleEnvelope>
        {
            private readonly ConduitContext _context;
            private readonly ICurrentUserAccessor _currentUserAccessor;
            private readonly IProfileReader _profileReader;

            public QueryHandler(ConduitContext context, ICurrentUserAccessor currentUserAccessor,
                IProfileReader profileReader)
            {
                _context = context;
                _currentUserAccessor = currentUserAccessor;
                _profileReader = profileReader;
            }

            public async Task<ArticleEnvelope> Handle(Query message, CancellationToken cancellationToken)
            {
                var article = await _context.Articles.GetAllData()
                    .FirstOrDefaultAsync(x => x.Slug == message.Slug, cancellationToken);

                if (article == null)
                {
                    throw new RestException(HttpStatusCode.NotFound, new { Article = Constants.NOT_FOUND });
                }

                if (_currentUserAccessor.GetCurrentUsername() is { } currentUserName)
                {
                    var currentPerson = await _context.Persons.AsNoTracking()
                        .SingleOrDefaultAsync(x => x.Username == currentUserName, cancellationToken);
                    article.AddIsFavoriteToggleInPlace(currentPerson);
                    article.AddIsFollowingAuthorInPlace(await _profileReader.IsFollowedByCurrentUser(currentUserName,
                        article.Author!.PersonId, cancellationToken));
                }

                return new ArticleEnvelope(article);
            }
        }
    }
}

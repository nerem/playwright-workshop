using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Features.Tags;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Articles
{
    public class Delete
    {
        public record Command(string Slug) : IRequest;

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Slug).NotNull().NotEmpty();
            }
        }

        public class QueryHandler : IRequestHandler<Command>
        {
            private readonly ConduitContext _context;
            private readonly TagsCleanup _tagsCleanup;

            public QueryHandler(ConduitContext context, TagsCleanup tagsCleanup)
            {
                _context = context;
                _tagsCleanup = tagsCleanup;
            }

            public async Task<Unit> Handle(Command message, CancellationToken cancellationToken)
            {
                var article = await _context.Articles
                    .FirstOrDefaultAsync(x => x.Slug == message.Slug, cancellationToken);

                if (article == null)
                {
                    throw new RestException(HttpStatusCode.NotFound, new { Article = Constants.NOT_FOUND });
                }

                _context.Articles.Remove(article);
                await _context.SaveChangesAsync(cancellationToken);

                await _tagsCleanup.RemoveAllTagsThatAreNotUsedInAnyArticle();

                return Unit.Value;
            }
        }
    }
}

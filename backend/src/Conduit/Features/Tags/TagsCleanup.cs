using System.Linq;
using System.Threading.Tasks;
using Conduit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Tags;

public class TagsCleanup
{
    private readonly ConduitContext _context;

    public TagsCleanup(ConduitContext context) => _context = context;

    public async Task RemoveAllTagsThatAreNotUsedInAnyArticle()
    {
        var tagsThatAreNotUsedByAnyArticleAnymore = await _context.Tags.Where(t => t.ArticleTags.Count == 0).ToListAsync();

        if (!tagsThatAreNotUsedByAnyArticleAnymore.Any())
        {
            return;
        }

        _context.Tags.RemoveRange(tagsThatAreNotUsedByAnyArticleAnymore);
        await _context.SaveChangesAsync();
    }
}

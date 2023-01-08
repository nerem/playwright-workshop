using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Profiles
{
    public class ProfileReader : IProfileReader
    {
        private readonly ConduitContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IMapper _mapper;

        public ProfileReader(ConduitContext context, ICurrentUserAccessor currentUserAccessor, IMapper mapper)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _mapper = mapper;
        }

        public async Task<ProfileEnvelope> ReadProfile(string username, CancellationToken cancellationToken)
        {
            var person = await _context.Persons.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

            if (person == null)
            {
                throw new RestException(HttpStatusCode.NotFound, new { User = Constants.NOT_FOUND });
            }

            var profile = _mapper.Map<Domain.Person, Profile>(person);

            var currentUserName = _currentUserAccessor.GetCurrentUsername();
            profile.IsFollowed = currentUserName != null && await IsFollowedByCurrentUser(currentUserName, person.PersonId, cancellationToken);

            return new ProfileEnvelope(profile);
        }

        public async Task<bool> IsFollowedByCurrentUser(string currentUserName,
            int otherPersonId, CancellationToken cancellationToken)
        {
            var currentPerson = await _context.Persons
                .Include(x => x.FollowingPersons)
                .Include(x => x.FollowerPersons)
                .FirstOrDefaultAsync(x => x.Username == currentUserName, cancellationToken);

            if (currentPerson.FollowingPersons.Any(x => x.TargetId == otherPersonId))
            {
                return true;
            }

            return false;
        }
    }
}

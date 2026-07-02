using Core.Entity;
using Core.Repository;

namespace Infrastructure.Repository;

public class DonationRepository : EfRepository<Donation>, IDonationRepository
{
    public DonationRepository(ApplicationDbContext context) : base(context)
    {
        
    }
}
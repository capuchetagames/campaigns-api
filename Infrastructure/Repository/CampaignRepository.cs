using Core.Entity;
using Core.Repository;

namespace Infrastructure.Repository;

public class CampaignRepository : EfRepository<Campaign>, ICampaignRepository
{
    public CampaignRepository(ApplicationDbContext context) : base(context)
    {
    }
}
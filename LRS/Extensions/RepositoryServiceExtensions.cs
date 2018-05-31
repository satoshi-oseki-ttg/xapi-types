using bracken_lrs.Services;

namespace bracken_lrs.Extensions
{
    public static class RepositoryServiceExtensions
    {
        public static IRepositoryService SetDb(this IRepositoryService service, string dbName)
        {
            service.Db = service.Client.GetDatabase(dbName);

            return service;
        }       
    }
}
    
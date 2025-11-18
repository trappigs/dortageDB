using dortageDB.Entities;

namespace dortageDB.Services
{
    public interface ISeoService
    {
        Task<SeoSetting?> GetSeoSettingAsync(string pagePath);
    }
}

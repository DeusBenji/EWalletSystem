using IdentityService.Domain.Model;

namespace IdentityService.Domain.Interfaces
{
    public interface IMitIdDbAccess
    {
       Task<MitID_Account?> GetMitIdAccountByAccId(Guid accId);
       Task<Guid> CreateMitIdAccount(MitID_Account mAcc);
       Task<List<MitID_Account>> GetAllMitIdAccounts();
       Task<bool> UpdateMitIdAccount(MitID_Account mAcc);
       Task<bool> DeleteMitIdAccount(Guid id);
       Task<MitID_Account?> GetMitIdAccountBySubId(string hashedSubId);
       Task<MitID_Account?> GetByNationalId(string nationalId);
    }
}

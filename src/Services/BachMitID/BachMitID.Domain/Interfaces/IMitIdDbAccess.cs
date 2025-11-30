using BachMitID.Domain.Model;

namespace BachMitID.Domain.Interfaces
{
    public interface IMitIdDbAccess
    {
       Task<MitID_Account?> GetMitIdAccountByAccId(Guid accId);
       Task<Guid> CreateMitIdAccount(MitID_Account mAcc);
       Task<List<MitID_Account>> GetAllMitIdAccounts();
       Task<bool> UpdateMitIdAccount(MitID_Account mAcc);
       Task<bool> DeleteMitIdAccount(Guid id);

    }
}

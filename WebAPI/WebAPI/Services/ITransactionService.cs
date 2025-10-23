using WebAPI.DTOs;

namespace WebAPI.Services
{
    public interface ITransactionService
    {
        PagedResult<TransactionDTO> GetPaged(TransactionDTO q, int currentUserId, bool isAdmin);
        TransactionDTO? GetById(int id, int currentUserId, bool isAdmin);
        TransactionDTO CreateOrGetByReference(TransactionDTO dto, int currentUserId);
        //TransactionDTO Cancel(int id, int currentUserId, bool isAdmin);
        TransactionDTO Refund(int id, int currentUserId, bool isAdmin);
        //TransactionDTO Approve(int id, int currentUserId, bool isAdmin);
        TransactionDTO CreateVipTransaction (TransactionDTO transactionDTO,int currentUserId);
        byte[] ExportCsv(TransactionDTO q, int currentUserId, bool isAdmin);
    }
}

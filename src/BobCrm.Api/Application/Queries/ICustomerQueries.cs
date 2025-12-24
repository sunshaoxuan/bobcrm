using BobCrm.Api.Contracts.Responses.Customer;

namespace BobCrm.Api.Application.Queries;

public interface ICustomerQueries
{
    List<CustomerListItemDto> GetList();
    CustomerDetailDto? GetDetail(int id);
}

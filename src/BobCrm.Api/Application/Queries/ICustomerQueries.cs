namespace BobCrm.Api.Application.Queries;

public interface ICustomerQueries
{
    List<object> GetList();
    object? GetDetail(int id);
}

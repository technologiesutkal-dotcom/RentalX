using RentalX.Models;

namespace RentalX.Services;

public interface ISearchService
{
    Task<List<SearchResultItem>> SearchAsync(string query);
}

using APIServiceManagement.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IMasterDataService
{
    Task<IReadOnlyList<State>> GetStatesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<City>> GetCitiesAsync(int? stateId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CityPincode>> GetCityPincodesAsync(int cityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetServicesAsync(int? categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetServicesByPincodeAsync(string pincode, CancellationToken cancellationToken = default);
}

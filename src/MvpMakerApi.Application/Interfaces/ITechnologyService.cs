using MvpMakerApi.Application.DTOs;

namespace MvpMakerApi.Application.Interfaces;

public interface ITechnologyService
{
    Task<IEnumerable<TechnologyDto>> GetAllTechnologiesAsync();
    Task<TechnologyDto> CreateTechnologyAsync(CreateTechnologyRequest request);
}

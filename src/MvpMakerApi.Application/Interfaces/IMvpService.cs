using MvpMakerApi.Application.DTOs;

namespace MvpMakerApi.Application.Interfaces;

public interface IMvpService
{
    Task<MvpDto> CreateMvpAsync(CreateMvpRequest request, Guid userId);
    Task<List<MvpDto>> GetMvpListAsync(MvpListQuery query);
    Task<MvpDetailsDto> GetMvpDetailsAsync(Guid id);
}

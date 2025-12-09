using MvpMakerApi.Application.DTOs;

namespace MvpMakerApi.Application.Interfaces;

public interface IMvpService
{
    Task<MvpDto> CreateMvpAsync(CreateMvpRequest request, Guid userId);
    Task<MvpDto> UpdateMvpAsync(UpdateMvpRequest request, Guid mvpId);
    Task<bool> DeleteMvpAsync(Guid mvpId);
    Task<List<MvpDto>> GetMvpListAsync(MvpListQuery query);
    Task<MvpDetailsDto> GetMvpDetailsAsync(Guid id);
    Task<PagedResult<MvpDto>> GetUserMvpsAsync(Guid userId, int pageNumber, int pageSize);
    Task<ValidateTokenResponse> ValidateMvpTokenAsync(Guid mvpId);
}

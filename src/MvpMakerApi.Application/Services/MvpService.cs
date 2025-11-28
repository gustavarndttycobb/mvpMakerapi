using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;
using MvpMakerApi.Domain.Entities;
using MvpMakerApi.Domain.Interfaces;

namespace MvpMakerApi.Application.Services;

public class MvpService : IMvpService
{
    private readonly IMvpRepository _mvpRepository;
    private readonly IUserRepository _userRepository;

    public MvpService(IMvpRepository mvpRepository, IUserRepository userRepository)
    {
        _mvpRepository = mvpRepository;
        _userRepository = userRepository;
    }

    public async Task<MvpDto> CreateMvpAsync(CreateMvpRequest request, Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        var mvp = new Mvp
        {
            Name = request.Name,
            Description = request.Description,
            Technologies = request.Technologies,
            Categories = request.Categories,
            ImageUrl = request.ImageUrl,
            Price = request.Price,
            OwnerId = userId,
            Highlights = request.Highlights,
            Objective = request.Objective,
            MainFeatures = request.MainFeatures,
            Status = request.Status,
            Screenshots = request.Screenshots,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _mvpRepository.AddAsync(mvp);

        return MapToDto(mvp, user);
    }

    public async Task<MvpDto> UpdateMvpAsync(UpdateMvpRequest request, Guid mvpId)
    {
        var mvp = await _mvpRepository.GetByIdAsync(mvpId);
        if (mvp == null)
        {
            throw new Exception("MVP not found");
        }

        mvp.Name = request.Name;
        mvp.Description = request.Description;
        mvp.Technologies = request.Technologies;
        mvp.Categories = request.Categories;
        mvp.ImageUrl = request.ImageUrl;
        mvp.Price = request.Price;
        mvp.Highlights = request.Highlights;
        mvp.Objective = request.Objective;
        mvp.MainFeatures = request.MainFeatures;
        mvp.Status = request.Status;
        mvp.Screenshots = request.Screenshots;
        mvp.UpdatedAt = DateTime.UtcNow;

        await _mvpRepository.UpdateAsync(mvp);

        return MapToDto(mvp, mvp.Owner!);
    }

    public async Task<bool> DeleteMvpAsync(Guid mvpId)
    {
        var mvp = await _mvpRepository.GetByIdAsync(mvpId);
        if (mvp == null)
        {
            throw new Exception("MVP not found");
        }

        await _mvpRepository.DeleteAsync(mvp);

        return true;
    }

    public async Task<List<MvpDto>> GetMvpListAsync(MvpListQuery query)
    {
        decimal? minPrice = null;
        decimal? maxPrice = null;
        if (query.PriceRange != null && query.PriceRange.Count == 2)
        {
            minPrice = query.PriceRange[0];
            maxPrice = query.PriceRange[1];
        }

        DateTime? startDate = null;
        DateTime? endDate = null;
        if (query.DateRange != null && query.DateRange.Count == 2)
        {
            if (DateTime.TryParse(query.DateRange[0], out var start)) startDate = start;
            if (DateTime.TryParse(query.DateRange[1], out var end)) endDate = end;
        }

        var mvps = await _mvpRepository.ListAsync(query.Category, query.Technology, minPrice, maxPrice, startDate, endDate);

        return mvps.Select(m => MapToDto(m, m.Owner!)).ToList();
    }

    public async Task<MvpDetailsDto> GetMvpDetailsAsync(Guid id)
    {
        var mvp = await _mvpRepository.GetByIdAsync(id);
        if (mvp == null)
        {
            throw new Exception("MVP not found");
        }

        return MapToDetailsDto(mvp, mvp.Owner!);
    }

    public async Task<PagedResult<MvpDto>> GetUserMvpsAsync(Guid userId, int pageNumber, int pageSize)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Max 100 items per page

        var (items, totalCount) = await _mvpRepository.GetByOwnerIdAsync(userId, pageNumber, pageSize);

        return new PagedResult<MvpDto>
        {
            Items = items.Select(m => MapToDto(m, m.Owner!)).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private MvpDto MapToDto(Mvp mvp, User owner)
    {
        return new MvpDto
        {
            Id = mvp.Id,
            Name = mvp.Name,
            Description = mvp.Description,
            Technologies = mvp.Technologies,
            Categories = mvp.Categories,
            ImageUrl = mvp.ImageUrl,
            Price = mvp.Price,
            CreatedAt = mvp.CreatedAt,
            UpdatedAt = mvp.UpdatedAt,
            Owner = new OwnerDto
            {
                Id = owner.Id,
                Name = owner.Name,
                Email = owner.Email
            }
        };
    }

    private MvpDetailsDto MapToDetailsDto(Mvp mvp, User owner)
    {
        return new MvpDetailsDto
        {
            Id = mvp.Id,
            Name = mvp.Name,
            Description = mvp.Description,
            Technologies = mvp.Technologies,
            Categories = mvp.Categories,
            ImageUrl = mvp.ImageUrl,
            Price = mvp.Price,
            CreatedAt = mvp.CreatedAt,
            UpdatedAt = mvp.UpdatedAt,
            Owner = new OwnerDto
            {
                Id = owner.Id,
                Name = owner.Name,
                Email = owner.Email
            },
            Highlights = mvp.Highlights,
            Objective = mvp.Objective,
            MainFeatures = mvp.MainFeatures,
            Status = mvp.Status,
            Screenshots = mvp.Screenshots
        };
    }
}

using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;
using MvpMakerApi.Domain.Entities;
using MvpMakerApi.Domain.Interfaces;

namespace MvpMakerApi.Application.Services;

public class TechnologyService : ITechnologyService
{
    private readonly ITechnologyRepository _technologyRepository;

    public TechnologyService(ITechnologyRepository technologyRepository)
    {
        _technologyRepository = technologyRepository;
    }

    public async Task<IEnumerable<TechnologyDto>> GetAllTechnologiesAsync()
    {
        var technologies = await _technologyRepository.GetAllAsync();
        return technologies.Select(t => new TechnologyDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            CreatedAt = t.CreatedAt
        });
    }

    public async Task<TechnologyDto> CreateTechnologyAsync(CreateTechnologyRequest request)
    {
        // Validate name is not empty
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Technology name is required");
        }

        // Check if technology already exists
        if (await _technologyRepository.ExistsByNameAsync(request.Name))
        {
            throw new InvalidOperationException($"Technology '{request.Name}' already exists");
        }

        var technology = new Technology
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty
        };

        var created = await _technologyRepository.CreateAsync(technology);

        return new TechnologyDto
        {
            Id = created.Id,
            Name = created.Name,
            Description = created.Description,
            CreatedAt = created.CreatedAt
        };
    }
}

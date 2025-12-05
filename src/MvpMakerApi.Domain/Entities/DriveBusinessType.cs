namespace MvpMakerApi.Domain.Entities;

public enum DriveBusinessType
{
    Transfer = 0,  // Single sale - transfer ownership (requires same Google Workspace domain)
    Share = 1      // Multiple sales - share file access (works with any Google account)
}

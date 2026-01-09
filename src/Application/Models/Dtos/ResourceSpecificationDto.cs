using Application.Models.Enums;

namespace Application.Models.Dtos;

/// <summary>
/// Represents a resource specification using ResourceCategory and specific type enums
/// </summary>
public class ResourceSpecificationDto
{
    public ResourceCategory Category { get; set; }
    public ComputeType? ComputeType { get; set; }
    public DatabaseType? DatabaseType { get; set; }
    public StorageType? StorageType { get; set; }
    public NetworkingType? NetworkingType { get; set; }
    public AnalyticsType? AnalyticsType { get; set; }
    public ManagementType? ManagementType { get; set; }
    public SecurityType? SecurityType { get; set; }
    public AIType? AIType { get; set; }

    /// <summary>
    /// Gets the specific type value as an integer for comparison
    /// </summary>
    public int GetTypeValue()
    {
        return Category switch
        {
            ResourceCategory.Compute => (int)(ComputeType ?? Enums.ComputeType.None),
            ResourceCategory.Database => (int)(DatabaseType ?? Enums.DatabaseType.None),
            ResourceCategory.Storage => (int)(StorageType ?? Enums.StorageType.None),
            ResourceCategory.Networking => (int)(NetworkingType ?? Enums.NetworkingType.None),
            ResourceCategory.Analytics => (int)(AnalyticsType ?? Enums.AnalyticsType.None),
            ResourceCategory.Management => (int)(ManagementType ?? Enums.ManagementType.None),
            ResourceCategory.Security => (int)(SecurityType ?? Enums.SecurityType.None),
            ResourceCategory.AI => (int)(AIType ?? Enums.AIType.None),
            _ => 0
        };
    }

    public override bool Equals(object? obj)
    {
        if (obj is ResourceSpecificationDto other)
        {
            return Category == other.Category && GetTypeValue() == other.GetTypeValue();
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Category, GetTypeValue());
    }
}

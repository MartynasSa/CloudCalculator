using Application.Models.Enums;

namespace Application.Models.Requests;

internal class SimpleVmRequest
{
    public CloudRegion Region { get; set; }
    public VmOperatingSystem OperatingSystem { get; set; }
    public int CpuCores { get; set; }
    public int RamGb { get; set; }
    public int StorageGb { get; set; }
}
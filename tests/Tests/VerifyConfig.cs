using System.Runtime.CompilerServices;
using Argon;

namespace Tests;

public static class VerifyConfig
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Configure Verify to serialize enums using EnumMember attribute values
        // This ensures CloudProvider.AWS serializes as "aws" instead of 0 or being omitted
        VerifierSettings.AddExtraSettings(settings =>
        {
            // Use custom ArgonEnumMemberConverter for EnumMember support
            settings.Converters.Add(new ArgonEnumMemberConverter());
            
            // Force serialization of default values (needed for AWS which is enum value 0)
            settings.DefaultValueHandling = DefaultValueHandling.Include;
        });
    }
}

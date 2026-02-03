using FredrikHr.PowerPlatformSdkExtensions.SampleDependencyLib;

namespace FredrikHr.PowerPlatformSdkExtensions.SamplePluginPackage;

public class SamplePlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext>();
        var outputs = context.OutputParameters;

        string sampleOutput = Convert.ToBase64String(
            SampleUtility.SampleXxHash32.ToArray()
            );

        outputs["SampleXxHash32Base64"] = sampleOutput;
    }
}
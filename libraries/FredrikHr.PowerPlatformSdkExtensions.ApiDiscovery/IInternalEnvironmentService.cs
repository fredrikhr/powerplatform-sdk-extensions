#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Xrm.Kernel.Contracts.Internal;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal readonly record struct IInternalEnvironmentService
    : IEnvironmentService
{
    private const string AssemblyQualifiedName =
        "Microsoft.Xrm.Sdk.IInternalEnvironmentService" + ", " +
        "Microsoft.Xrm.Kernel.Contracts.Internal, PublicKeyToken=31bf3856ad364e35";

    public static Type TypeReference { get; } =
        Type.GetType(AssemblyQualifiedName, throwOnError: true);

    public readonly IEnvironmentService Target { get; }

    private IInternalEnvironmentService(IEnvironmentService target)
    {
        _ = target ?? throw new ArgumentNullException(nameof(target));
        if (!TypeReference.IsAssignableFrom(target.GetType()))
            throw new InvalidCastException();
        Target = target;
    }

    public static IInternalEnvironmentService Wrap(IEnvironmentService target)
        => new(target);

    public readonly Uri AzureAuthorityHost => Target.AzureAuthorityHost;

    public readonly string Geo => Target.Geo;

    public readonly string AzureRegionName => Target.AzureRegionName;

    public readonly string ClusterCategory => (string)TypeReference.InvokeMember(
        nameof(ClusterCategory),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );
}
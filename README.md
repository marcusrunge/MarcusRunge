# MarcusRunge

A collection of reusable .NET libraries for data binding, controlled service initialization, Entity Framework Core, localization, networking, and development certificates.

The repository contains focused NuGet packages targeting .NET Standard 2.1, .NET 10, and .NET 10 for Windows.

## Packages

| Package | Target framework | Purpose |
|---|---|---|
| `MarcusRunge.Base` | `netstandard2.1` | Bindable base classes, entities, and controlled service initialization |
| `MarcusRunge.Base.EntityFramework` | `net10.0` | Entity Framework Core configuration for bindable entities |
| `MarcusRunge.Toolbox.Localization.Core` | `netstandard2.1` | Resource-based enum localization |
| `MarcusRunge.Toolbox.Localization.Wpf` | `net10.0-windows` | WPF enum binding and localization |
| `MarcusRunge.Toolbox.Network` | `net10.0` | IPv4 and IPv6 subnet, complement, and scanning utilities |
| `MarcusRunge.Toolbox.Security` | `net10.0` | Windows development-certificate generation |

## MarcusRunge.Base

`MarcusRunge.Base` provides the shared binding and initialization foundation. The package targets `netstandard2.1`.

### BindableBase

`BindableBase` implements `INotifyPropertyChanged` and provides:

- automatic capture of the current `SynchronizationContext`
- synchronous notifications when raised on the captured context
- asynchronous dispatch when raised from another context
- `SetProperty<T>` with optional callbacks and custom comparers
- notification of multiple properties
- support for logical and composite notification names
- isolation and diagnostic reporting of subscriber exceptions

A failing `PropertyChanged` subscriber does not prevent later subscribers from receiving the notification and does not propagate into the operation that caused the state change.

### BindableEntityBase

`BindableEntityBase` extends `BindableBase` with:

- an integer `Id` property for database identity
- a bindable `Id` implementation using `SetProperty`
- a `RowVersion` concurrency token maintained by the persistence layer

Changes to `RowVersion` do not raise `PropertyChanged`.

### CreateableBindableBase

`CreateableBindableBase<TInterface, TClass, TBase>` provides thread-safe synchronous creation followed by one-time asynchronous initialization.

Supported lifetimes:

- `Global`: one instance per closed generic creation type
- `Scoped`: one instance per initialization-context reference
- `Transient`: one independent instance per creation call

Additional behavior:

- synchronous initialization through `OnCreate`
- asynchronous initialization through `OnCreateAsync`
- bindable lifecycle properties
- caller-specific cancellation while waiting
- shared initialization that is not canceled by an individual waiter
- state-specific initialization tasks and exceptions
- isolated `OnCreated` subscriber failures
- immediate invocation of subscribers registered after successful initialization
- permanent faulting of an affected creation state after initialization failure

The first context used for a global creation state remains associated with that state. Scoped creation requires a stable, non-null reference-type context.

Reentrant creation of the same global or scoped creation state during synchronous initialization is not supported.

### ICreateableAware

`ICreateableAware` exposes:

- `Initialization`
- `InitializationException`
- `IsCreated`
- `IsInitializing`
- `OnCreated`

## MarcusRunge.Base.EntityFramework

`MarcusRunge.Base.EntityFramework` targets `net10.0` and depends on Entity Framework Core.

`EntityConfigurationBase<TEntity, TConfiguration>` configures:

- `Id` as the primary key
- `Id` as database-generated on insert
- `RowVersion` through `IsRowVersion()`
- optimistic concurrency through the database-generated row-version value

Concrete configurations may override `Configure`, call `base.Configure(builder)`, and then add entity-specific mappings.

## MarcusRunge.Toolbox.Localization.Core

`MarcusRunge.Toolbox.Localization.Core` targets `netstandard2.1` and provides resource-based enum localization.

### LocalizedDescriptionAttribute

`LocalizedDescriptionAttribute` resolves enum descriptions from strongly typed `.resx` resources.

Behavior includes:

- resolution for a requested `CultureInfo`
- fallback to `CurrentUICulture`
- reuse of `ResourceManager` instances
- visible `[[ResourceKey]]` markers for missing resources
- safe handling of missing resource manifests and satellite assemblies

### EnumDescriptionProvider

`EnumDescriptionProvider` resolves:

- localized descriptions
- ordinary `DescriptionAttribute` values
- declared enum names when no description exists
- combined localized `[Flags]` values
- textual representations of undefined numeric values

### EnumDescriptionTypeConverter

`EnumDescriptionTypeConverter` integrates localized enum descriptions with the `System.ComponentModel` type-conversion infrastructure.

## MarcusRunge.Toolbox.Localization.Wpf

`MarcusRunge.Toolbox.Localization.Wpf` targets `net10.0-windows` and enables WPF support.

### EnumBindingSourceExtension

`EnumBindingSourceExtension` exposes declared enum values as an `ItemsSource`.

Nullable enum types are accepted and resolved to their underlying enum type. The resulting items source contains the declared enum values and does not add a separate `null` item.

### EnumDescriptionValueConverter

`EnumDescriptionValueConverter` converts enum values into localized descriptions.

It:

- forwards the WPF binding culture to the localization core
- preserves `DependencyProperty.UnsetValue`
- returns non-enum values unchanged
- intentionally does not support reverse conversion

## MarcusRunge.Toolbox.Network

`MarcusRunge.Toolbox.Network` targets `net10.0`.

### IPv4

The IPv4 namespace provides:

- discovery of local IPv4 host addresses
- address-range calculation
- subnet-mask lookup for local addresses
- asynchronous range scanning
- bounded parallel ping operations
- progress reporting
- cancellation support
- calculation of allowed CIDR networks from excluded ranges

### IPv6

The IPv6 namespace provides:

- discovery of local IPv6 host addresses
- address-range calculation from prefix lengths
- local prefix-length lookup
- asynchronous range scanning
- configurable scan safety limits
- bounded parallel ping operations
- progress reporting
- cancellation support
- calculation of allowed CIDR networks from excluded ranges

IPv6 scanning defaults to a safety limit of 1,000,000 addresses. Callers may provide a different positive limit when required.

## MarcusRunge.Toolbox.Security

`MarcusRunge.Toolbox.Security` targets `net10.0`.

`CertificateProvider` creates self-signed certificates for development and testing with:

- configurable RSA key size
- SHA-256 signatures
- PKCS#1 signature padding
- basic constraints
- digital-signature and key-encipherment usage
- server-authentication enhanced key usage
- DNS subject alternative names
- subject-key identifiers
- password-protected PKCS#12 export

The API is marked with `[SupportedOSPlatform("windows")]` and loads generated certificates using Windows machine-key storage.

Generated certificates are intended for development and testing. They are not a replacement for certificates issued and managed through an appropriate production certificate infrastructure.

## Installation

Install only the packages required by the consuming application.

```bash
dotnet add package MarcusRunge.Base
dotnet add package MarcusRunge.Base.EntityFramework
dotnet add package MarcusRunge.Toolbox.Localization.Core
dotnet add package MarcusRunge.Toolbox.Localization.Wpf
dotnet add package MarcusRunge.Toolbox.Network
dotnet add package MarcusRunge.Toolbox.Security
```

## Usage

### Bindable ViewModel

```csharp
using MarcusRunge.Base;

public sealed class UserViewModel : BindableBase
{
    private string? _email;
    private bool _isActive;
    private string? _name;

    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string? Name
    {
        get => _name;
        set => SetProperty(ref _name, value, onChanged: OnNameChanged);
    }

    private void OnNameChanged()
    {
        // Perform dependent work after the stored value changed.
    }
}
```

### Controlled service initialization

```csharp
using MarcusRunge.Base;

public interface IApplicationService : ICreateableAware
{
}

public sealed class ApplicationService
    : CreateableBindableBase<IApplicationService, ApplicationService, ApplicationContext>,
      IApplicationService
{
    protected override void OnCreate(ApplicationContext context)
    {
        // Establish every invariant required before the instance is published.
    }

    protected override Task OnCreateAsync(ApplicationContext context, CancellationToken cancellationToken)
    {
        // Perform one-time asynchronous initialization.
        return Task.CompletedTask;
    }
}
```

Create and await a global instance:

```csharp
var service = await ApplicationService.CreateAsync(
    context,
    cancellationToken);
```

Create a context-scoped instance:

```csharp
var service = await ApplicationService.CreateAsync(
    context,
    CreationLifetime.Scoped,
    cancellationToken);
```

Create an independent instance:

```csharp
var service = await ApplicationService.CreateAsync(
    context,
    CreationLifetime.Transient,
    cancellationToken);
```

The cancellation token passed to `CreateAsync` cancels only that caller's wait. It does not cancel shared initialization.

### Entity Framework Core configuration

```csharp
using MarcusRunge.Base;
using MarcusRunge.Base.EntityFramework;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class User : BindableEntityBase
{
    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

public sealed class UserConfiguration
    : EntityConfigurationBase<User, UserConfiguration>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.Property(user => user.Name).IsRequired().HasMaxLength(100);
        builder.Property(user => user.Email).IsRequired().HasMaxLength(320);
    }
}
```

### Localized enum

```csharp
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum OrderStatus
{
    [LocalizedDescription("OrderStatusPending", typeof(Resources))]
    Pending = 0,

    [LocalizedDescription("OrderStatusShipped", typeof(Resources))]
    Shipped = 1,

    [LocalizedDescription("OrderStatusDelivered", typeof(Resources))]
    Delivered = 2
}
```

Add the matching entries to the neutral and localized resource files:

```text
Properties/Resources.resx
Properties/Resources.de.resx
```

### WPF enum binding

Register the namespaces and converter:

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="clr-namespace:Example"
    xmlns:localization="clr-namespace:MarcusRunge.Toolbox.Localization.Wpf;assembly=MarcusRunge.Toolbox.Localization.Wpf">

    <Window.Resources>
        <localization:EnumDescriptionValueConverter x:Key="EnumDescriptionValueConverter" />
    </Window.Resources>

    <Grid>
        <ComboBox
            ItemsSource="{localization:EnumBindingSource {x:Type local:OrderStatus}}"
            SelectedItem="{Binding Status}">
            <ComboBox.ItemTemplate>
                <DataTemplate>
                    <TextBlock
                        Text="{Binding Converter={StaticResource EnumDescriptionValueConverter}}" />
                </DataTemplate>
            </ComboBox.ItemTemplate>
        </ComboBox>
    </Grid>
</Window>
```

### Development certificate

```csharp
using MarcusRunge.Toolbox.Security;

using var certificate = CertificateProvider.CreateCertificate(
    password: "development-password",
    commonName: "localhost",
    rsaKeySize: 2048,
    years: 5);
```

## Building

Restore and build the solution:

```bash
dotnet restore MarcusRunge.slnx
dotnet build MarcusRunge.slnx
```

Build configurations are centralized in `Directory.Build.props`.

The repository uses MinVer for tag-based package versioning. Version tags use the `v` prefix.

## Testing

Run all tests:

```bash
dotnet test MarcusRunge.slnx
```

The repository contains test projects for:

- `MarcusRunge.Base`
- `MarcusRunge.Base.EntityFramework`
- `MarcusRunge.Toolbox.Network`
- `MarcusRunge.Toolbox.Security`

Tests use xUnit v3.

## Continuous integration

The repository contains two GitHub Actions workflows:

- `main.yml` builds and tests pushes and pull requests targeting `main`
- `release.yml` restores, builds, packs, and publishes NuGet packages for `v*` tags based on the `release` branch

NuGet publication uses the `nuget` GitHub environment and a configured `NUGET_API_KEY` secret.

## Repository utilities

### Export-SourceCode.ps1

Exports supported source and configuration files into `Source.txt`.

The script excludes generated and transient directories including:

- `.git`
- `.vs`
- `artifacts`
- `bin`
- `obj`
- `packages`
- `TestResults`

Because local PowerShell execution policies may block unsigned scripts, run it explicitly when required:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Export-SourceCode.ps1
```

### Clean-BuildArtifacts.ps1

Removes generated build and test directories such as:

- `bin`
- `obj`
- `.vs`
- `TestResults`
- `artifacts`

Run it explicitly when required:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Clean-BuildArtifacts.ps1
```

## Repository structure

```text
MarcusRunge/
├── .github/
│   └── workflows/
│       ├── main.yml
│       └── release.yml
├── MarcusRunge.Base/
│   ├── Properties/
│   │   ├── AssemblyInfo.cs
│   │   ├── Resources.de.resx
│   │   ├── Resources.Designer.cs
│   │   └── Resources.resx
│   ├── BindableBase.cs
│   ├── BindableEntityBase.cs
│   ├── CreateableBindableBase.cs
│   ├── CreationLifetime.cs
│   ├── ICreateableAware.cs
│   └── MarcusRunge.Base.csproj
├── MarcusRunge.Base.EntityFramework/
│   ├── EntityConfigurationBase.cs
│   └── MarcusRunge.Base.EntityFramework.csproj
├── MarcusRunge.Base.EntityFramework.Test/
│   ├── EntityConfigurationBaseTest.cs
│   └── MarcusRunge.Base.EntityFramework.Test.csproj
├── MarcusRunge.Base.Tests/
│   ├── BindableBaseTest.cs
│   ├── BindableEntityBaseTest.cs
│   ├── CreateableBindableBaseTest.cs
│   └── MarcusRunge.Base.Test.csproj
├── MarcusRunge.Toolbox.Localization.Core/
│   ├── EnumDescriptionProvider.cs
│   ├── EnumDescriptionTypeConverter.cs
│   ├── LocalizedDescriptionAttribute.cs
│   └── MarcusRunge.Toolbox.Localization.Core.csproj
├── MarcusRunge.Toolbox.Localization.Wpf/
│   ├── EnumBindingSourceExtension.cs
│   ├── EnumDescriptionValueConverter.cs
│   └── MarcusRunge.Toolbox.Localization.Wpf.csproj
├── MarcusRunge.Toolbox.Network/
│   ├── Helper/
│   ├── IPv4/
│   ├── IPv6/
│   └── MarcusRunge.Toolbox.Network.csproj
├── MarcusRunge.Toolbox.Network.Test/
│   └── MarcusRunge.Toolbox.Network.Test.csproj
├── MarcusRunge.Toolbox.Security/
│   ├── CertificateProvider.cs
│   └── MarcusRunge.Toolbox.Security.csproj
├── MarcusRunge.Toolbox.Security.Test/
│   ├── CertificateProviderTest.cs
│   └── MarcusRunge.Toolbox.Security.Test.csproj
├── .gitattributes
├── .gitignore
├── Clean-BuildArtifacts.ps1
├── Directory.Build.props
├── Export-SourceCode.ps1
├── LICENSE.txt
├── MarcusRunge.slnx
└── README.md
```

Generated `bin`, `obj`, `.vs`, `TestResults`, and `artifacts` directories are intentionally omitted from this structure.

## License

This project is distributed under the MIT License. See [LICENSE.txt](LICENSE.txt).

## Author

**Marcus Runge**

## Repository

- **GitHub**: [marcusrunge/MarcusRunge](https://github.com/marcusrunge/MarcusRunge)
- **NuGet**: [MarcusRunge packages](https://www.nuget.org/packages?q=MarcusRunge)

## Contributing

Bug reports, feature requests, and pull requests are welcome through the GitHub repository.
# MarcusRunge

A comprehensive .NET library providing base classes, utilities, and integrations for building robust, maintainable applications. This repository contains multiple specialized packages targeting .NET Standard 2.1 and .NET 10, organized into cohesive modules for MVVM, data access, security, networking, and localization.

## Overview

The MarcusRunge project is a collection of production-ready, reusable components organized into focused packages. Whether you're building desktop applications with WPF, web services with ASP.NET Core, or any .NET application, MarcusRunge provides battle-tested base classes and utilities to accelerate development.

### Core Areas

- **MVVM & Data Binding**: Base classes for ViewModels and bindable entities with `INotifyPropertyChanged` support
- **Entity Framework Integration**: Enhanced data access patterns and entity configuration
- **Security**: Certificate generation, cryptographic utilities for development and testing
- **Networking**: IPv4/IPv6 utilities, address scanning, subnet operations
- **Localization**: Multi-language support for WinForms and WPF applications

## Packages

### Core Foundation

#### **MarcusRunge.Base** ⭐ `netstandard2.1`
Foundation library providing base classes for MVVM and data binding scenarios.

**Key Components:**
- **`BindableBase`**: Abstract base implementing `INotifyPropertyChanged` with:
  - Property change notifications with optional callbacks
  - Custom equality comparison for properties
  - Batch property notifications
  - Automatic `SynchronizationContext` capture for UI thread safety
  - Debug-time verification of property names

- **`BindableEntityBase`**: Domain entity base with:
  - Built-in `Id` property for entity identification
  - Timestamp support for optimistic concurrency
  - Full MVVM binding support

- **`CreateableBindableBase<TInterface, TClass, TBase>`**: Thread-safe singleton-like creation with:
  - One-time async initialization
  - Creation state tracking
  - Safe event notification after creation
  - Exception handling and reporting

- **`ICreateableAware`**: Interface for creation-aware components:
  - `IsCreated` property
  - `Initialization` task tracking
  - `InitializationException` for error capture
  - `OnCreated` event

**Usage**: Inherit from `BindableBase` for ViewModels, `BindableEntityBase` for domain models

---

### Data Access

#### **MarcusRunge.Base.EntityFramework** `netstandard2.1`
Entity Framework Core integration and configuration patterns.

**Key Components:**
- **`EntityConfigurationBase<TEntity, TConfiguration>`**: Base class for Entity Framework entity configurations
  - Automatic setup of `Id` as primary key
  - Built-in support for concurrency tokens (RowVersion)
  - Simplified entity mapping configuration

**Dependencies**: Entity Framework Core

**Usage**: Inherit when configuring entities for EF Core

#### **MarcusRunge.Base.EntityFramework.Test** `netstandard2.1`
Testing utilities and helpers for Entity Framework scenarios.

---

### Security

#### **MarcusRunge.Toolbox.Security** `net10`
Cryptographic utilities and certificate generation for development and testing.

**Key Components:**
- **`CertificateProvider`**: Self-signed certificate generation
  - RSA key support (configurable size, default 2048)
  - SHA-256 signing
  - Comprehensive X.509 extensions (BasicConstraints, KeyUsage, EnhancedKeyUsage, SubjectAlternativeName)
  - PKCS#12 (PFX) export format
  - Ideal for development, testing, and SSL/TLS scenarios

**Usage**: 
```csharp
var cert = CertificateProvider.CreateCertificate("password", "localhost");
```

**Platform**: Windows-only (`[SupportedOSPlatform("windows")]`)

#### **MarcusRunge.Toolbox.Security.Test** `net10`
Test suites for security utilities.

---

### Networking

#### **MarcusRunge.Toolbox.Network** `net10`
IPv4 and IPv6 networking utilities and address manipulation.

**Key Components:**
- **IPv4 Support**:
  - `Scanner`: Scan and retrieve IPv4 addresses
  - `Complement`: IPv4 address complementing
  - Subnet operations

- **IPv6 Support**:
  - `Scanner`: Scan and retrieve IPv6 addresses
  - `Complement`: IPv6 address complementing
  - Comprehensive IPv6 utilities

**Usage**: Network scanning, subnet analysis, address manipulation

#### **MarcusRunge.Toolbox.Network.Test** `net10`
Comprehensive test coverage for networking utilities.

---

### Localization & Internationalization

#### **MarcusRunge.Toolbox.Localization.Core** `netstandard2.1`
Core localization framework for multi-language support.

**Key Components:**
- **`LocalizedDescriptionAttribute`**: Attribute for localizable enum descriptions
  - Resource-based localization using `.resx` files
  - Works with `ComponentModel` infrastructure

- **`EnumDescriptionTypeConverter`**: Type converter for localized enums

- **`EnumDescriptionProvider`**: Provider for enum descriptions with localization support

**Usage**: 
```csharp
public enum Status
{
    [LocalizedDescription("status_active", typeof(Resources))]
    Active,
    [LocalizedDescription("status_inactive", typeof(Resources))]
    Inactive
}
```

#### **MarcusRunge.Toolbox.Localization.Wpf** `net10`
WPF-specific localization extensions and value converters.

**Key Components:**
- **`EnumBindingSourceExtension`**: XAML markup extension for enum binding with localization
- **`EnumDescriptionValueConverter`**: WPF value converter for localized enum descriptions

**Usage**: Bind localized enum values in XAML

---

### Testing Utilities

#### **MarcusRunge.Toolbox.Test** `net10`
General-purpose testing utilities and helpers.

---

## Getting Started

### Installation

Install packages via NuGet:

```bash
# Foundation package
dotnet add package MarcusRunge.Base

# With Entity Framework support
dotnet add package MarcusRunge.Base.EntityFramework

# For security utilities
dotnet add package MarcusRunge.Toolbox.Security

# For networking utilities
dotnet add package MarcusRunge.Toolbox.Network

# For localization
dotnet add package MarcusRunge.Toolbox.Localization.Core
dotnet add package MarcusRunge.Toolbox.Localization.Wpf
```

### Example: MVVM ViewModel

```csharp
using MarcusRunge.Base;

public class UserViewModel : BindableBase
{
    private string _name;
    private string _email;
    private bool _isActive;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, onChanged: () => 
        {
            // React to name changes
        });
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}
```

### Example: Entity Framework Configuration

```csharp
using MarcusRunge.Base;
using MarcusRunge.Base.EntityFramework;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class User : BindableEntityBase
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class UserConfiguration : EntityConfigurationBase<User, UserConfiguration>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder); // Handles Id and RowVersion

        builder.Property(u => u.Name).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Email).IsRequired();
    }
}
```

### Example: Certificate Generation

```csharp
using MarcusRunge.Toolbox.Security;

// Create a self-signed certificate for development
var certificate = CertificateProvider.CreateCertificate(
    password: "dev-password",
    commonName: "localhost",
    rsaKeySize: 2048,
    years: 5
);

// Use in your application
services.AddHttpsRedirection(options => 
{
    options.HttpsPort = 5001;
});
```

### Example: Localization

```csharp
// Define localized enum
public enum OrderStatus
{
    [LocalizedDescription("order_pending", typeof(Resources))]
    Pending,
    [LocalizedDescription("order_shipped", typeof(Resources))]
    Shipped,
    [LocalizedDescription("order_delivered", typeof(Resources))]
    Delivered
}

// In XAML (WPF)
<ComboBox ItemsSource="{markup:EnumBindingSource markup:local, OrderStatus}"/>

// Or use the converter
<TextBlock Text="{Binding Status, Converter={local:EnumDescriptionValueConverter}}"/>
```

## Features

✅ **MVVM Foundation**: Production-ready `INotifyPropertyChanged` implementation  
✅ **Entity Framework Integration**: Simplified entity configuration  
✅ **Security Utilities**: Certificate generation for SSL/TLS development  
✅ **Networking Tools**: IPv4/IPv6 address and subnet utilities  
✅ **Localization Support**: Resource-based multi-language support  
✅ **.NET Standard 2.1**: Broad framework compatibility  
✅ **Thread-Safe**: Proper synchronization for concurrent scenarios  
✅ **Performance**: Optimized with minimal allocations  
✅ **Comprehensive Documentation**: Full XML docs and IntelliSense support  
✅ **Well-Tested**: Extensive unit test coverage  

## Requirements

- **.NET Standard 2.1** or higher (MarcusRunge.Base)
- **.NET 10** or higher (Toolbox packages)
- No external dependencies (except Entity Framework for EF integration)

## Building

```bash
dotnet build
```

## Testing

```bash
dotnet test
```

## Project Structure

```
MarcusRunge/
├── MarcusRunge.Base/                          # Core MVVM base classes
├── MarcusRunge.Base.EntityFramework/          # EF Core integration
├── MarcusRunge.Base.EntityFramework.Test/     # EF testing utilities
├── MarcusRunge.Base.Tests/                    # Core library tests
├── MarcusRunge.Toolbox.Security/              # Security utilities
├── MarcusRunge.Toolbox.Security.Test/         # Security tests
├── MarcusRunge.Toolbox.Network/               # IPv4/IPv6 utilities
├── MarcusRunge.Toolbox.Network.Test/          # Network tests
├── MarcusRunge.Toolbox.Localization.Core/     # Core localization
├── MarcusRunge.Toolbox.Localization.Wpf/      # WPF localization
└── MarcusRunge.Toolbox.Test/                  # General testing utilities
```

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## Author

**Marcus Runge**

## Repository

- **GitHub**: [marcusrunge/MarcusRunge](https://github.com/marcusrunge/MarcusRunge)
- **NuGet**: [MarcusRunge packages](https://www.nuget.org/packages?q=MarcusRunge)

## Contributing

Contributions are welcome! Please feel free to:
- Submit a pull request with improvements
- Open an issue for bugs or feature requests
- Share feedback and suggestions

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/marcusrunge/MarcusRunge).

---

**Made with ❤️ by Marcus Runge**
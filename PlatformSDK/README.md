# Creates a new Service Account for use with CI

## Prerequisites

- [.NET Core 2.0](https://www.microsoft.com/net/core) or any .NET toolset supporting .NET Core 2.0 projects

## Building and running the C# solution

You can build and run the C# solution in an IDE, or using the .NET Core CLI.

### Using an IDE
1. Open [PlatformSDK.sln](PlatformSDK.sln) in your preferred IDE, configured to use the .NET Core Runtime.
2. Run any of the projects in the solution.

### Using the .NET Core CLI
   
```bash
dotnet restore
dotnet run -p ServiceAccountMaintenance/ServiceAccountMaintenance.csproj
```

## Installing the C# Platform SDK

* Please see the [SpatialOS documentation](https://docs.improbable.io/reference/latest/platform-sdk/introduction)
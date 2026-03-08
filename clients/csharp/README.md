Auto-generated C# client for the [Universalis](https://universalis.app) FFXIV
market board API.

## Installation

```shell
dotnet add package Universalis.Client
```

## Usage

The package provides clients for all three API versions under separate
namespaces. Use the latest version (V3) unless you have a specific reason to
target an older one.

```csharp
using Universalis.Client.V3;

var httpClient = new HttpClient
    { BaseAddress = new Uri("https://universalis.app") };
var client = new Client(httpClient);

// Get current market board listings + recent sales for item 5 on Gilgamesh
var overview = await client.OverviewAsync("Gilgamesh", 5);
```

### V1 / V2

```csharp
using Universalis.Client.V1;
// or
using Universalis.Client.V2;

var client = new Client(httpClient);
```

## Versioning

This package version tracks the Universalis server releases. The generated code
is produced from the live OpenAPI spec — no manual maintenance required.

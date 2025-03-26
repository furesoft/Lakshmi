# Lakshmi
Lakshmi is a source generator for c# to make working with extism pdk more the idiomatic c# way

[![CodeFactor](https://www.codefactor.io/repository/github/furesoft/lakshmi/badge)](https://www.codefactor.io/repository/github/furesoft/lakshmi)
![NuGet Version](https://img.shields.io/nuget/v/Lakshmi)
![NuGet Downloads](https://img.shields.io/nuget/dt/Lakshmi)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
![Discord](https://img.shields.io/discord/455738571186241536)
![Libraries.io SourceRank](https://img.shields.io/librariesio/sourcerank/nuget/Lakshmi)

# Installation

1. Install the nuget package with `dotnet add package Lakshmi`
2. Use it

# Usage

To work with Lakshmi you have to reference the dotnet extism pdk. Using extism forces you to write a more native like c#. Lashmi provides a source generator to use higher level types.

## Importing functions

```csharp
record MyPoint(int x, int y);

class Test {
  [Import("host", Entry="add")]
  public partial int Add(int a, int b);

  [Import("host", Entry="getPoint")]
  public partial MyPoint GetPoint();
}
```

## Exporting functions

```csharp
record ExtensionInfo(string author, string[] allowed_hosts);

class Test {
  [Export("register")]
  public static ExtensionInfo Register() {
    return new ExtensionInfo("furesoft", []);
  }
}
```

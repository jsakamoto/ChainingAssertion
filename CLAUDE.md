# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

ChainingAssertion ("bin-edition" branch): fluent assertion extension methods (`Is`, `IsNot`, `IsNull`, `IsStructuralEqual`, `AsDynamic()` private accessor, ...) shipped as NuGet binary packages for three test frameworks: NUnit, xUnit.net, MSTest. The `bin-edition` branch is the main branch.

## Layout

There are three independent, parallel "editions", each with its own solution, library project, test project, version and `RELEASE-NOTES.txt`:

| Edition | Solution | Library (package id) | Tests |
|---|---|---|---|
| NUnit | `ChainingAssertion.NUnit-Bin.slnx` | `ChainingAssertion.NUnit` (`ChainingAssertion-NUnit.Bin`) | `ChainingAssertion.NUnit.UnitTest` |
| xUnit | `ChainingAssertion.xUnit-Bin.slnx` | `ChainingAssertion.xUnit` (`ChainingAssertion-xUnit.Bin`) | `ChainingAssertion.xUnit.UnitTest` |
| MSTest | `ChainingAssertion.MSTest-Bin.slnx` | `ChainingAssertion.MSTest` (`ChainingAssertion-MSTest.Bin`) | `ChainingAssertion.MSTest.UnitTest` |

- Each edition's main code is a single large file (`ChainingAssertion.<Fx>/ChainingAssertion.<Fx>.cs`) defining `public static partial class AssertEx`. The three files are near-copies that differ in the underlying framework calls, so a behavioral change to the shared API usually has to be applied to all three (and to the three `AssertExTest.cs` files). The MSTest file is larger because it also has the parameterized-test / exception-test helpers.
- `ChainingAssertion.Shared` is a shared project (`.shproj`/`.projitems`) imported by every library csproj. It holds framework-independent internals: `DynamicAccessor`, `ReflectAccessor` (backing `AsDynamic()`), and the `MaybeNull`/`NotNull` attribute polyfills. New shared files must be added to `ChainingAssertion.Shared.projitems`.
- `ChainingAssertion/` (package `ChainingAssertion.Bin`) is a deprecated metadata-only package that just depends on `ChainingAssertion-MSTest.Bin`; it contains no code.
- Target frameworks: NUnit lib `net462;net8.0;net10.0` (NUnit 5); xUnit/MSTest libs `netstandard2.0;netstandard2.1`; all test projects `net8.0;net10.0` with nullable reference types on and `nullable` warnings as errors.
- Tests run on Microsoft.Testing.Platform (MTP), selected by `global.json`. The xUnit test project stays on xUnit v2 (the library depends on it) and gets MTP via `YTest.MTP.XUnit2`.

## Commands

One CI workflow per edition (`.github/workflows/unit-tests-for-*.yml`), running both TFMs. Run from the repo root with `--solution`; `--project` with a relative path fails from the root.

```
dotnet test --solution ChainingAssertion.NUnit-Bin.slnx
dotnet test --solution ChainingAssertion.xUnit-Bin.slnx
dotnet test --solution ChainingAssertion.MSTest-Bin.slnx
```

Run a single test: `dotnet test --solution ChainingAssertion.NUnit-Bin.slnx -f net8.0 --filter "FullyQualifiedName~<TestName>"`.

Release builds (`-c Release`) generate the `.nupkg` into `dist/` (`GeneratePackageOnBuild`); `nuget.config` registers `.\dist` as a local package source.

## Releasing

Version lives in each library csproj (`<Version>`). Package release notes are extracted from the topmost `v.x.y.z` block of that edition's `RELEASE-NOTES.txt` at pack time, so keep newest entries on top in the `v.x.y.z` format. The README is packed into every package.

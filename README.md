# ediFabric Native API for AWS Lambda

## 1. Overview
This example hosts [ediFabric Native](https://www.edifabric.com/edifabric-native.html) behind an AWS Lambda and API Gateway HTTP API. It translates X12 EDI to JSON and back, validates transaction sets, and generates acknowledgments using the C# bindings from [edifabric-csharp-bindings](https://github.com/EdiFabric/edifabric-csharp-bindings).

The native library is a self-contained shared library. No `EdiFabric.Api` NuGet package (aka EdiNation InHouse) is required on the target machine beyond this Lambda host.

## 2. Requirements
- [Visual Studio](https://visualstudio.microsoft.com/vs/) or the .NET 10 SDK.
- An [AWS account](https://aws.amazon.com/) and the [AWS Toolkit for Visual Studio](https://aws.amazon.com/visualstudio/) or the [Amazon.Lambda.Tools](https://github.com/aws/aws-extensions-for-dotnet-cli) CLI.
- [Download Postman](https://www.postman.com/downloads/) - it's an application to consume/test your API.
- The native library for your platform:

| Platform | File |
| --- | --- |
| Windows (local) | `edifabric-x12-tools.dll` |
| Linux (AWS Lambda) | `edifabric-x12-tools.so` |
| macOS | `edifabric-x12-tools.dylib` |

[Download **ediFabric Native** Library](https://support.edifabric.com/hc/en-us/articles/37289848931869-Download)

Put the library in the repository root or in `EdiFabric.Api.AWS`, or set `Configuration.LibraryPath` / `EDIFABRIC_X12_LIB`. The project copies it next to the Lambda package on build when it is found in those folders. Deploy the Linux `.so` with the function.

- X12 test file(s). If you don't have a test file, use one of ours - [X12 HIPAA](https://support.edifabric.com/hc/en-us/sections/360001487352-X12-HIPAA-Files-Templates), [X12](https://support.edifabric.com/hc/en-us/sections/360005274077-X12-Files-Templates).

## 3. License
Set `Configuration.ApiKey` to your serial. The free-plan serial is:

```
bd96a836feca45cb91c86ee65d281f52
```

The free plan authorizes with `set_serial` only. Tokens (`S3Cache`) are available for the Enterprise license.

## 4. Setup
Rebuild the solution. If there are any build errors, contact us at https://support.edifabric.com/hc/en-us/requests/new for assistance.

The C# bindings live in `EdiFabric.Api.AWS/Native` (`NativeMethods.cs` and `EdiFabricX12.cs`), copied from the [edifabric-csharp-bindings](https://github.com/EdiFabric/edifabric-csharp-bindings) repository.

By default the API uses the online spec service (`SetMap` with `"default": "<serial>"`). To use local JSON models instead, place a `map/map.json` next to the Lambda project (see the bindings README for the map format).

## 5. Getting started
To deploy, follow the steps in the [How to use EDI API as AWS Lambda](https://support.edifabric.com/hc/en-us/articles/7726206553361-How-to-use-EDI-API-as-AWS-Lambda) article, then POST X12 EDI to:

| Endpoint | Native call | Input | Output
| --- | --- | --- | --- |
| `POST /x12/read` | `EdiFabricX12.Parse` (JSON only) | X12 | JSON |
| `POST /x12/write` | `EdiFabricX12.Build` | JSON (the output from /read) | X12 |
| `POST /x12/validate` | `EdiFabricX12.Parse` (JSON + validation report) | JSON (the output from /read) or X12 | JSON |
| `POST /x12/ack` | `EdiFabricX12.Parse` (JSON + validation + 999/997/TA1) | JSON (the output from /read) or X12 | JSON |

## 6. Warranty
*The source code in these example projects is strictly for demonstrational purposes and is provided "AS IS" without warranty of any kind, whether expressed or implied, including but not limited to the implied warranties of merchantability and/or fitness for a particular purpose.*

## 7. Additional information

[ediFabric Native documentation](https://support.edifabric.com/hc/en-us/articles/37276016388125-Introduction)

[C# bindings](https://github.com/EdiFabric/edifabric-csharp-bindings)

[Support](https://support.edifabric.com/hc/en-us/requests/new)
### 2026 © EdiFabric

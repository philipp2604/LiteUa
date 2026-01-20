# LiteUa 🔗

[![Language](https://img.shields.io/badge/language-C%23-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![GitHub issues](https://img.shields.io/github/issues/philipp2604/LiteUa)](https://github.com/philipp2604/LiteUa/issues)  

**LiteUa** is a native, lightweight, and dependency-free OPC UA Client Library written entirely in C# from scratch.

Unlike other libraries that wrap the official OPC Foundation .NET Stack (which imposes GPL licensing constraints) or commercial SDKs, LiteUa implements the OPC UA binary protocol directly on top of TCP. This makes it extremely fast, lean, and permissive-license friendly.

> **⚠️ Work In Progress (WIP)**
>
> This library is currently in an early development stage. While the core functionality (Secure Connection, Read/Write, Subscriptions) is implemented and tested, it does not yet cover the entire 1000+ page OPC UA specification. Use with caution in production environments.

## 🚀 Why LiteUa?

- **Zero 3rd Party Dependencies**: The core library is built directly on `System.Net.Sockets` and `System.Buffers`. No dependency on the official OPC Foundation Stack. No GPL viral licensing issues.
- **Pure C#**: Fully managed code without native wrappers.
- **High Performance**: Features a built-in `ConnectionPool` for request-heavy scenarios.
- **Resilient**: Includes a `SubscriptionClient` with automatic reconnection and self-healing capabilities.
- **Tested**: Everything is tested using integration tests against Siemens S7-1200 (modeled OPC Server interface) and S7-1500 controllers (standard Simatic OPC Server interface).

### 📦 Dependencies & Licenses

While the **LiteUa** core library is strictly dependency-free to ensure ease of licensing, the **Test Projects** utilize external packages (e.g., xUnit, Microsoft.NET.Test.Sdk) to ensure code quality and protocol correctness.

For a full list of dependencies used in the test environment, please refer to:
📄 **[THIRD-PARTY-PACKAGES.md](https://github.com/philipp2604/LiteUa/blob/master/README.md)**

## ✨ Implemented Features

LiteUa covers the most important features required for HMI/SCADA and IoT connectivity:

### 🔐 Security & Connectivity
- [x] **OPC UA TCP Transport**: Efficient Chunking and Message handling.
- [x] **Secure Channel**: Support for `Sign` and `SignAndEncrypt`.
- [x] **Policies**: Full implementation of `Basic256Sha256` (RSA-OAEP, AES-CBC, HMAC-SHA256).
- [x] **Authentication**: `Anonymous` and `UserName` (including encrypted password transmission).
- [x] **Certificate Management**: Handling of X.509 Certificates and Server Signature validation.

### 📡 Core Services
- [x] **Read / Write**: Support for Scalars, **Arrays**, and complex **Structures** (UDTs).
- [x] **Browse**: Recursive browsing with Bulk support and automatic Paging (ContinuationPoints).
- [x] **Method Calls**: Call methods directly on the server with typed argument mapping.
- [x] **Subscriptions**: Monitoring of data changes (DataChangeNotification) with KeepAlive logic.

### 🛠 Architecture
- [x] **Type Registry**: Dynamic decoding of custom structures (ExtensionObjects) without code generation.
- [x] **Connection Pool**: Thread-safe pooling for high-throughput transactional operations.
- [x] **Async/Await**: Fully asynchronous API design supporting `CancellationTokens`.

## 🚧 Limitations / Roadmap

The following features are **not** (yet) implemented:
- Events & Alarms (EventNotificationList).
- History Access (HDA).
- Complex Discovery Services (GDS).
- XML Encoding (only Binary is supported).
- Other Security Policies / Algorithms than Basic256Sha256.

## 📦 Architecture

The library is split into logical layers:

1.  **Builtin**: OPC Type definitions (`Variant`, `DataValue`, `NodeId`).
2.  **Client**: Client classes for data access, pooling and subscriptions.
3.  **Encoding**: Classes for binary encoding and decoding.
4.  **Security**: Everything security related (policies, certificate factory, ...).
5.  **Stack**: Implementation of the OPC UA stack / services.
6.  **Transport**: The underlying TCP connection logic.

## 🤝 Contributing

Contributions are welcome! Since this is a "from scratch" implementation, please ensure any PRs maintain the zero-dependency philosophy for the main library.

## ⚖️ License

This project is licensed under the **Apache License 2.0**. See the [LICENSE](https://github.com/philipp2604/LiteUa/blob/master/LICENSE.txt) file for details.

You are free to use, modify, and distribute this software in **commercial** and **private** applications.

---
*Built with ❤️ and a lot of reverse engineering.*

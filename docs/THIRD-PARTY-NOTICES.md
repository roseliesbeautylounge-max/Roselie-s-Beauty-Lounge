# Third-party components

The application uses Microsoft.NET10/WPF and Microsoft.Extensions.DependencyInjection10.0.12 (MIT), Entity Framework Core/Microsoft.Data.Sqlite.Core10.0.12 (MIT), SQLitePCLRaw2.1.11 (Apache-2.0), and Konscious.Security.Cryptography.Argon2 1.3.1 / Blake2 1.1.1 (MIT). Windows DPAPI, printing and shortcuts are operating system services.

SQLitePCLRaw's `e_sqlcipher` package contains community SQLCipher native builds. Its package metadata explicitly identifies those builds as unofficial and unsupported by Zetetic. The encrypted database is verified by integration tests; obtain and validate the vendor-supported SQLCipher runtime if commercial vendor support is required.

SQLCipher's upstream code is distributed under the BSD-style SQLCipher license, SQLite is public domain, and native crypto components have their own licenses. See the [SQLitePCLRaw repository](https://github.com/ericsink/SQLitePCL.raw), [SQLCipher license](https://github.com/sqlcipher/sqlcipher/blob/master/LICENSE), [Konscious repository](https://github.com/kmaragon/Konscious.Security.Cryptography) and [.NET notices](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT). Preserve included runtime license and notice files when distributing the binaries. Review native dependency notices and release licensing before commercial distribution.

xUnit and Microsoft.NET.Test.Sdk are development-only test dependencies and are not bundled in the operating application. Excel and PDF export use application code rather than a separately licensed report renderer.

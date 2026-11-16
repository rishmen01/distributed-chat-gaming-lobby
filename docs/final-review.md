# Final source review (16 November 2026)

## Scope

Reviewed the packaged solution structure, project references, WCF contracts and service implementation, in-memory data layer, WPF XAML/code-behind pairing, client polling lifecycle, application configuration and test documentation.

## Checks completed

- Seven project files and their explicit project references/items were checked for missing paths.
- XAML, project, application configuration and resource XML parsed successfully.
- Thirty-six XAML event-handler references were checked against the corresponding code-behind files.
- The duplex and polling clients remain separate projects and use separate service contracts/endpoints.
- The service uses a single-instance, single-concurrency WCF service implementation for its session/callback dictionaries; the database protects its shared collections with locks.
- The server enforces username/session identity, one-room membership, room membership for room operations, same-room private-message sending, and membership-checked file downloads.
- File uploads are restricted by extension and size; image signatures are checked for supported image types.
- A database smoke-test project and manual multi-client test plan are included.

## Findings and limitations

- The application is still a .NET Framework 4.8 WPF/WCF solution. It must be built and run on Windows with the .NET Framework 4.8 Developer Pack and Visual Studio/MSBuild.
- No compiler or WPF/WCF runtime is available in the preparation environment. The smoke-test harness has therefore not been compiled or executed, and no end-to-end test is claimed.
- WCF transport security is disabled and endpoints bind to localhost. Do not expose this configuration to an untrusted network.
- The data tier and lobby state are in-memory; registrations, rooms and message history are lost when the server stops. Shared-file bytes remain on disk.
- The clients' main-window code-behind still combines view event handling, service calls and presentation state. This is workable for the assignment but is not a clean MVVM separation; a full MVVM refactor should be a separate, tested change rather than a rushed structural rewrite.
- The current UI uses resizable WPF grids and flexible composer columns, but it does not implement a verified breakpoint-based reflow from two columns to stacked panels. Test the actual target window sizes in Windows and treat narrow-window reflow as an outstanding enhancement if required by the rubric.

## Release gate

Do not describe this package as fully tested until the solution builds in Visual Studio in Debug and Release and the complete manual test plan passes with multiple clients, including mixed duplex/polling clients, negative authorization tests and file transfer.

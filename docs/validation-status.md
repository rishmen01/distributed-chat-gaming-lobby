# Validation status

## Static checks completed in the preparation environment

- Parsed all project, XAML, application configuration, resource and settings XML files successfully (23 files).
- Checked all seven solution project paths and the explicit project items/references in each `.csproj` file; no missing paths were found.
- Checked that the duplex and polling contracts' declared operations have corresponding methods in `LobbyService`.
- Checked that the duplex client uses `DuplexChannelFactory<InterfaceLobbyDuplex>` and the `/LobbyService/Duplex` endpoint, while the polling client uses the project-defined `PollingChannelFactory<InterfaceLobbyPolling>` wrapper over WCF `ChannelFactory<T>` and the `/LobbyService/Polling` endpoint.
- Checked WPF XAML event-handler names against their code-behind files.
- Checked the two independent client code-behind files for balanced delimiters; no conditional compilation is used to switch a client between polling and duplex modes.
- Checked the packaged source for generated binaries, build directories, local databases, identity documents and runtime-shared files; none are included.

## Not run in this environment

The preparation environment does not include the Windows .NET Framework 4.8 Developer Pack, Visual Studio/MSBuild, WPF runtime or a C# compiler. Therefore:

- The revised Visual Studio solution has **not** been compiled here.
- The `DatabaseLibrary.SmokeTests` console harness has been added but **has not** been compiled or executed here.
- WCF service hosting, multi-session login, duplex callbacks, polling, WPF interaction and file transfer have **not** been runtime-tested here.

Run the solution and follow `manual-test-plan.md` on Windows before claiming that the corrected application builds and works. Record the actual build and test outcomes in this file after execution.

## User-reported Visual Studio build correction

The first Windows build reported CS0136 at `MainWindow.xaml.cs` line 543 in both `GamingLobbyDuplexClient` and `GamingLobbyPollingClient`. The local paragraph variable was renamed to `paragraph`; the proxy lambda parameter remains `p`. This resolves the reported name-shadowing conflict. A new Windows rebuild is still required to identify any subsequent compiler errors or warnings.


## Follow-up review: authentication UI, private chat and file links

Source-level checks after the follow-up changes:

- Both client XAML files name the controls needed for login-aware enable/disable state. All room and chat actions start disabled; login and username entry remain available.
- Both code-behind files centralise authenticated and room-action state changes. Login success, login failure, logout, room entry/leave and room selection update the controls.
- Both clients filter the current username out of the displayed room roster, with case-insensitive comparison.
- Both private-chat windows render messages authored by the local user with the label `You`.
- Both chat `RichTextBox` controls set `IsDocumentEnabled="True"`; the filename hyperlink already has a download handler and now has explicit visual/cursor affordances.
- The polling client now seeds message and private-message counts on room entry, and protects the polling state maps/sets used by both the UI and worker.
- The duplex client continues to use `DuplexChannelFactory<InterfaceLobbyDuplex>` and callback methods; the polling client continues to use `PollingChannelFactory<InterfaceLobbyPolling>` and `StartPolling`/`PollServerOnce` in the dedicated polling-client implementation.

Microsoft documents `RichTextBox.IsDocumentEnabled` as enabling interaction with UIElement and ContentElement objects inside the RichTextBox; the default is false. See https://learn.microsoft.com/en-gb/dotnet/api/system.windows.controls.richtextbox.isdocumentenabled?view=netframework-4.8.1.

These are static source/configuration checks only. A Visual Studio rebuild and interactive Windows testing are still required; no claim of runtime success is made here.


## Client-mode naming clarification

- The duplex application has a dedicated source implementation and uses `DuplexChannelFactory<InterfaceLobbyDuplex>` with `InterfaceLobbyDuplexCallback` at `/LobbyService/Duplex`.
- The polling application has a dedicated source implementation and uses `PollingChannelFactory<InterfaceLobbyPolling>` at `/LobbyService/Polling`. `PollingChannelFactory<T>` is a small project-defined subclass of WCF's standard non-duplex `ChannelFactory<T>`; WCF does not provide a built-in type with the `PollingChannelFactory` name. Background polling is performed separately by the polling client's worker loop.
- The `POLLING_CLIENT` conditional compilation symbol has been removed. Neither WPF client contains the other client's channel setup or callback/polling implementation.

## Explicit Duplex/Polling naming refactor

Static checks after separating the two client implementations:

- The duplex client has `DuplexChannelFactory<InterfaceLobbyDuplex>`, implements `InterfaceLobbyDuplexCallback`, and connects to `/LobbyService/Duplex`.
- The polling client has `PollingChannelFactory<InterfaceLobbyPolling>`, connects to `/LobbyService/Polling`, and runs its own five-second polling worker.
- `PollingChannelFactory<TChannel>` is a project-defined subclass of WCF's built-in non-duplex `ChannelFactory<TChannel>`. WCF does not provide a built-in `PollingChannelFactory`; the project-defined name makes the polling client's role explicit.
- Removed the `POLLING_CLIENT` compilation symbol and `#if` blocks. The duplex client no longer contains polling state/worker logic, and the polling client no longer contains duplex callback code.
- Renamed the duplex service and callback contracts to `InterfaceLobbyDuplex` and `InterfaceLobbyDuplexCallback`; the polling contract remains `InterfaceLobbyPolling`.
- Updated the self-hosted server to publish separate `/Duplex` and `/Polling` endpoint addresses.
- Static validation passed: 23 XML/project/XAML/configuration files parsed, seven solution project paths resolved, 30 C# files passed delimiter checks, and 19/19 mode/isolation/package checks passed.

These are static checks only. A Visual Studio rebuild and WCF runtime test on Windows are still required.


## Responsive UI and polling enhancements

- Replaced fixed-width message composer rows in both clients with responsive Grid layouts. The message input takes remaining width while Send and Share File buttons remain content-sized. Login, room action rows and the private-chat composer were also made responsive.
- Increased the main window minimum dimensions while retaining user resizing. Private-chat windows are resizable and their message input grows/shrinks with the window.
- Updated the polling worker to use a five-second start-to-start cadence where possible. It measures each cycle and waits only for the remaining interval.
- Added a Polling-client **Refresh Now** action that immediately refreshes the room list, roster, messages, shared-file notices and private-message histories without replacing the automatic timer. A semaphore prevents the manual and automatic polling cycles from overlapping.
- Added a **Refresh** action inside Polling private-chat windows for immediate conversation-history reload. Private-message history continues to be checked by the central five-second polling loop, including for open private chats; it does not create one timer per conversation.
- Fixed the first-seen participant case: messages received since the local user joined the room can be surfaced even when another player joins later and has no existing message-count snapshot.
- Added manual Windows test scenarios for resizing, five-second polling, immediate refresh and private-chat refresh.

These remain source-level changes. No Visual Studio/.NET Framework build or live WCF test was performed in the preparation environment.

## Responsive layout and polling refresh update

Static checks after the responsive-layout and five-second polling update:

- Parsed all 23 XAML, project, configuration, resource and settings XML files.
- Checked all explicit `.csproj` source/resource/page items; no missing project items were found.
- Checked all seven solution project paths; none were missing.
- Checked XAML event-handler names against code-behind methods; no missing handlers were found.
- Confirmed neither main message composer retains the old fixed `Width="520"` text box and both use responsive Grid columns for the input, Send and Share File controls.
- Confirmed the Polling client uses a five-second start-to-start cadence and no longer contains the old two-second delay.
- Confirmed the Polling client polls private-message history, supports `Refresh Now`, supports private-conversation history refresh, serialises overlapping polling cycles, and handles a participant first seen after room entry.
- Confirmed the Duplex client remains callback-driven and contains no polling worker or polling delay.
- Confirmed no generated build output or binaries are included in the source folder.

The environment does not provide `dotnet` or `msbuild`; therefore no compile, WPF runtime or WCF integration test has been executed here. The user must rebuild and run the solution in Visual Studio on Windows.

- Manual private-history refresh advances the automatic polling count baseline while holding the polling gate; both private-chat windows keep a displayed-message key set to prevent duplicate append operations after history reloads.

## Independent preparation-environment recheck (16 November 2026)

The packaged source was extracted afresh and rechecked:

- All seven `.csproj` files and all XAML, application configuration and resource XML files parsed successfully.
- All project references and explicitly listed project items resolved to files in the package.
- All 36 XAML event-handler references found in the checked views had matching code-behind method declarations.
- The source contains 30 C# files; no `TODO`, `FIXME`, or `NotImplementedException` markers were found in the C# source.
- No .NET SDK, MSBuild, Mono compiler or C# compiler is installed in this environment. Consequently no C# compilation, smoke-test execution, WCF hosting, or WPF runtime test could be performed.

This is a static-validation result, not a passing build or runtime-test result. The Windows build and manual multi-client test plan remain mandatory before submission or public release.

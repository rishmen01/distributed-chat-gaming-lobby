# Assignment 1 – Review and Enhancement Report

## User-requested changes

### 1. Responsive Send and Share File controls

Both main WPF clients previously used a fixed-width 520-pixel message input followed by fixed-width buttons inside a horizontal `StackPanel`. This could overflow or fail to adapt when the available width changed.

The message composer now uses a three-column `Grid`: the input uses `Width="*"`, while Send and Share File use `Width="Auto"` with minimum widths. Login and room-action rows were also converted to responsive grids. Both private-chat windows now use a flexible message input plus content-sized action buttons. Main and private-chat windows remain resizable and have sensible minimum dimensions.

### 2. Five-second polling

The Polling client now uses a five-second start-to-start cadence where the request cycle completes within five seconds. Each iteration measures its elapsed time and waits only for the remaining portion of the five-second interval. The Duplex client has no polling timer; it remains callback-driven.

### 3. Private-message polling

Private-message history was already queried by the central Polling worker for each other participant in the active room. This design is retained rather than creating a separate timer in every private-chat window. New private messages are routed to the matching open conversation window; if the conversation window is closed, opening it loads the complete history.

A first-seen participant edge case is corrected: when a participant joins after the current client has already joined the room, messages received since the current client's room-entry timestamp are not silently discarded just because a history-count baseline did not exist yet.

### 4. Manual refresh

The Polling client now includes a **Refresh Now** action. It immediately synchronises the room list, room roster, room messages, shared-file notices and private-message histories. This is supplemental; it does not replace or disable the automatic five-second worker.

Polling private-chat windows also have an immediate **Refresh** action to reload the full conversation history. A `SemaphoreSlim` serialises full polling cycles and manual history refreshes to avoid overlapping reads and conflicting history-count updates.

## Component-by-component review

| Component | Current assessment | Notes |
|---|---|---|
| Server host | Good foundation | Hosts separate data, Duplex and Polling endpoints. |
| Shared contracts | Good foundation | Separate duplex callback and non-duplex polling contracts make the two client modes explicit. |
| Lobby service | Good for assignment scale | Single-instance/single-concurrency behaviour protects shared session, callback and room state. Server validates authenticated identity, room membership, private-chat participants and file access. |
| Data tier | Good for demonstration | Collections are protected by locks, and one-room-per-player membership is enforced. The store is in memory, not persistent. |
| Duplex client | Good foundation | Uses `DuplexChannelFactory` and server callbacks; it does not poll. |
| Polling client | Improved | Uses a single background worker, five-second cadence, private-message history polling, manual refresh, and serialized polling cycles. |
| Main window layout | Improved | Message composer, login and room actions adapt using Grid columns rather than fixed-width horizontal StackPanels. |
| Private-chat layout | Improved | Message input expands/contracts with the window; Polling private chat has a manual history refresh. |
| File sharing | Good foundation | File IDs, type/size checks, image signature validation, room membership checks and clickable download links are present. |

## Limitations that remain

1. **No persistence:** player registrations, rooms, messages and file metadata are stored in an in-memory singleton. They are lost when the server process exits. Shared file bytes are stored under `Server/SharedFiles`.
2. **Local demonstration security:** WCF uses `SecurityMode.None` and localhost endpoints. Do not expose the service to an untrusted network.
3. **Some UI operations are synchronous:** login, joining/leaving rooms, sending messages and file operations can still block the WPF UI while waiting for a WCF response. The polling cycle and manual refresh operations run in the background. Converting all user-triggered operations to asynchronous commands is a valuable next step, but should be done with dedicated Windows runtime testing.
4. **Polling payload efficiency:** the current polling implementation retrieves history snapshots and uses count-based offsets/deduplication. This is appropriate for the assignment's one-to-five-client demonstration, but it is less efficient as histories grow. A scalable follow-up would use stable message IDs or sequence cursors and server-side `Get...Since` operations.
5. **Server throughput:** the lobby service serialises operations to protect the shared state. This is appropriate for the assignment scale but limits throughput as client count grows.
6. **Reconnect behaviour:** a client whose WCF channel faults may need to log in again; robust automatic session recovery and stale-session cleanup are not yet a production-grade facility.

## Windows verification still required

The preparation environment does not include Visual Studio/MSBuild or the .NET Framework 4.8 WPF/WCF toolchain. XML/project paths, XAML event-handler references and source-level assertions have been checked, but the complete solution has not been compiled or run here. Follow `manual-test-plan.md` on Windows before treating the project as fully validated.

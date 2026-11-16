# Assignment 1 – Client and System Review

## Responsive interface

Both main WPF windows now use Grid-based message composers. The message TextBox receives remaining width and the Send / Share File buttons use content-sized columns. Login and room-action rows use the same approach. Both private-chat composers are responsive; the Polling private-chat additionally exposes a history Refresh button. The main windows have minimum dimensions to avoid an unusably narrow layout.

## Duplex client

- Uses `DuplexChannelFactory<InterfaceLobbyDuplex>` and the `/LobbyService/Duplex` endpoint.
- Receives room messages, roster changes, room-list changes, private messages and file notices using WCF callbacks.
- Does not contain a polling timer.
- The signed-in player is excluded from the private-chat recipient list.

## Polling client

- Uses `PollingChannelFactory<InterfaceLobbyPolling>` (the project's named wrapper over WCF's non-duplex `ChannelFactory<T>`) and the `/LobbyService/Polling` endpoint.
- A single background worker refreshes rooms, room members, messages, shared-file metadata and private-message histories for other members of the active room. The cadence is five seconds start-to-start where each cycle completes within that period.
- `Refresh Now` immediately runs a complete sync. It is supplemental and does not replace the automatic timer.
- Each private-chat window can manually reload its full history, while normal incoming private messages are delivered by the central polling loop. A separate timer per chat window is intentionally avoided.
- A semaphore prevents automatic polling, manual room refresh and manual private-history refresh from using overlapping poll cycles.
- When another player joins after the current user, the poller uses the room-entry timestamp to avoid silently discarding newly received private messages before that player's history-count snapshot exists.

## Server and data tier

- The self-hosted server exposes separate Duplex and Polling endpoints and retains a separate data-tier endpoint.
- The lobby service uses a single service instance with single concurrency to serialise access to in-memory session, callback and user-room state.
- The in-memory data store protects its collections with locks and enforces one room per player.
- Server-side operations validate the authenticated session, room membership, private-chat participants and shared-file access.
- File sharing uses file IDs; the server validates allowed extensions, non-empty content, a 5 MB maximum and signatures for common image formats.

## Known limitations to disclose

- The data tier is in-memory. Registrations, rooms, messages and file metadata are lost when the server process exits. File bytes are stored under the server's `SharedFiles` directory.
- The endpoints use `SecurityMode.None` and localhost addresses for a single-machine educational demonstration. Do not expose them to an untrusted network.
- The UI still uses synchronous WCF operations for some user-initiated actions (for example login, join, send and file upload/download). Those actions may temporarily block the UI when the service is slow. Moving all user-triggered calls to asynchronous commands would be a worthwhile next enhancement, but should be tested carefully against the shared WCF proxy and session lifecycle.
- A full Windows build and runtime validation are still required.

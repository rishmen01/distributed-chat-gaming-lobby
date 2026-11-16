# Assignment 1 requirements mapping

This mapping relates the corrected source tree to the requirements in the Distributed Computing Assignment 1 brief. It describes the implementation in this portfolio-preparation revision; it is not a university mark prediction or evidence of a completed runtime demonstration.

| Brief requirement | Implementation in this revision | Validation still required |
|---|---|---|
| Unique active usernames | `Server/LobbyService.cs` maps WCF session IDs to registered usernames and rejects a second active session for the same username. | Attempt duplicate login across both client types. |
| Lobby room creation, joining and leaving | `LobbyService` delegates room state to `DatabaseLibrary.Database`. | Test room create/join/leave with 1–5 clients. |
| One room per player | `Database.AddPlayerToRoom` checks existing memberships atomically. | Attempt to join a second room without leaving the first. |
| Public room messages | `SendRoomMessage` checks session identity and room membership before storing and distributing the message. | Verify only room members see messages; attempt a forged sender/room. |
| Private messages | `SendPrivateMessage` verifies sender identity and that both players are in the same room; history is returned only to a conversation participant. | Test both directions and an out-of-room recipient. |
| Text and image sharing | `ShareFile` restricts extensions, rejects empty or over-5-MB data, validates basic image signatures, and stores metadata with a file ID. | Test each accepted type, unsupported type, malformed image and size boundary. |
| Room-only file access | `GetSharedFiles` and `DownloadSharedFile` require room membership. | Attempt download from a non-member session. |
| Base WPF client uses polling | `GamingLobbyPollingClient` uses the project-defined `PollingChannelFactory<InterfaceLobbyPolling>` wrapper over WCF `ChannelFactory<T>` and a five-second background polling task. | Confirm new rooms, rosters, messages, private messages and files arrive without pressing Refresh. |
| Additional duplex WCF client | `GamingLobbyDuplexClient` uses `DuplexChannelFactory<InterfaceLobbyDuplex>` and callback methods. | Confirm callbacks arrive for room, roster, public/private message and file updates. |
| GUI login/logout and room navigation | Both clients share the same WPF interaction model; logout clears session and room membership. | Test reconnect, window close, duplicate login and logout. |
| 1–5 clients | The manual test plan includes mixed polling and duplex clients. | Execute on Windows and record the results. |

## Implementation notes

- User registration and active login are separate. A registered username remains available after logout; only its active session and room membership are removed.
- The server binds to localhost and uses `NetTcpBinding`. The contracts allow a session, while the TCP binding supplies the channel session used by the server to identify callers.
- The lobby service serialises access to its session/callback state. The data library protects its shared collections with locks.
- File bytes are stored beneath the server application's runtime `SharedFiles` directory. The in-memory metadata and chat state are reset when the server process exits.
- The image-signature check is a basic file-type sanity check, not a complete security scanner or image-decoder validation.

## Not yet demonstrated

A Windows build and live WCF/WPF run are still required. In particular, run the one-to-five-client manual test plan, verify WCF session behaviour and callback delivery, and test the 5-MB file-transfer boundary. Do not mark these requirements as runtime-tested until those checks have been completed.


### Supplemental polling controls

The Polling client keeps automatic background polling at a five-second cadence, as the primary update mechanism. The **Refresh Now** button provides an additional immediate sync and does not disable the timer. A private-chat **Refresh** button reloads that conversation's complete history. The Duplex client remains callback-driven and has no polling loop.

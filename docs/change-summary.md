# Assignment 1 correction summary

This is a portfolio-preparation revision of the original group assignment. The original submitted archive was kept unchanged.

## Main corrections

- Added a separate `GamingLobbyPollingClient` WPF application. It uses the project-defined `PollingChannelFactory<InterfaceLobbyPolling>` wrapper over WCF's standard non-duplex `ChannelFactory<T>` and a background task that polls room lists, room members, room messages, file metadata and private-message history. The existing `GamingLobbyDuplexClient` remains the duplex WCF callback client.
- Added separate polling and duplex WCF endpoints to the self-hosted `Server` application.
- Changed the WCF lobby contract session mode to `Allowed`. The NetTcpBinding is session-based and supplies a channel session; this allows the shared singleton lobby service to maintain per-channel identity while accepting multiple client channels.
- Kept registered player accounts after logout. Logout now clears the active session and room membership instead of deleting the player's registration.
- Added server-side session-to-player mapping. Lobby operations no longer trust a caller-provided username as proof of identity.
- Enforced one active room per player in the data layer and service.
- Added room-membership checks for roster, message, private-message, file-list and file-download operations.
- Restricted private messaging to players in the same room and constrained message length.
- Added file-type allowlisting, filename validation, basic image signature checks and a 5 MB upload limit.
- Added stable file IDs to stored file metadata and used those IDs for download links.
- Added buffered WCF message-size settings on clients and server so the configured file-sharing size is supported by the transport quotas.
- Added locks around shared in-memory collections and serialised access to the lobby service's session/callback state.
- Removed an unused duplicate lobby-contract source file and an unused file-service implementation.
- Added a console smoke-test project for data-layer invariants and a manual test plan for end-to-end WCF/WPF validation.
- Added Git ignore rules and portfolio documentation. Removed the identity/originality declaration from the public-facing copy; retain the original declaration privately for university records.

## What has not been verified here

- The projects target .NET Framework 4.8 and WPF/WCF. This environment did not have the Windows .NET Framework developer pack, Visual Studio/MSBuild or a C# compiler, so a solution build was not performed.
- The console smoke-test source has been added but has not been compiled or executed here.
- The server, both clients, callback delivery, polling cadence, file transfer and one-to-five-client scenarios still require a Windows/Visual Studio runtime test. Follow `manual-test-plan.md` and record the actual results before stating that the program has passed them.
- The WCF endpoints use `SecurityMode.None` and localhost addresses for the assignment's single-machine demonstration. Do not expose them to an untrusted network.

## Before publishing

- Replace the team placeholders in the root README with the actual three contributors, GitHub accounts and truthful contribution descriptions.
- Confirm that every group member agrees to the public release and check the unit's rules on publishing assessed work.
- Add references for any external source code, tutorial snippets or assets used in the submitted implementation.

## Follow-up compiler fix

Resolved CS0136 in both WPF clients by renaming the local `Paragraph` variable from `p` to `paragraph` in `AppendDownloadLink`. This avoids shadowing the `p` lambda parameter used for the WCF proxy operation.


## UI, private-chat and file-link follow-up

- Disabled logout, room selection, room creation, joining/leaving, player-list actions, private chat, message entry, sending and file-sharing controls before login. Login and the username field remain enabled. Successful login enables lobby controls; room-only controls are enabled only while a joined room is being displayed. Logout restores the pre-login state.
- Filtered the signed-in user out of the visible room-player list in both clients, and made the recipient-selection check case-insensitive.
- Private conversation history and new outgoing messages now label the local participant as `You` instead of repeating their username. Incoming-message routing also rejects a self-conversation and resolves the other participant defensively.
- Enabled `RichTextBox.IsDocumentEnabled` in both client chat views. WPF otherwise disables interaction with embedded `Hyperlink` elements by default. File links now have a hand cursor, blue underlined styling and a download click action.
- Improved polling snapshot initialisation at room entry so messages, shared files and private messages sent after joining are picked up by the next background poll instead of being mistaken for initial history. Poll-state dictionaries shared by the UI and polling worker are protected by a lock.
- Prevented roster/poll refreshes for a joined room from re-enabling room actions when the user has selected a different, not-joined room in the room list.
- Updated the window titles to clearly distinguish the Duplex and Polling clients.

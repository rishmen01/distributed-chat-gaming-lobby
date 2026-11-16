# Distributed Gaming Lobby (WCF / WPF)

A collaborative project that implements a Mortal Kombat X-themed gaming lobby using C#, .NET Framework 4.8, WCF (`netTcpBinding`) and WPF.

## Team

This was developed as a three-person group project. 

| Contributor | Email |
|---|---|
| Senal Senanayake | senanayakesenal@gmail.com |
| Vishma Bandara | vishma.bandara@gmail.com |
| Rishon Mendis | rishonmendis@gmail.com |

## Solution structure

```text
DistributedChat/
├── DatabaseLibrary/          # In-memory data store and serialisable models
├── DataTierServer/           # WCF data-tier contract and implementation
├── SharedContracts/          # Lobby service, callback and file-notice contracts
├── Server/                   # Hosts data tier and lobby service endpoints
├── GamingLobbyDuplexClient/        # Duplex WPF client (server push callbacks)
├── GamingLobbyPollingClient/ # WPF client using background polling
├── docs/                     # Manual test plan and design notes
└── DistributedChat.sln
```

## Architecture

### Server

The `Server` console application hosts the data-tier endpoint and two lobby endpoints:

- Data tier: `net.tcp://localhost:8000/DataService`
- Duplex lobby service: `net.tcp://localhost:8001/LobbyService/Duplex`
- Polling lobby service: `net.tcp://localhost:8001/LobbyService/Polling`

The data tier owns the registered-player, room, message and shared-file metadata collections. The lobby service validates the active WCF session, enforces room membership, routes messages and manages shared files. It exposes distinct service contracts for duplex and polling clients.

### Clients

- `GamingLobbyDuplexClient` uses duplex WCF callbacks for near-real-time room, message, roster, private-message and file notices.
- `GamingLobbyPollingClient` uses `PollingChannelFactory<InterfaceLobbyPolling>` and a background task to query room lists, room members, room messages, private-message history and shared-file metadata every five seconds. It does not use server push callbacks.
- `PollingChannelFactory<T>` is a small project-defined subclass of WCF's standard non-duplex `ChannelFactory<T>`. WCF has no built-in `PollingChannelFactory`; the custom name makes the polling client's intent explicit while preserving the normal WCF channel-factory behaviour.

The clients have separate WPF implementations and separate WCF service contracts: `InterfaceLobbyDuplex` with `InterfaceLobbyDuplexCallback` for the duplex client, and `InterfaceLobbyPolling` for the polling client.

## Features

- Username registration and one active session per username.
- Room creation, joining and leaving. A player may belong to only one room at a time.
- Public room messaging and private messaging between players in the same room.
- Image and text file sharing with room-membership checks on download.
- Separate duplex and polling clients.
- Login-aware UI: login is available before authentication; room operations are disabled until login succeeds; logout and room-only operations are enabled only when appropriate.
- The signed-in player is excluded from the room's private-chat recipient list; messages sent by the local player are labelled `You` in the private conversation.
- Shared file names are interactive download links in the room chat. The client opens a Save File dialog and downloads by file ID after the server verifies room membership.
- WCF session-based caller identification and server-side validation.

## Requirements

- Windows 10/11
- Visual Studio 2022 (or compatible Visual Studio with .NET Framework project support)
- .NET Framework 4.8 Developer Pack / targeting pack
- WPF and WCF development components

## Run locally

1. Open `DistributedChat.sln` in Visual Studio.
2. Rebuild the solution. Restore any missing .NET Framework reference assemblies if prompted.
3. Set `Server` as the startup project and run it. It starts the data-tier and lobby WCF hosts. Keep the console open.
4. Start one or more instances of `GamingLobbyDuplexClient` for callback/duplex operation, or `GamingLobbyPollingClient` for background polling. The polling client captures room/message/private-message baselines when a room is opened so newly arriving content is not mistaken for old history on its first poll.
5. Register a username (3–24 letters, numbers, `_` or `-`), log in, create or join a room, and test the features.
6. Use different usernames for simultaneous client instances. A username may only have one active session at a time.

The endpoints are configured for `localhost`, matching the assignment's single-machine demonstration setup.

## File-sharing rules

- Allowed: `.txt`, `.png`, `.jpg`, `.jpeg`, `.gif`, `.bmp`.
- Maximum size: 5 MB per file.
- Downloads are available only to users currently in the room where the file was shared.
- Shared file names are clickable hyperlinks inside the read-only WPF chat `RichTextBox`; `IsDocumentEnabled` is enabled on that control so its `Hyperlink.Click` handlers can run.
- Uploaded content is written under `Server/SharedFiles/`; this runtime folder is excluded from Git.

## Testing

Run the `DatabaseLibrary.SmokeTests` project for automated data-layer checks, then follow [`docs/manual-test-plan.md`](docs/manual-test-plan.md) to build and validate the solution with one to five simultaneous clients, including negative tests for invalid membership and file access. The code was statically reviewed and updated in this preparation environment, but this environment does not include the Windows .NET Framework/WPF toolchain. A successful Visual Studio rebuild and runtime test is still required before claiming the application has been fully tested.

## Important limitations and security note

This is an educational local-network demonstration, not a production-ready messaging service. WCF transport security is configured as `SecurityMode.None` and the endpoints bind to `localhost`; do not expose them to an untrusted network. Player registrations, rooms, message history and file metadata are stored in an in-memory singleton and are lost when the server process exits. Shared-file bytes remain under the server's `SharedFiles` directory.

## Academic and third-party attribution

Retain the original group attribution and cite any external snippets, tutorials or assets used. Obtain agreement from all group members and check the course publication rules before making the repository public.


## Polling behaviour and refresh controls

The polling client performs automatic background synchronisation on a five-second cadence. This includes lobby-room discovery, room roster updates, room messages, shared-file notices, and private-message history for participants in the active room. The main **Refresh Now** control requests an immediate synchronisation without disabling or replacing the automatic polling schedule. Each polling private-chat window also has a **Refresh** control for an immediate history reload.

The duplex client does not poll; it receives updates through WCF duplex callbacks. Its private-chat updates are delivered through the callback contract.

The message composer uses a responsive grid: the input field takes the available width and the Send and Share File buttons retain their required widths. The login and room-action rows and both private-chat composers use the same responsive layout approach.

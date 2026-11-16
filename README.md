# Distributed Gaming Lobby (WCF / WPF)

A collaborative multiplayer gaming lobby application inspired by Mortal Kombat X, developed using C#, .NET Framework 4.8, Windows Communication Foundation (WCF), and Windows Presentation Foundation (WPF).

The application demonstrates distributed client-server communication through two approaches: WCF Duplex communication and background polling.

## Team

This project was developed collaboratively by:

| Contributor | Email |
|---|---|
| Senal Senanayake | [senanayakesenal@gmail.com] |
| Vishma Bandara | [vishma.bandara@gmail.com] |
| Rishon Mendis | [rishonmendis@gmail.com] |

## Solution Structure

```text
DistributedChat/
├── DatabaseLibrary/              # In-memory data store and serialisable models
├── DataTierServer/               # WCF data-tier contract and implementation
├── SharedContracts/              # Lobby service, callback and file-notice contracts
├── Server/                       # Hosts data tier and lobby service endpoints
├── GamingLobbyDuplexClient/       # Duplex WPF client (server push callbacks)
├── GamingLobbyPollingClient/      # WPF client using background polling
├── docs/                          # Manual test plan and design notes
└── DistributedChat.sln
```

## Architecture

### Server

The `Server` console application hosts the data-tier endpoint and two lobby endpoints:

- Data tier: `net.tcp://localhost:8000/DataService`
- Duplex lobby service: `net.tcp://localhost:8001/LobbyService/Duplex`
- Polling lobby service: `net.tcp://localhost:8001/LobbyService/Polling`

The data tier manages the registered-player, room, message and shared-file metadata collections. The lobby service validates active WCF sessions, enforces room membership, routes messages and manages shared files. Separate service contracts are provided for the Duplex and Polling clients.

### Clients

- **Duplex Client:** Uses WCF duplex callbacks for near-real-time room, message, roster, private-message and file notifications.
- **Polling Client:** Uses `PollingChannelFactory<InterfaceLobbyPolling>` and a background task to retrieve room lists, room members, room messages, private-message history and shared-file metadata every five seconds.

The polling client does not use server-push callbacks.

`PollingChannelFactory<T>` is a project-defined subclass of WCF's standard non-duplex `ChannelFactory<T>`. WCF does not provide a built-in `PollingChannelFactory`; the custom name makes the polling client's communication approach explicit while retaining standard WCF channel-factory behaviour.

The clients have separate WPF implementations and separate WCF service contracts:

- `InterfaceLobbyDuplex` with `InterfaceLobbyDuplexCallback` for the Duplex client.
- `InterfaceLobbyPolling` for the Polling client.

## Features

- Username registration and one active session per username.
- Room creation, joining and leaving.
- One-room-per-player membership enforcement.
- Public room messaging.
- Private messaging between players in the same room.
- Image and text file sharing with room-membership checks on download.
- Separate Duplex and Polling clients.
- Login-aware interface with controls enabled according to authentication and room membership.
- Private-chat recipient selection excludes the signed-in player.
- Messages sent by the local player are labelled `You` in private conversations.
- Interactive file-download links in the room chat.
- WCF session-based caller identification and server-side validation.

## Technologies

- C#
- .NET Framework 4.8
- Windows Communication Foundation (WCF)
- Windows Presentation Foundation (WPF)
- `netTcpBinding`
- Visual Studio 2022

## Requirements

- Windows 10 or Windows 11
- Visual Studio 2022 or a compatible version with .NET Framework project support
- .NET Framework 4.8 Developer Pack / targeting pack
- WPF and WCF development components

## Getting Started

1. Open `DistributedChat.sln` in Visual Studio.
2. Restore any missing dependencies or .NET Framework reference assemblies.
3. Rebuild the solution.
4. Set `Server` as the startup project and run it. It starts the data-tier and lobby WCF hosts. Keep the console window open.
5. Start one or more instances of `GamingLobbyDuplexClient` for callback-based communication, or `GamingLobbyPollingClient` for background polling.
6. Register a username and log in.
7. Create or join a room and start using the lobby features.

Use different usernames for simultaneous client instances. A username can only have one active session at a time.

The endpoints are configured for `localhost`, so the server and clients are expected to run on the same machine.

## File Sharing

Supported file types:

- `.txt`
- `.png`
- `.jpg`
- `.jpeg`
- `.gif`
- `.bmp`

Additional rules:

- Maximum file size: 5 MB per file.
- Downloads are available only to users currently in the room where the file was shared.
- Shared file names appear as clickable hyperlinks in the read-only WPF chat `RichTextBox`.
- The client displays a Save File dialog and downloads the file using its file ID after the server verifies room membership.
- Uploaded content is stored under `Server/SharedFiles/`. This runtime directory is excluded from Git.

## Testing

The `DatabaseLibrary.SmokeTests` project provides automated data-layer checks.

Additional scenarios can be tested using [`docs/manual-test-plan.md`](docs/manual-test-plan.md), including:

- Multiple simultaneous clients.
- Room creation, joining and leaving.
- Public and private messaging.
- Duplex callbacks and polling synchronisation.
- File uploads and downloads.
- Invalid room-membership and file-access attempts.

The source code has undergone static review. A successful full Visual Studio rebuild and runtime verification should be completed before treating the application as fully tested.

## Limitations and Security

This project is designed as a local-network demonstration rather than a production-ready messaging service.

- WCF transport security is configured as `SecurityMode.None`.
- The endpoints bind to `localhost` and should not be exposed to untrusted networks.
- Player registrations, rooms, message history and file metadata are stored in an in-memory singleton and are lost when the server process exits.
- Shared-file contents remain in the server's `SharedFiles` directory.

## Credits

Developed collaboratively by Senal Senanayake, Vishma Bandara and Rishon Mendis.

External code snippets, tutorials and third-party assets should be appropriately acknowledged.

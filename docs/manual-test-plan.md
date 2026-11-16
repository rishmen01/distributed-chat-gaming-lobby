# Manual test plan

Run these checks in Windows with Visual Studio and the .NET Framework 4.8 Developer Pack installed.

## Build checks
1. Open `DistributedChat.sln`.
2. Restore/rebuild the solution in Debug and Release.
3. Confirm that the Server, Duplex WPF client and Polling WPF client all build.
4. Start the `Server` project; it hosts the data-tier and lobby endpoints.

## Multi-client checks (1–5 clients)
1. Start one to five client instances using distinct usernames.
2. Register a username, log out, and log back in with the same username. Registration should remain available after logout.
3. Attempt to log in twice with the same username simultaneously. The second session should be rejected.
4. Create a room and verify that all connected clients see it.
5. Join a room with one client. Attempt to join a different room without leaving the first; the server should reject this.
6. Leave the room and join another room.
7. Send a public message. Only current room members should receive it.
8. Attempt to send as a different username or to a room the caller has not joined. The server should reject the operation.
9. Send a private message to another member of the same room and verify the separate private-chat window and history.
10. Attempt a private message to a player outside the room; the server should reject it.
11. Share a `.txt` file and supported image. Verify that room members can download them.
12. Attempt to share an unsupported extension, empty file and file larger than 5 MB; each should be rejected.
13. Attempt to download a file while not a member of its room; the server should reject the operation.
14. Compare duplex and polling clients: duplex clients should receive callback updates; polling clients should refresh lists/messages/files approximately every five seconds.
15. Close a client window and verify that its room membership is removed.

## Limitations to disclose
- This is an educational, single-machine WCF demonstration, not a production-ready chat platform.
- WCF `NetTcpBinding` uses `SecurityMode.None` and endpoints are bound to `localhost`; do not expose the service to untrusted networks.
- Player registrations, rooms and message history are held in an in-memory singleton and are lost when the server process stops. Shared-file content is written under the server's `SharedFiles` folder.
- The project targets .NET Framework 4.8 and WPF, so build and runtime validation must be performed on Windows.


## UI authentication state
1. Start either client without logging in. Confirm that Logout, room list, room creation, Join, Leave, player actions, Private Chat, Refresh Players, message input, Send and Share File are disabled. Login and username entry should remain enabled.
2. Log in successfully. Confirm that Logout and lobby/room-list controls become enabled, while room-only actions remain disabled until the player joins a room.
3. Join a room. Confirm that Leave, player refresh, private chat (when another player is present), message entry, Send and Share File become enabled.
4. Log out. Confirm that all authenticated controls return to the disabled state and the username field and Login become enabled again.

## Private-chat presentation
1. Join the same room using two clients.
2. Confirm that each client sees the other player in the Players in Room list but does not see its own username there.
3. Open a private chat and exchange messages in both directions.
4. Confirm that the local user's messages are labelled `You`, while the other participant's messages use that participant's username.
5. Confirm that a client cannot open a private chat with itself.

## File-link interaction
1. Join the same room using a duplex client and a polling client.
2. Share a small `.txt` file and a supported image from one client.
3. Confirm that the recipient sees the filename as a blue, underlined link and that the pointer changes to a hand when hovering over it.
4. Click the filename. Confirm that a Save File dialog appears and that the downloaded bytes match the uploaded file.
5. Repeat with the sender's own link, and with both client types.
6. Have a client leave the room and attempt to use an old file link; the server should reject the download because the client is no longer a room member.

## Polling/duplex distinction and initial-poll timing
1. Use `GamingLobbyDuplexClient` and verify that room/message/roster/private-message/file updates arrive through WCF callbacks without a polling loop.
2. Use `GamingLobbyPollingClient` and verify that it uses the `/LobbyService/Polling` endpoint and the background polling task.
3. Immediately after joining a room in the polling client, have another user send a room message, a private message and a file before the next five-second poll. Confirm that all three appear on the next poll.
4. Repeat the same functional tests with one to five clients, mixing polling and duplex clients.


## Additional UI and polling checks

1. Resize each main client between its minimum size and a large window size. Confirm the message input expands or contracts while Send and Share File remain visible.
2. In the polling client, confirm the last-refresh label updates and automatic synchronisation continues at approximately five-second intervals.
3. Click **Refresh Now** while logged in. Confirm rooms, players, messages, files and private messages synchronise immediately without freezing the UI.
4. Open a private conversation in the polling client. Send a private message from another client and confirm it appears within the next polling cycle.
5. Use the private window's **Refresh** control and confirm its complete history reloads without duplicate entries.
6. In the duplex client, confirm public messages, private messages, roster changes and shared-file notices arrive through callbacks without a polling timer.
7. Resize both private-chat windows and confirm the input and action buttons remain usable.


## Responsive controls and refresh behaviour

15. Resize the Duplex and Polling main windows from their minimum size to a larger size. Confirm the message composer expands/contracts and Send and Share File stay visible.
16. Resize both private-chat windows and confirm the message input expands/contracts without overlapping the action buttons.
17. In the Polling client, verify automatic refresh runs on a five-second cadence (allowing for the time taken to complete a server cycle).
18. Click **Refresh Now** with and without an active room. Confirm the rooms are refreshed in both cases, and roster/messages/files/private histories are synchronised when in a room.
19. Click the private-chat **Refresh** button and verify the full conversation history is reloaded without duplicate messages.
20. Open a room, then have a new player join and send a private message before the next poll. Confirm the recipient sees the message on the next cycle.
21. Confirm manual refresh does not disable or reset the automatic polling schedule, and overlapping poll cycles are prevented.

22. Open a private chat, then have the other participant leave the room. Confirm the related private-chat window closes after the room roster is updated.
23. Leave the room from the current client and confirm all private-chat windows close.

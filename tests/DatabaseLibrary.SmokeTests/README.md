# DatabaseLibrary smoke tests

A dependency-free console test harness for the critical in-memory data-layer invariants. Build and run `DatabaseLibrary.SmokeTests` from Visual Studio after building the solution. It checks duplicate registrations, one-room membership, leave/rejoin behaviour, message persistence, file metadata IDs, logout cleanup and concurrent join attempts.

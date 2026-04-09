This scene is for synthetic testing of network bandwidth and performance of a dedicated server running in batch mode.

Recommended setup
========================================
1. One machine running 1-N dedicated server instances.
2. Clients must always run on separate machines.

Examples
========================================
Fusion KCC.exe -batchmode -nographics -logfile logServer.txt -server -targetFrameRate 300 -waypoints 100 -radius 500 -aoiCellSize 32 -aoiPreset 1
Fusion KCC.exe -batchmode -nographics -logfile logClients.txt -players 10

Arguments
========================================
-server            - Starts dedicated server.
-players X         - Starts X clients (runners). Spawns X fake players and simulates their input if combined with -server argument.
-targetFrameRate X - Sets Application.targetFrameRate to X.
-waypoints X       - The server generates X waypoints.
-radius X          - Waypoints and players are spawned in distance <Radius * 0.5, Radius> from center.
-aoiCellSize X     - Calls Runner.SetAreaOfInterestCellSize(X).
-aoiPreset X       - Activate AoI preset from list defined in BatchPlayer.
-seed X            - Seed for environment random generator. All processes (server + clients) must use same seed.
-scene X           - Loads scene X upon startup (this works only if Startup is loaded for the first time).
-room X            - Custom room/session name.
-port X            - Custom port the server binds to.

Steps
========================================
1. Update Network Project Config.
    A. Set Peer Mode to Multiple.
    B. Set Replication Features to Scheduling and Interest Management.
    C. Set Object Data Consistency to Eventual.
    D. Update Client / Server tick and send rates.
2. Update EnvironmentSpawner parameters - map size, obstacles, ...
3. Add BatchClient scene to build settings as first scene or use -scene BatchClient command line argument.
4. Make a build, create shortcuts with command line arguments above and test.
5. Use KillAll.bat file to easily kill all processes.

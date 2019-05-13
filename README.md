# Setup Instructions
1. Clone, or download and unzip
2. Open BoggleLegend.sln
3. Build the solution in Visual Studio

The server (BoggleServer) or client (BoggleClient) can now be run depending on which project is "Set as StartUp Project".

# Notes
- Every two consecutive clients that connect to the server are paired together for gameplay. There is no direct way to choose an opponent.
- If you want to test a full game on one machine, i.e. run the server and two clients on the same machine, make sure the `if (firstPlayer.IP.Equals(newPlayer.IP))` statement/block in BoggleServer.cs is commented out.
- BoggleServer.cs also used to function as a web server that connected to a database and returned simple HTML. The database is no longer running and hence the web server code has been commented out.
- Some comments in the code my be outdated.

# Repository Overview
#### Boggle Client Installer
You may build this project to generate a .msi file which will formally install the client on Windows machines. This project is not required to run the server or client.
(see the BoggleClientInstaller README for details)

#### BoggleClient
Contains the client interface (MainWindow.xaml) and the client interface logic (MainWindow.xaml.cs)

#### BoggleClientModel
Contains the code (Model.cs) to communicate with the server

#### BoggleServer
Contains all the server code. All game logic/processing is handled by the server.
(see the BoggleServer README for details)

#### Resources
Contains game resources such as DLLs, sounds, and the list of legal words

#### BoggleLegend.sln
The Visual Studio solution file for the project

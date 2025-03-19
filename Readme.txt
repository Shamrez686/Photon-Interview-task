Project Overview
This project demonstrates a simple Photon integration for Android, implementing a two-player turn-based Tic Tac Toe game 
as per the given requirements. An APK file is attached for testing purposes.

Prototype Details
The prototype consists of two scenes:

Lobby Scene - Players enter and connect to the game.
Gameplay Scene - The Tic Tac Toe game is played turn by turn.

//////////////////////////////How to Play////////////////////////////////
Launch the Lobby Scene and connect.
Repeat the same process on another device to establish a connection.
Once connected, both players can take turns playing the game.

/////////////////////////////////Reconnect Player Feature//////////////////
To test the player reconnection feature:
Disconnect the internet on one device.
A popup message will appear indicating the disconnection.
Reconnect the internet within 30 seconds, as the TTL (Time to Leave Room) is set to 30 seconds.
If reconnected within this timeframe, the player will be able to resume the game seamlessly.
This implementation ensures a smooth multiplayer experience while handling disconnections effectively.
Things to do:

Potential Problems:

Testing:
- Don't know how to force beginSend or Recieve to return an exception in testcase.
- Don't know the best way to handle static testing.

Keep an eye for:
- Closing tcplistener and sockets
- Upcassing all messages.

DATABASE DESCRIPTION:
The database follows the same design that the assignment suggests with player,
game, and word tables.

For inserts into the DB, the code below is an example:

command.CommandText = "INSERT INTO Players(player_name) " +
	"VALUES (@player1_name)";
command.Prepare();               
command.Parameters.AddWithValue("player1_name", one.Name);
command.ExecuteNonQuery(); // This is in a try block

For reads from the DB, the code below is an example:

command.CommandText = "SELECT * FROM Players WHERE player_name='" + one.Name +
	"' OR player_name='" + two.Name + "'";
command.ExecuteReader() // This is in a using statement




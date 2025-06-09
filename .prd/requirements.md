# DB OPS Tool Requirements

##Goal

This is to be a simple TUI application. The purpose is to allow the user to select a POSTGRES DB instance from the list of configured instances and display information about active sessions, the statements those sessions are executing, how long they are taking, what locks they have and the status of those sessions.  It should NOT allow for any modification, it is read only.

1. The application should be a TUI. For each DB connection in its .json config file it will list the following:
    1. Any looged in sessions in the DB ordered by Database and Program logging in.
    1. For each session show:
        - Program
        - Machine
        - OSUser
        - Server
        - SID
        - Status
        - Terminal
        - Type
    1. Active sessions (query still executing), should be highlighted in Red.
    1. Allow the selection of each of those sessions.
    1. When selected by default show the Current Statement.
    1. Allow for viewing of the following for each session:
        1. Current Statement
            - Press S to view this
        1. Wait information:
            - Press W to view this
            - Event Name
            - Wait Time
            - Seconds in Wait
            - State
        1. Locking information
            - Press L to view this
            - Lock Type
            - Mode Held
            - Mode Requested
            - Any lock Ids.
            - Based on what Postgres provides highlight if the loc is blocking
        1. If the Q key is pressed in ANY control it should prompt user to quit.
1. The section that displays the current connection should stand out to the user with a different colored background (black would be a good attempt.)  However it should not be too bright or obnoxious
1. The application should have the ability to add a new connection.  Users will be prompted for standard Postgres connection credentials.
    1. These will be stored in a .json file.
    1. The passwords should be stored encrypted.
    1. The key for connecting shall be 'N'
    1. If the user presses N it will bring up a list of connections in the file.
    1. If the user presses + in that box it will bring up a box to add a new connection.
        - Host
        - Database
        - Port
        - Username
        - Password
    1. Only sessions for one connection can be dispalyed.
    1. If a duplication connection is attempted to be added an error should be displayed to the users.
1. The application should be .NET 8.0.
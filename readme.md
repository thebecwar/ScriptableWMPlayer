Command Line Scriptable Windows Media Player Component

Player Command Line Options

- `-alive` returns 'Alive' if the player is currently running, otherwise returns 'Dead'
- `-hideconsole` hides the console window spawned by the process
- `-noautoplay` suppresses the auto play
- `-autoclose` closes the player automatically when the playback is stopped
- `-playlist [file]` loads the playlist specified by `[file]`
- `-files [file1] [file2] [...]` starts the player with the items listed added to the current playlist. All arguments after `-files` are treated as filenames.

Player Return Codes

- -1 General Error
-  0 Ok

Control Command Line Options

- `-status` gets the current play state.
- `-play` starts playback.
- `-pause` pauses playback.
- `-stop` stops playback. If the player was started with `-autoclose` the player will close.
- `-next` plays the next item in the playlist.
- `-prev` plays the previous item in the playlist.
- `-mute` mutes the player.
- `-volume [value | +[+...] | -[-...]]`  sets the volume to the specified `value`, each `+` increases the current volume by 5%, each `-` reduces the volume by 5%.
- `-seek [value | +[+...] | -[-...]]` seeks the player by `value` seconds (can be negative), each `+` seeks 10 seconds, each `-` seeks back 10 seconds.
- `-close` closes the player.

Examples:

- Start the player from a batch file, playing `playlist.m3u`, and close when complete

    ```start scriptablewmpplayer.exe -hideconsole -autoclose -playlist playlist.m3u```

- Set the player volume to 30%

    ```scriptablewmpplayer.exe -volume 30```



# D2MultiPortConnector
Client-side proxy that redirects Game.exe connections from fixed port 4000 to configurable ports, enabling d2gs instances on one machine

## Usage

1. Place the built binary in the same folder as `Game.exe`
2. Create `serverlists.txt` in the same folder
3. Add server entries (one per line):
```
your-d2gs-server-1.com:4000
your-d2gs-server-1.com:4001
your-d2gs-server-1.com:4002
your-d2gs-server-2.com:4000
```

4. Enter your bnetd server domain or IP in the `Server IP` field and click `Start`
5. Change the game client gateway to `127.0.0.1` (required for proxy to work)

> Note: Gateway modification feature is built into the program.


## Server-side Port Configuration

You can change the D2GS listening port by patching `D2Net.dll` with a hex editor.

1. Open `D2Net.dll` in a hex editor
2. Go to offset `0x65AD` or search for:
```
   68 A0 0F 00 00 6A 03 51
```
3. `A0 0F` = `0x0FA0` = port `4000` (little-endian)
4. Replace with your desired port:
   | Port | Hex (little-endian) |
   |------|---------------------|
   | 4001 | `A1 0F` |
   | 4002 | `A2 0F` |
   | 4003 | `A3 0F` |
5. Save the modified `D2Net.dll` and restart D2GS

<img width="566" height="129" alt="image" src="https://github.com/user-attachments/assets/08ee326b-c633-410a-b751-870d1d6b2240" />

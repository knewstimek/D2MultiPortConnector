# D2MultiPortConnector
Client-side proxy that redirects Game.exe connections from fixed port 4000 to configurable ports, enabling d2gs instances on one machine

## Background

By default, D2CS only stores D2GS IP addresses in `gameservlist` and assumes a fixed port (4000). This makes it impossible to run multiple D2GS instances on a single machine with different ports.

To support multiple D2GS instances on one host:
1. D2CS must be modified to store IP:Port pairs in `gameservlist`
2. The JoinGame (`0x04`) response packet must include the port information

This project handles the modified JoinGame packet on the client side and redirects Game.exe to the correct port — without requiring any reverse engineering of the game client.

## Current Limitations

The current implementation does not fully handle the JoinGame (`0x04`) response with custom port information from D2CS.

To enable dynamic port routing, modify `PatchJoinGameReply` in `PacketPatcher.cs` to parse the custom D2CS packet containing the D2GS port. Once implemented, the proxy will be able to route Game.exe connections to the correct D2GS instance based on the port specified by D2CS.

## Usage (without D2CS modification)

> [!WARNING]
> This is for testing only. Full functionality requires D2CS modification.

1. Place the built binary in the same folder as `Game.exe`
2. Create `serverlists.txt` in the same folder
3. Add server entries (one per line):
```
your-d2gs-server-1.com:4001
your-d2gs-server-2.com:4002
```

> [!NOTE]
> Each IP can only map to one port. Use this for testing D2GS instances running on non-default ports.

4. Enter your bnetd server domain or IP in the `Server IP` field and click `Start`
5. Change the game client gateway to `127.0.0.1` (required for proxy to work)

> [!NOTE]
> Gateway modification feature is built into the program.


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

6. Launch `D2GS.exe` — it will now listen on your configured port

## Running Multiple D2GS Instances

On Windows, D2GS checks for an existing instance via `CreateMutex`. If you see "Seems another server is running", the mutex is blocking multiple instances.

### Workarounds

- **Wine (Linux)**: Use separate `WINEPREFIX` for each instance — mutex is isolated per prefix
- **VM/Docker**: Run each D2GS in a separate virtual environment
- **Patch D2GS**: NOP out the `CreateMutex` check in `D2GSCheckRunning`

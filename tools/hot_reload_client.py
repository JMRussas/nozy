#!/usr/bin/env python3
"""
NoZ Hot Reload Client — Demo & Debug Tool

Connects to the NoZ CommandServer WebSocket and sends reload commands.
Usage:
    python tools/hot_reload_client.py                    # Interactive mode
    python tools/hot_reload_client.py ping               # Health check
    python tools/hot_reload_client.py reload Texture hero # Reload specific asset
    python tools/hot_reload_client.py list               # List asset types
    python tools/hot_reload_client.py capture [path.png] # Capture current frame
    python tools/hot_reload_client.py watch               # Watch library/ and auto-reload

Requires: pip install websockets
Default port: 19999 (override with --port)
"""

import asyncio
import json
import sys
import argparse

try:
    import websockets
except ImportError:
    print("Install websockets: pip install websockets")
    sys.exit(1)


async def send_command(uri: str, cmd: dict, timeout: float = 10.0) -> dict:
    async with websockets.connect(uri) as ws:
        payload = json.dumps(cmd).encode("utf-8")
        await ws.send(payload)
        response = await asyncio.wait_for(ws.recv(), timeout=timeout)
        if isinstance(response, bytes):
            response = response.decode("utf-8")
        return json.loads(response)


async def interactive_mode(uri: str):
    print(f"Connected to {uri}")
    print("Commands: ping | reload <Type> <name> | list | capture [path] | quit")
    print()

    while True:
        try:
            line = input("noz> ").strip()
        except (EOFError, KeyboardInterrupt):
            break

        if not line:
            continue
        if line == "quit":
            break

        parts = line.split()
        cmd_name = parts[0]

        try:
            if cmd_name == "ping":
                result = await send_command(uri, {"cmd": "ping"})
            elif cmd_name == "reload" and len(parts) >= 3:
                result = await send_command(uri, {
                    "cmd": "reload",
                    "type": parts[1],
                    "name": parts[2],
                })
            elif cmd_name == "list":
                result = await send_command(uri, {"cmd": "list"})
            elif cmd_name == "capture":
                print("  Capturing frame...")
                result = await send_command(uri, {"cmd": "capture"}, timeout=30.0)
                if result.get("data"):
                    import base64
                    data = base64.b64decode(result["data"])
                    w, h = result.get("width", 0), result.get("height", 0)
                    path = parts[1] if len(parts) > 1 else "capture.raw"
                    with open(path, "wb") as f:
                        f.write(data)
                    print(f"  Saved {w}x{h} RGBA to {path} ({len(data)} bytes)")
                    result = {"ok": True, "width": w, "height": h, "saved": path}
            else:
                print(f"Unknown: {line}")
                continue

            print(json.dumps(result, indent=2))
        except Exception as e:
            print(f"Error: {e}")


async def watch_mode(uri: str, library_path: str):
    """Watch the library directory and auto-reload changed assets."""
    try:
        from watchdog.observers import Observer
        from watchdog.events import FileSystemEventHandler
    except ImportError:
        print("Watch mode requires: pip install watchdog")
        return

    type_map = {
        "texture": "Texture",
        "sprite": "Sprite",
        "shader": "Shader",
        "font": "Font",
        "sound": "Sound",
        "animation": "Animation",
        "skeleton": "Skeleton",
        "atlas": "Atlas",
        "vfx": "Vfx",
    }

    class ReloadHandler(FileSystemEventHandler):
        def __init__(self):
            self.queue = asyncio.Queue()

        def on_modified(self, event):
            if event.is_directory:
                return
            path = event.src_path.replace("\\", "/")
            parts = path.split("/")
            # Find library/{type}/{name} pattern
            for i, p in enumerate(parts):
                if p == "library" and i + 2 < len(parts):
                    type_dir = parts[i + 1]
                    name = parts[i + 2].rsplit(".", 1)[0]  # strip extension
                    asset_type = type_map.get(type_dir)
                    if asset_type:
                        self.queue.put_nowait((asset_type, name))
                    break

    handler = ReloadHandler()
    observer = Observer()
    observer.schedule(handler, library_path, recursive=True)
    observer.start()
    print(f"Watching {library_path} for changes... (Ctrl+C to stop)")

    try:
        while True:
            try:
                asset_type, name = handler.queue.get_nowait()
                print(f"  Reloading {asset_type}/{name}...")
                result = await send_command(uri, {
                    "cmd": "reload",
                    "type": asset_type,
                    "name": name,
                })
                print(f"  -> {json.dumps(result)}")
            except asyncio.QueueEmpty:
                await asyncio.sleep(0.2)
    except KeyboardInterrupt:
        observer.stop()
    observer.join()


def main():
    parser = argparse.ArgumentParser(description="NoZ Hot Reload Client")
    parser.add_argument("--port", type=int, default=19999)
    parser.add_argument("--host", default="localhost")
    parser.add_argument("command", nargs="?", default=None,
                        help="ping | reload | list | capture | watch")
    parser.add_argument("args", nargs="*")

    args = parser.parse_args()
    uri = f"ws://{args.host}:{args.port}"

    if args.command is None:
        asyncio.run(interactive_mode(uri))
    elif args.command == "ping":
        result = asyncio.run(send_command(uri, {"cmd": "ping"}))
        print(json.dumps(result, indent=2))
    elif args.command == "reload" and len(args.args) >= 2:
        result = asyncio.run(send_command(uri, {
            "cmd": "reload",
            "type": args.args[0],
            "name": args.args[1],
        }))
        print(json.dumps(result, indent=2))
    elif args.command == "list":
        result = asyncio.run(send_command(uri, {"cmd": "list"}))
        print(json.dumps(result, indent=2))
    elif args.command == "capture":
        import base64
        result = asyncio.run(send_command(uri, {"cmd": "capture"}, timeout=30.0))
        if result.get("data"):
            data = base64.b64decode(result["data"])
            path = args.args[0] if args.args else "capture.raw"
            with open(path, "wb") as f:
                f.write(data)
            print(f"Saved {result.get('width')}x{result.get('height')} RGBA to {path}")
        else:
            print(json.dumps(result, indent=2))
    elif args.command == "watch":
        library_path = args.args[0] if args.args else "library"
        asyncio.run(watch_mode(uri, library_path))
    else:
        parser.print_help()


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
#  NoZ MCP Bridge
#
#  MCP server that bridges Claude Code to a running NoZ engine instance.
#  Connects to the NoZ CommandServer via WebSocket and exposes tools for
#  frame capture, asset hot-reload, and engine inspection.
#
#  Depends on: websockets, mcp
#  Used by:    Claude Code (via .mcp.json)

import asyncio
import base64
import io
import json
import logging
import struct
import sys
import zlib

import websockets
from mcp.server.fastmcp import FastMCP

logging.basicConfig(level=logging.INFO, stream=sys.stderr)
log = logging.getLogger("noz-bridge")

NOZ_WS_URL = "ws://localhost:19999"

mcp = FastMCP("noz-bridge")


async def _send_command(cmd: dict, timeout: float = 10.0) -> dict:
    """Send a JSON command to the NoZ CommandServer and return the response."""
    async with websockets.connect(NOZ_WS_URL) as ws:
        payload = json.dumps(cmd).encode("utf-8")
        await ws.send(payload)
        response = await asyncio.wait_for(ws.recv(), timeout=timeout)
        if isinstance(response, bytes):
            response = response.decode("utf-8")
        return json.loads(response)


def _rgba_to_png(data: bytes, width: int, height: int) -> bytes:
    """Encode raw RGBA pixels as a PNG file (no PIL dependency)."""
    def _chunk(chunk_type: bytes, data: bytes) -> bytes:
        c = chunk_type + data
        crc = struct.pack(">I", zlib.crc32(c) & 0xFFFFFFFF)
        return struct.pack(">I", len(data)) + c + crc

    header = b"\x89PNG\r\n\x1a\n"
    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)  # 8-bit RGBA
    stride = width * 4

    # Add filter byte (0 = None) before each row
    raw_rows = bytearray()
    for y in range(height):
        raw_rows.append(0)  # filter byte
        row_start = y * stride
        raw_rows.extend(data[row_start:row_start + stride])

    compressed = zlib.compress(bytes(raw_rows), 9)

    png = header
    png += _chunk(b"IHDR", ihdr)
    png += _chunk(b"IDAT", compressed)
    png += _chunk(b"IEND", b"")
    return png


@mcp.tool()
async def noz_ping() -> str:
    """Check if the NoZ engine is running and responsive."""
    try:
        result = await _send_command({"cmd": "ping"})
        return json.dumps(result)
    except Exception as e:
        return json.dumps({"ok": False, "error": f"Cannot connect to NoZ: {e}"})


@mcp.tool()
async def noz_list() -> str:
    """List all registered asset types in the running NoZ engine."""
    try:
        result = await _send_command({"cmd": "list"})
        return json.dumps(result)
    except Exception as e:
        return json.dumps({"ok": False, "error": str(e)})


@mcp.tool()
async def noz_reload(asset_type: str, name: str) -> str:
    """Hot-reload an asset in the running NoZ engine.

    Args:
        asset_type: Asset type (Texture, Sprite, Shader, Font, Sound, Animation, Skeleton, Atlas, Vfx)
        name: Asset name (without extension)
    """
    try:
        result = await _send_command({
            "cmd": "reload",
            "type": asset_type,
            "name": name,
        })
        return json.dumps(result)
    except Exception as e:
        return json.dumps({"ok": False, "error": str(e)})


@mcp.tool()
async def noz_capture(save_path: str = "") -> str:
    """Capture the current frame from the NoZ engine.

    Returns the frame as a base64-encoded PNG. If save_path is provided,
    also saves the PNG to that file path.

    Args:
        save_path: Optional file path to save the PNG (e.g. "C:/tmp/frame.png")
    """
    try:
        # Capture has longer timeout — GPU readback is async
        result = await _send_command({"cmd": "capture"}, timeout=30.0)

        if not result.get("ok", True) or "error" in result:
            return json.dumps(result)

        width = result["width"]
        height = result["height"]
        rgba_b64 = result["data"]
        rgba_bytes = base64.b64decode(rgba_b64)

        # Encode as PNG
        png_bytes = _rgba_to_png(rgba_bytes, width, height)
        png_b64 = base64.b64encode(png_bytes).decode("ascii")

        if save_path:
            with open(save_path, "wb") as f:
                f.write(png_bytes)
            log.info(f"Saved capture to {save_path} ({width}x{height})")

        return json.dumps({
            "ok": True,
            "width": width,
            "height": height,
            "png_base64": png_b64,
            "saved_to": save_path if save_path else None,
        })
    except Exception as e:
        return json.dumps({"ok": False, "error": str(e)})


if __name__ == "__main__":
    mcp.run()

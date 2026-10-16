"""Use the configured main-project Funplay MCP transport with its real tool schema."""
import argparse
import json
from pathlib import Path
import urllib.request

URL = "http://127.0.0.1:25823/"
ARCHIVE = Path(__file__).resolve().parent


def request(payload, session=None):
    headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
    if session:
        headers["Mcp-Session-Id"] = session
    req = urllib.request.Request(URL, data=json.dumps(payload).encode(), headers=headers)
    with urllib.request.urlopen(req, timeout=55) as response:
        raw = response.read().decode()
        session = response.headers.get("Mcp-Session-Id", session)
        if raw.startswith("event:") or raw.startswith("data:"):
            raw = next(line[6:] for line in raw.splitlines() if line.startswith("data: "))
        return json.loads(raw) if raw.strip() else {}, session


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("tool")
    parser.add_argument("--args", default="{}")
    parser.add_argument("--code", type=Path)
    parser.add_argument("--out", required=True)
    args = parser.parse_args()
    arguments = json.loads(args.args)
    if args.code:
        arguments.update(code=args.code.read_text(encoding="utf-8-sig"), skip_refresh=True)
    _, session = request({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {
        "protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "occ-hd2d-quality", "version": "1"}}})
    result, _ = request({"jsonrpc": "2.0", "id": 2, "method": "tools/call",
        "params": {"name": args.tool, "arguments": arguments}}, session)
    (ARCHIVE / (args.out + ".json")).write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    blocks = result.get("result", {}).get("content", [])
    for block in blocks:
        if block.get("type") == "text":
            print(block.get("text", "")[:5500])
    if result.get("error"):
        print(json.dumps(result["error"], ensure_ascii=False))


if __name__ == "__main__":
    main()

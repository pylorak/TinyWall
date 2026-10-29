#!/usr/bin/env python3
"""
TinyWall FastMCP Server (Full Capabilities)
Standard MCP implementation using FastMCP for Antigravity / Claude / Cursor.
"""

import sys
import os
import json
import subprocess
from mcp.server.fastmcp import FastMCP

# Initialize FastMCP Server
mcp = FastMCP("tinywall")

# Locate TinyWall.exe
DEFAULT_PATHS = [
    r"C:\Program Files (x86)\TinyWall\TinyWall.exe",
    os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Desktop", "TinyWall", "TinyWall", "bin", "Release", "TinyWall.exe")),
]

TINYWALL_EXE = None
for p in DEFAULT_PATHS:
    if os.path.exists(p):
        TINYWALL_EXE = p
        break

if not TINYWALL_EXE:
    TINYWALL_EXE = DEFAULT_PATHS[0]


def run_tinywall_cli(args: list[str]) -> dict:
    cmd = [TINYWALL_EXE, "cli"] + args + ["--json"]
    try:
        proc = subprocess.run(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            timeout=15
        )
        out = proc.stdout.strip()
        if not out and proc.stderr:
            return {"success": False, "error": proc.stderr.strip()}
        if not out:
            return {"success": False, "error": f"TinyWall CLI exited with code {proc.returncode}."}
        try:
            return json.loads(out)
        except json.JSONDecodeError:
            return {"success": proc.returncode == 0, "raw_output": out}
    except Exception as e:
        return {"success": False, "error": str(e)}


# ---------------------------------------------------------------------------
# 1. STATUS & MODES
# ---------------------------------------------------------------------------

@mcp.tool()
def tinywall_get_status() -> str:
    """Get current TinyWall status, active firewall mode, lock state, and active profile."""
    res = run_tinywall_cli(["status"])
    return json.dumps(res, indent=2)


@mcp.tool()
def tinywall_set_mode(mode: str) -> str:
    """Set TinyWall firewall mode: normal, blockall, allowoutgoing, disabled, or autolearn."""
    res = run_tinywall_cli(["mode", "set", mode])
    return json.dumps(res, indent=2)


# ---------------------------------------------------------------------------
# 2. GLOBAL CONFIG & SPECIAL EXCEPTIONS
# ---------------------------------------------------------------------------

@mcp.tool()
def tinywall_get_config() -> str:
    """Get global configuration options (allow local subnet, hosts blocklist, blocklists, display off block, etc.)."""
    res = run_tinywall_cli(["config", "get"])
    return json.dumps(res, indent=2)


@mcp.tool()
def tinywall_set_config(
    allow_local_subnet: str = "",
    blocklists: str = "",
    hosts_blocklist: str = "",
    display_off_block: str = "",
    lock_hosts: str = ""
) -> str:
    """Set global configuration options. Pass 'true' or 'false' for any option you wish to change."""
    args = ["config", "set"]
    if allow_local_subnet:
        args.extend(["--allow-local-subnet", allow_local_subnet])
    if blocklists:
        args.extend(["--blocklists", blocklists])
    if hosts_blocklist:
        args.extend(["--hosts-blocklist", hosts_blocklist])
    if display_off_block:
        args.extend(["--display-off-block", display_off_block])
    if lock_hosts:
        args.extend(["--lock-hosts", lock_hosts])

    res = run_tinywall_cli(args)
    return json.dumps(res, indent=2)


@mcp.tool()
def tinywall_list_special_exceptions() -> str:
    """List built-in special exception profiles (Windows Update, WSL 2, File & Printer Sharing, DHCP, DNS, etc.)."""
    res = run_tinywall_cli(["special", "list"])
    return json.dumps(res, indent=2)


@mcp.tool()
def tinywall_toggle_special_exception(name: str, enable: bool = True) -> str:
    """Enable or disable a built-in special exception profile by its ID or name (e.g. WSL_2, File_and_Printer_Sharing)."""
    action = "enable" if enable else "disable"
    res = run_tinywall_cli(["special", action, "--name", name])
    return json.dumps(res, indent=2)


# ---------------------------------------------------------------------------
# 3. ADVANCED RULES (EXE, SERVICES, UWP, TIMERS, PORTS)
# ---------------------------------------------------------------------------

@mcp.tool()
def tinywall_list_rules() -> str:
    """List all configured firewall application exceptions and rules in TinyWall."""
    res = run_tinywall_cli(["rule", "list"])
    return json.dumps(res, indent=2)


@mcp.tool()
def tinywall_allow_program(
    path: str,
    policy: str = "tcpudp",
    ports: str = "",
    timer: str = "permanent"
) -> str:
    """Allow an executable through TinyWall firewall.
    policy: 'tcpudp' or 'unrestricted'.
    ports: optional comma-separated list of remote ports (e.g. '80,443').
    timer: 'permanent', 'reboot', '5m', '30m', '1h', '4h', '9h', '24h'.
    """
    args = ["rule", "add", "--path", path, "--policy", policy, "--timer", timer]
    if ports:
        args.extend(["--ports", ports])
    res = run_tinywall_cli(args)
    return json.dumps(res, indent=2)


@mcp.tool()
def tinywall_allow_service(
    name: str,
    policy: str = "tcpudp",
    timer: str = "permanent"
) -> str:
    """Allow a Windows Service through TinyWall firewall by service name (e.g. Spooler, LanmanServer)."""
    res = run_tinywall_cli(["rule", "add-service", "--name", name, "--policy", policy, "--timer", timer])
    return json.dumps(res, indent=2)


@mcp.tool()
def tinywall_list_uwp_packages(filter: str = "") -> str:
    """List installed Microsoft Store / UWP packages with their names, display names, and SIDs."""
    args = ["rule", "list-uwp"]
    if filter:
        args.extend(["--filter", filter])
    res = run_tinywall_cli(args)
    return json.dumps(res, indent=2)


@mcp.tool()
def tinywall_allow_uwp_package(
    package: str,
    policy: str = "tcpudp",
    timer: str = "permanent"
) -> str:
    """Allow a Microsoft Store / UWP package through TinyWall by package name, display name, or SID."""
    res = run_tinywall_cli(["rule", "add-uwp", "--package", package, "--policy", policy, "--timer", timer])
    return json.dumps(res, indent=2)


@mcp.tool()
def tinywall_block_program(path: str) -> str:
    """Explicitly block an executable from accessing the network."""
    res = run_tinywall_cli(["rule", "block", "--path", path])
    return json.dumps(res, indent=2)


@mcp.tool()
def tinywall_remove_rule(path: str = "", service: str = "", id: str = "") -> str:
    """Remove a firewall rule by executable path, Windows service name, or rule GUID."""
    args = ["rule", "remove"]
    if path:
        args.extend(["--path", path])
    elif service:
        args.extend(["--service", service])
    elif id:
        args.extend(["--id", id])
    else:
        return json.dumps({"success": False, "error": "Either path, service, or id must be provided."}, indent=2)
    res = run_tinywall_cli(args)
    return json.dumps(res, indent=2)


# ---------------------------------------------------------------------------
# 4. EXPORT & IMPORT
# ---------------------------------------------------------------------------

@mcp.tool()
def tinywall_export_config(output_path: str) -> str:
    """Export the complete TinyWall configuration (all rules, profiles, settings) to a .tws file."""
    res = run_tinywall_cli(["export", "--output", output_path])
    return json.dumps(res, indent=2)


@mcp.tool()
def tinywall_import_config(input_path: str) -> str:
    """Import and immediately apply a complete TinyWall configuration (.tws file)."""
    res = run_tinywall_cli(["import", "--input", input_path])
    return json.dumps(res, indent=2)


# ---------------------------------------------------------------------------
# 5. LOGS
# ---------------------------------------------------------------------------

@mcp.tool()
def tinywall_get_recent_logs(limit: int = 25) -> str:
    """Retrieve recent firewall activity and blocked connections to diagnose connectivity issues."""
    res = run_tinywall_cli(["log", "--limit", str(limit)])
    return json.dumps(res, indent=2)


if __name__ == "__main__":
    mcp.run()

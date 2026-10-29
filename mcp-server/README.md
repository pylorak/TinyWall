# TinyWall Model Context Protocol (MCP) Server

This MCP server enables AI agents (such as Claude Desktop, Cursor, Antigravity, VS Code Copilot, and custom LLM workflows) to inspect, configure, and automate **TinyWall** through standardized declarative tools.

---

## 1. Prerequisites

- Python 3.10 or later
- The official FastMCP library:
  ```bash
  pip install mcp
  ```
- TinyWall installed and running on Windows.

---

## 2. Configuration for AI Agents

Add the server to your agent's configuration file (e.g., `claude_desktop_config.json` or Antigravity/Cursor MCP config):

```json
{
  "mcpServers": {
    "tinywall": {
      "command": "python",
      "args": [
        "C:\\path\\to\\TinyWall\\mcp-server\\tinywall_mcp.py"
      ]
    }
  }
}
```

---

## 3. Available MCP Tools

### Firewall Status & Mode Control
| Tool | Description | Parameters |
|---|---|---|
| `tinywall_get_status` | Retrieve current firewall mode, lock status, and active profile. | *None* |
| `tinywall_set_mode` | Switch the active firewall mode. | `mode`: `"normal"`, `"blockall"`, `"allowoutgoing"`, `"disabled"`, `"autolearn"` |

### Global Settings & Special Exceptions
| Tool | Description | Parameters |
|---|---|---|
| `tinywall_get_config` | Get global configuration settings. | *None* |
| `tinywall_set_config` | Modify global options (e.g., local subnet, blocklists, display off block). | `allow_local_subnet`, `blocklists`, `hosts_blocklist`, `display_off_block`, `lock_hosts` (`"true"` / `"false"`) |
| `tinywall_list_special_exceptions` | List built-in special exception profiles (Windows Update, WSL 2, File Sharing, etc.). | *None* |
| `tinywall_toggle_special_exception` | Enable or disable a built-in special exception profile. | `name` (e.g. `"WSL_2"`, `"File_and_Printer_Sharing"`), `enable` (`true` / `false`) |

### Application & Service Rules
| Tool | Description | Parameters |
|---|---|---|
| `tinywall_list_rules` | List all configured firewall rules and application exceptions. | *None* |
| `tinywall_allow_program` | Allow an executable through the firewall. | `path` (required), `policy` (`"tcpudp"` or `"unrestricted"`), `ports` (e.g. `"80,443"`), `timer` (`"permanent"`, `"1h"`, etc.) |
| `tinywall_allow_service` | Allow a Windows Service through the firewall. | `name` (service name, e.g. `"Spooler"`), `policy`, `timer` |
| `tinywall_list_uwp_packages` | List installed Microsoft Store / UWP packages and SIDs. | `filter` (optional search filter) |
| `tinywall_allow_uwp_package` | Whitelist a UWP package. | `package` (name, display name, or package SID), `policy`, `timer` |
| `tinywall_block_program` | Explicitly block an executable from accessing the network. | `path` (required) |
| `tinywall_remove_rule` | Remove an existing firewall rule. | `path`, `service`, or `id` (GUID) |

### Configuration Backup & Diagnostic Logs
| Tool | Description | Parameters |
|---|---|---|
| `tinywall_export_config` | Export the complete TinyWall configuration to a `.tws` file. | `output_path` (required) |
| `tinywall_import_config` | Import and apply a complete configuration from a `.tws` file. | `input_path` (required) |
| `tinywall_get_recent_logs` | Retrieve recent blocked connections and traffic events. | `limit` (default: 25) |

---

## 4. Architecture

Under the hood, `tinywall_mcp.py` communicates directly with `TinyWall.exe cli ... --json`.
The CLI communicates securely over the local named pipe (`\\.\pipe\TinyWallController`) with mutual process authentication against the active TinyWall Windows service.

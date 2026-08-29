# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

**TokenizerDesktop** is a Windows queue‑ticketing system (a college diploma project). It has two
WinForms apps plus a shared library, backed by a FastAPI/Postgres service that lives in a **separate
repo** at `D:\Developer_D\python\tokenizer-api`.

- **ButtonsApp** (`Buttons.exe`) — public kiosk. Renders one button per ticket type; pressing a
  button books a ticket via the API and prints it.
- **ManagerApp** (`Manager.exe`) — operator console. Live queue, call‑next, spoken number
  announcements + gong, ticket reprint, delete, settings editor, and batch ticket creation.
- **Shared** (`Shared.dll`) — config, HTTP client, JSON parsing, printing, audio, and the HTML UI
  pages/assets that both apps load.

UI text is Russian. Comments mix Russian and English.

## Build & run

This targets **.NET Framework 3.5** and builds with the legacy MSBuild — **not `dotnet build`**.

```
C:\Windows\Microsoft.NET\Framework\v3.5\MSBuild.exe ManagerApp\Manager.csproj
C:\Windows\Microsoft.NET\Framework\v3.5\MSBuild.exe ButtonsApp\Buttons.csproj
# add /p:Configuration=Release for release output
```

- **Do not build `TokenizerDesktop.sln`.** `ButtonsApp/Buttons.csproj` and `ManagerApp/Manager.csproj`
  reference Shared with the literal placeholder `{YOUR-SHARED-GUID}`, so the sln build is broken and
  is commented out in `.vscode/tasks.json`. Build the three `.csproj` files individually (Shared
  builds automatically as a project reference).
- `taskkill /IM Manager.exe /F` and `/IM Buttons.exe /F` before rebuilding — a running exe locks the
  output directory.
- All three projects output to `..\bin\$(Configuration)\`. Run `bin\Debug\Manager.exe` and
  `bin\Debug\Buttons.exe`.
- `.vscode/tasks.json` already encodes every combination: `Build All`, `Build Release All`,
  `Debug All`, `Release All`, and per‑app variants. Prefer these.
- There is **no test project and no linter** in either repo.

## Architecture: the WebBrowser + COM bridge

Both apps are the same shell: a `Form1` hosting a docked `WebBrowser` control pointed at
`file:///{StartupPath}\Pages\{buttons|manager}.html`, with `ObjectForScripting` set to a
`ScriptManager` instance. All UI logic lives in the HTML/JS; C# is just a native bridge.

- Each app defines its **own** `[ComVisible(true)] public class ScriptManager` inside `Forms/Form1.cs`
  (`ButtonsApp/Forms/Form1.cs`, `ManagerApp/Forms/Form1.cs`). These are separate classes with
  different method sets — not shared.
- JS invokes bridge methods as `window.external.MethodName(...)`. **Adding a UI feature = add a public
  method to the right `ScriptManager` and call it from the matching page in `Shared/Pages/`.**
- Bridge methods take and return **strings only**, and must never let an exception cross the boundary.
  The convention is to catch and return `"error:" + ex.Message` or `{"error":"..."}` (see
  `ManagerApp/Forms/Form1.cs`, `ScriptManager` methods).
- `Shared/Pages/*.html` must stay **legacy‑IE compatible**: ES3/ES5 only — `var`, no arrow functions,
  no `fetch`/Promises/template literals. Existing code even falls back to `eval('(' + json + ')')`
  when `JSON.parse` is unavailable.
- `manager.html` polls `GET /ticket/list/` on a `setInterval` whose period is the `refreshRate`
  config value; it has three pages (`manager` / `config` / `batch`) toggled by `showPage(...)`.
- Pages, `Audio/`, and icons are copied into `bin\` via `<Content ... PreserveNewest>` in
  `Shared/Shared.csproj`. **Editing an HTML/asset file requires rebuilding Shared** (or manually
  copying it into `bin\<cfg>\Pages\`).

## `Shared/` library

- **`Config.cs`** — flat `key=value` file at `%APPDATA%\TokenizerDesktop\config.ini`. Every
  `Get`/`Set` re‑reads / rewrites the whole file. Defaults are seeded by `StartConfig()` in
  `ManagerApp/Forms/Form1.cs`, so **ButtonsApp assumes ManagerApp has run at least once**. Two
  distinct config layers exist: local `Config` (per machine: `apiUrl`, `printer`, `printDebug`,
  `volume`, `voiceSkin`, `gongSkin`, `refreshRate`, window bounds, `print_*` layout keys) vs.
  server‑side `/settings/` (shared across machines: `QUEUE_POSITION_SCOPE`, slot rules).
- **`ApiManager.cs`** — `HttpWebRequest` wrapper. Base URL is `Config.Get("apiUrl")`. Request
  parameters go in the **query string**; POST bodies are empty. **All TLS certificates are accepted
  unconditionally** (`AcceptCertificate`) for the LAN deployment.
- **`JsonUtil.cs`** — a naive flat‑JSON string scanner (`GetString`). There is **no JSON library** in
  use: `Shared/lib/Newtonsoft.Json.dll` exists but every reference to it is commented out.
  **Consequence: any API response the C# side parses must expose its fields as flat top‑level keys.**
  This is why `tokenizer-api/app/routers/admin_tickets.py` returns `id/name/number/timestamp/
  created_at/position` at the top level instead of nesting them under `"ticket"`.
- **`PrinterManager.cs`** — GDI ticket drawing and the `Ticket` model. Layout is re‑read from the
  `print_*` config keys on every print. `printDebug=true` opens a `PrintPreviewDialog` instead of
  printing; a blank or `"None"` printer name is a no‑op.
- **`AudioManager.cs`** — master volume via `winmm.dll` `waveOutSetVolume`; playback via
  `SoundPlayer.PlaySync`. Number announcements are assembled digit‑by‑digit from
  `Audio\Voices\<skin>\{welcome,0-9}.wav`; gongs come from `Audio\Gongs\*.wav`. Skin lists are just
  directory/file enumerations.
- **`Shared/Program.cs`** — empty `Main`; Shared builds as a `Library`. Harmless leftover.

## Backend — `D:\Developer_D\python\tokenizer-api` (separate repo)

FastAPI + SQLAlchemy + Postgres 15, served by uvicorn on **port 9009**, `TZ=Europe/Moscow`.
Interactive docs at `http://localhost:9009/docs`.

```
docker-compose up --build                              # run
docker-compose down -v && docker-compose up --build    # full wipe + rebuild  (start.bat / start.sh)
```

`.env` supplies `DATABASE_URL` and `POSTGRES_USER/PASSWORD/DB`; data persists in `./pgdata`.

- **Runs only inside the container.** `Dockerfile` does `COPY ./app /app` while `docker-compose.yml`
  also mounts `./app:/app/app`, so modules resolve both as `app.logic` and as bare `logic`. The code
  mixes both import styles (`from logic import get_user` in `admin_tlist.py` / `user_book.py`,
  `from models import *` in `logic.py`). Plain `uvicorn app.main:app` on the host will fail on the
  bare imports.
- **Layout:** `app/main.py` mounts routers with URL prefixes and tags them `_admin` (consumed by the
  desktop apps) vs. `_user` (external / bot clients). `app/models.py` =
  User / TicketType / Course / Ticket / Log / Setting. `app/logic.py` = all business rules.
  `app/db.py` = engine, `get_db`, and startup seeding (`ensure_default_tickettypes`,
  `ensure_default_settings`).
- Runtime settings live in the `settings` DB table and are re‑read per request via `load_settings()`.
- Every HTTP request is written to the `logs` table by middleware in `app/main.py`; `GET /logs/`
  trims old rows down to the `MAX_LOGS` setting.
- Routers take **query parameters, not JSON bodies** (despite `app/schemas.py` existing), matching
  `ApiManager`'s empty‑body POSTs.

**Endpoints the desktop apps actually call:**

| Purpose | Call |
| --- | --- |
| List ticket types | `GET /ticket/types/` |
| Create ticket | `POST /ticket/?ticket_type_id=<id>` |
| Update ticket status | `POST /ticket/?id=<id>&status=<status>&timestamp=<iso>` |
| List all tickets (queue poll) | `GET /ticket/list/` |
| Delete tickets | `DELETE /delete/?ticket_ids=<id>&ticket_ids=<id>` |
| Read / write shared settings | `GET /settings/` · `POST /settings/?key=&value=` |

**Ticket semantics** (needed before touching either side):

- `name` = the printed ticket number, formatted `{symbol}-{zero-padded number}` (e.g. `Ë-0001`).
  **Never the type title** — the C# side deliberately avoids falling back to the button title.
- `created_at` = issue time (auto).
- `timestamp` = assigned time slot; set **only** for types with `require_time` (e.g. `debt`), and
  auto‑picked by `generate_timestamp`. Other types are next‑in‑line with no timestamp.
- `position` comes from `queue_position` and is scoped by the `QUEUE_POSITION_SCOPE` setting
  (`global` | `type`).

**Dead / unwired code — do not mistake for live:** `app/database.py` (superseded by `app/db.py`),
`app/main.py.backup`, `app/logic copy.py.backup`, `app/routers/admin_export.py` +
`admin_import.py` (never imported), `admin_courses` (imported in `main.py` but never
`include_router`‑ed).

## Conventions

- C# is written in a .NET 3.5 / C#‑2 style: explicit types, `Dictionary<string,string>` for
  parameters, anonymous `delegate(...)` instead of lambdas, string concatenation instead of
  interpolation, `ThreadPool.QueueUserWorkItem` for background audio. No LINQ, no `async`/`await`,
  no `?.` (but `??` is fine — it's C# 2 and already used). New code must compile under
  `TargetFrameworkVersion v3.5`.
- `.gitignore` excludes `*.zip` and `[Bb]in/`; the checked‑in `TokenizerDesktopApp.zip` and `bin/`
  are pre‑existing artifacts, not build output you should refresh.

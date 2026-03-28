# CLAUDE.md

This file provides context for AI assistants working in this repository.

## Project Overview

A GitHub Pages static site with two features:

1. **Concept Art Gallery** (`index.html`) — displays JPG images in a responsive dark-themed grid
2. **Novel Writing Team** (`novel.html` + `novel_team.py`) — orchestrates 5 Claude agents sequentially to write a short story from a user-supplied theme

The site is deployed directly from the repository root as a GitHub Pages site. There is no build step.

## Repository Structure

```
/
├── index.html        # Concept art gallery (landing page)
├── novel.html        # Web UI for multi-agent novel generation
├── novel_team.py     # CLI version of the same novel generation system
├── requirements.txt  # Python dependency: anthropic>=0.40.0
├── art01.JPG         # Gallery image assets
├── art_01.JPG
├── art_02.JPG
└── art_03.JPG
```

All files are in the root. There are no subdirectories for source, assets, or configuration.

## Technologies

- **Frontend**: Vanilla HTML5 / CSS3 / JavaScript (ES6+) — no frameworks, no build tools
- **Python**: Python 3, `anthropic` SDK (>=0.40.0)
- **AI Model**: `claude-opus-4-6` (hardcoded in both `novel.html` and `novel_team.py`)
- **Claude API features used**:
  - Streaming responses
  - Extended thinking (`thinking` parameter)
  - Direct browser access header (`anthropic-dangerous-direct-browser-access`)
  - Beta header: `interleaved-thinking-2025-05-14`

## Novel Writing Multi-Agent Architecture

Both `novel.html` and `novel_team.py` implement the same 5-agent sequential pipeline:

| Step | Agent (Japanese) | Role |
|------|-----------------|------|
| 1 | プロット作家 (Plot Writer) | Story structure, outline, plot points |
| 2 | キャラクター設計 (Character Designer) | Characters, backstories, relationships |
| 3 | 場面描写家 (Scene Descriptor) | Sensory scene descriptions |
| 4 | 対話作家 (Dialogue Writer) | Character dialogue and conversations |
| 5 | 編集者 (Editor) | Integrates all outputs into final story (3000–5000 chars) |

Each agent receives all previous agents' outputs as context (progressive accumulation pattern). The final story is produced by the Editor agent.

## Conventions

- **Language**: UI labels and code comments are in Japanese. Code identifiers are in English.
- **Styling**: Consistent dark theme throughout — background `#05060a` (gallery) / `#0d0e14` (novel), accent color `#6c7aff`
- **CSS class naming**: BEM-like (`.agent-chip`, `.agent-block`, `.agent-status`, `.agent-header`)
- **Image naming**: `art01.JPG`, `art_01.JPG`, `art_02.JPG`, `art_03.JPG` — note inconsistent prefix style (do not rename without updating HTML)
- **Image paths**: `art01.JPG` is served from root; `art_02.JPG` and `art_03.JPG` are referenced via `images/` subdirectory in index.html (subdirectory does not exist yet — those images will 404 until the directory is created or paths fixed)
- **Font stack**: `-apple-system, "Hiragino Sans", "Yu Gothic", sans-serif` for Japanese support

## Development Workflow

### Running the Python CLI

```bash
pip install -r requirements.txt
export ANTHROPIC_API_KEY=sk-ant-...
python novel_team.py "テーマをここに書く"
```

Output is streamed to stdout and saved to a `.txt` file in the current directory.

### Running the Web UI

Open `novel.html` in a browser (or serve via any static server). Enter your Anthropic API key — it is saved to `localStorage`. The key is sent directly from the browser to the Anthropic API using the `anthropic-dangerous-direct-browser-access` header.

### Adding Gallery Images

1. Place `.JPG` files in the repository root (or an `images/` subdirectory)
2. Copy the template block in `index.html:78` and update the `src` and label text

### Deploying

Push to `origin/main`. GitHub Pages serves the repository root automatically. No CI/CD is configured.

## Git Branch Conventions

Claude agent branches follow the pattern: `claude/<task-name>-<random-id>`

Example: `claude/add-claude-documentation-a9kbz`

## Key Notes for AI Assistants

- There is no test suite and no linter. Validate changes by reading the code carefully.
- The `ANTHROPIC_API_KEY` environment variable is required for `novel_team.py`. It is never committed.
- The web UI (`novel.html`) stores the API key in `localStorage` — this is intentional for mobile convenience.
- Both `novel.html` and `novel_team.py` must stay in sync if the agent logic changes (they implement the same pipeline independently).
- `index.html` has a comment at line 78 marking the copy-paste template for adding new images — preserve this comment.
- The `thinking` parameter differs between the two implementations: `novel_team.py` uses `{"type": "adaptive"}`, while `novel.html` uses `{"type": "enabled", "budget_tokens": 2000}`.

# AGENTS.md

## Cursor Cloud specific instructions

### Overview

Static GitHub Pages site with 4 HTML products and 2 Python CLI tools. No build step, no test suite, no linter. See `CLAUDE.md` for full details.

### Running the static site

Serve the repo root with any HTTP server:

```bash
python3 -m http.server 8080
```

All 4 HTML pages are accessible at `http://localhost:8080/`:
- `index.html` — Concept Art Gallery
- `novel.html` — Novel Writing Team (5 AI agents)
- `spec_team.html` — Game Spec Writer Team (5 AI agents)
- `indie_game.html` — Indie Game Gem Discovery Team (5 AI agents)

### Gotchas

- The AI-powered pages (`novel.html`, `spec_team.html`, `indie_game.html`) require a valid Anthropic API key entered in the browser UI. The key is stored in `localStorage` and sent directly to `api.anthropic.com` from the browser.
- The Python CLI scripts (`novel_team.py`, `indie_game_team.py`) require the `ANTHROPIC_API_KEY` environment variable.
- `art_02.JPG` and `art_03.JPG` are referenced via `images/` in `index.html` but the subdirectory does not exist; those images will 404. This is a known issue (see `CLAUDE.md`).
- There is no test suite, linter, or CI. Validate changes by code review and manual browser testing.
- `pip install` uses `--user` by default in this environment since system site-packages is not writable.

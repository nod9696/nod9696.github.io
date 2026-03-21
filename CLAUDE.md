# CLAUDE.md

This file provides guidance for AI assistants working on the nod9696.github.io repository.

## Project Overview

A static **Concept Art Gallery** website hosted on GitHub Pages at `https://nod9696.github.io`. The site displays concept art images in a responsive grid layout.

**Author**: Akamutsu (nod9696)
**Language**: Japanese (`lang="ja"`, commit messages and inline comments are in Japanese)

## Technology Stack

- **HTML5** — single `index.html` file, no framework
- **CSS3** — embedded in `<style>` block, no external stylesheet
- **No JavaScript**, no build system, no package manager, no dependencies
- **Hosting**: GitHub Pages (auto-deploys on push to `main`/`master`)

## Repository Structure

```
nod9696.github.io/
├── index.html       # The entire site — HTML + CSS in one file
├── art01.JPG        # Gallery image (root-level, legacy path)
├── art_01.JPG       # Gallery image
├── art_02.JPG       # Gallery image
├── art_03.JPG       # Gallery image
└── images/          # Directory for newer images (art02.JPG, art03.JPG)
```

> Note: Image paths are inconsistent — `art01.JPG` is at the root, while `art02.JPG` and `art03.JPG` are under `images/`. New images should be placed in `images/`.

## Development Workflow

No build step. To work on the site:

1. Clone the repo
2. Open `index.html` directly in a browser to preview
3. Edit `index.html` for layout/style changes
4. Add new images to the `images/` directory
5. Commit and push to `main` (auto-deploys via GitHub Pages)

## Adding New Gallery Items

The HTML includes a template comment for adding new items. Copy-paste this pattern inside the `.grid` div:

```html
<div class="item">
  <img src="images/art_NN.JPG" alt="">
  <span>Art NN</span>
</div>
```

- Place new image files in `images/`
- Filename format: `art_NN.JPG` (uppercase `.JPG` extension, consistent with existing files)
- Label format: `Art NN` (title case)

## CSS Conventions

- **Dark theme**: background `#05060a`, primary text `#eee`, muted text `#aaa`
- **Grid**: `repeat(auto-fill, minmax(160px, 1fr))` with `8px` gap — responsive, no media queries needed
- **Thumbnail aspect ratio**: fixed via `padding-top: 70%` trick with absolute-positioned image inside
- **Hover effect**: `transform: scale(1.03)` with `0.3s ease` transition
- **Border radius**: `8px` on `.item`
- **Font**: system font stack (`-apple-system, BlinkMacSystemFont, "Helvetica Neue", Arial, sans-serif`)
- All styles are inline in the `<style>` block — do not create external CSS files

## Key Constraints

- Keep it as a **single HTML file** — do not split into multiple files unless the owner explicitly requests it
- No JavaScript — avoid adding JS unless specifically asked
- No build tools or package managers — keep zero-dependency
- Preserve Japanese comments in the HTML
- Do not change the overall dark aesthetic or grid layout without being asked

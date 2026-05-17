# Markdown Linting

This repository uses [markdownlint-cli2](https://github.com/DavidAnson/markdownlint-cli2) to enforce consistent markdown formatting and comply with [GitHub Flavored Markdown](https://github.github.com/gfm/) specifications.

## Configuration

- [.markdownlint.json](.markdownlint.json) - Markdownlint rules configuration
- [.markdownlint-cli2.jsonc](.markdownlint-cli2.jsonc) - CLI-specific configuration (paths, ignores)

## Usage

### Install Dependencies

```bash
pnpm install
```

### Check for Issues

```bash
pnpm lint:md
```

### Auto-Fix Issues

```bash
pnpm lint:md:fix
```

## VS Code Integration

### Tasks

Two VS Code tasks are available (Ctrl+Shift+P → "Tasks: Run Task"):

1. **markdown: lint** - Check for markdown issues
1. **markdown: fix** - Automatically fix markdown issues

### Extension

Install the [markdownlint extension](https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint) for real-time linting in the editor.

## CI/CD Integration

Markdown linting runs automatically in CI via the `lint-markdown.yml` workflow:

- Runs on push/PR for any markdown file changes
- Blocks PRs if linting fails
- Comments on PRs with failure details

## Common Issues Auto-Fixed

- Missing blank lines before/after lists
- Documents starting with H2 instead of H1
- Code blocks without language specifiers
- Numbered lists using incremental numbers (1. 2. 3.) instead of all ones (1. 1. 1.)
- Trailing whitespace
- Multiple blank lines
- Missing blank lines around headings

## Rules

Key rules enforced:

- **MD001**: Heading levels increment by one
- **MD022**: Headings surrounded by blank lines
- **MD023**: Headings must start at beginning of line
- **MD025**: Single H1 per document
- **MD029**: Ordered list item prefix (style: "one")
- **MD032**: Lists surrounded by blank lines
- **MD040**: Fenced code blocks must have language
- **MD047**: Files end with single newline

Rule **MD013** (line length) is disabled for flexibility.

For the complete rule list, see [markdownlint rules](https://github.com/DavidAnson/markdownlint/blob/main/doc/Rules.md).

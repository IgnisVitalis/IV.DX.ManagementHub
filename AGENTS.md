# Agent Instructions (IV.ManagementHub)

## The agent is allowed to:

- Run build and test commands automatically
- Analyze the entire repository
- Execute validation scripts

## Restrictions:

- Only one shell command at a time
- Never execute commands in parallel
- Never retry the same command more than once
- Stop if execution exceeds 90 seconds

## UI (Fluent UI Blazor) rule

When implementing or adjusting UI (especially dialogs, viewers, grids, and layout) in `IV.ManagementHub.Web`:

1. **Prefer Fluent UI Blazor features first**  
   Try to achieve the desired behavior using Fluent UI components and their parameters/settings (e.g., `FluentStack`, `FluentGrid`, `FluentDivider`, `FluentCard`, `FluentDataGrid` options, typography/label settings).

2. **Use custom CSS only as a fallback**  
   Add CSS only when Fluent components cannot express the behavior (typical examples: sticky headers, nested scrolling/flex `min-height: 0`, overflow fixes, small spacing/polish).

3. **Keep styling consistent with Fluent**  
   When CSS is necessary, prefer Fluent design tokens/CSS variables (e.g., `--neutral-foreground-rest`, `--neutral-layer-1`) and scope styles narrowly (component-scoped `.razor.css` when possible).


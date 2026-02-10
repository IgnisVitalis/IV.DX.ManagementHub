# Agent Instructions (IV.ManagementHub)

## Tool usage rules

- Do NOT run commands in parallel
- Run at most ONE shell command at a time
- Never scan the entire repository
- Never auto-run formatting or analysis tools
<!-- - Ask before running any command -->

## UI (Fluent UI Blazor) rule

When implementing or adjusting UI (especially dialogs, viewers, grids, and layout) in `IV.ManagementHub.Web`:

1. **Prefer Fluent UI Blazor features first**  
   Try to achieve the desired behavior using Fluent UI components and their parameters/settings (e.g., `FluentStack`, `FluentGrid`, `FluentDivider`, `FluentCard`, `FluentDataGrid` options, typography/label settings).

2. **Use custom CSS only as a fallback**  
   Add CSS only when Fluent components cannot express the behavior (typical examples: sticky headers, nested scrolling/flex `min-height: 0`, overflow fixes, small spacing/polish).

3. **Keep styling consistent with Fluent**  
   When CSS is necessary, prefer Fluent design tokens/CSS variables (e.g., `--neutral-foreground-rest`, `--neutral-layer-1`) and scope styles narrowly (component-scoped `.razor.css` when possible).


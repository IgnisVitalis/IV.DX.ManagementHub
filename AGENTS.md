# Agent Instructions (IV.DX.ManagementHub)

## The agent is allowed to:

- Run build and test commands automatically
- Analyze the entire repository
- Execute validation scripts

## Restrictions:

- Only one shell command at a time
- Never execute commands in parallel
- Never retry the same command more than once
- Stop if execution exceeds 90 seconds

## Must Follow
- If request contains question before modyfing code discuss the question;
- Using best practices of desing patterns;
- Use existing patterns and conventions in the repo;
- Avoid breaking changes without explicit approval;
- Prefer small, focused changes;
- Do not commit unless asked;

## Code Style
- C#: follow existing formatting and naming conventions.
- Keep changes minimal and consistent with nearby code.

## UI: which project, which rules

The repository currently hosts two UIs side by side while the Angular rewrite is in
progress. Pick the rule set by the project you are touching:

| Project | UI stack | Rules |
|---|---|---|
| `IV.DX.ManagementHub.Web` | Fluent UI Blazor | the section below |
| `IV.DX.ManagementHub.WebApp` | Angular + Material | [WebApp/AGENTS.md](src/IV.DX.ManagementHub/IV.DX.ManagementHub.WebApp/AGENTS.md) |

`IV.DX.ManagementHub.Web` is kept intact as the reference implementation until the
rewrite is finished — do not port changes into it and do not delete it.

## UI (Fluent UI Blazor) rule

When implementing or adjusting UI (especially dialogs, viewers, grids, and layout) in `IV.DX.ManagementHub.Web`:

1. **Always prefer Fluent UI Blazor features first**  
   Try to achieve the desired behavior using Fluent UI components and their parameters/settings (e.g., `FluentStack`, `FluentGrid`, `FluentDivider`, `FluentCard`, `FluentDataGrid` options, typography/label settings).

2. **Fluent UI Blazor examples**
   Use examples using https://fluentui-blazor.azurewebsites.net/

2. **Use custom CSS only as a last resort**  
   Add CSS only when Fluent components cannot express the behavior (typical examples: sticky headers, nested scrolling/flex `min-height: 0`, overflow fixes, small spacing/polish).

3. **Keep styling consistent with Fluent**  
   When CSS is necessary, prefer Fluent design tokens/CSS variables (e.g., `--neutral-foreground-rest`, `--neutral-layer-1`) and scope styles narrowly (component-scoped `.razor.css` when possible).

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

## Projects

| Project | Role |
|---|---|
| `IV.DX.ManagementHub.ApiService` | the host: DX bootstrap, seeds, auth, instance routing, API controllers |
| `IV.DX.ManagementHub.Common` | shared models |
| `IV.DX.ManagementHub.WebApp` | the UI — Angular + Material |

The Blazor UI (`IV.DX.ManagementHub.Web`) was removed once the Angular app covered
its functionality; the host that lived inside it moved into `ApiService`. Its
sources remain in git history if something needs to be looked up.

## UI rule

All UI work happens in `IV.DX.ManagementHub.WebApp` and follows
[WebApp/AGENTS.md](src/IV.DX.ManagementHub/IV.DX.ManagementHub.WebApp/AGENTS.md).

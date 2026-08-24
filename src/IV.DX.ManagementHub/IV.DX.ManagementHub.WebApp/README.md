# IV.DX.ManagementHub.WebApp

The ManagementHub UI: Angular 22 + Angular Material. It replaced the Blazor app
(`IV.DX.ManagementHub.Web`), which has been removed — its sources are still in git
history, and the API host that used to live inside it moved to
`IV.DX.ManagementHub.ApiService`.

## Stack

|                 |                                                                    |
| --------------- | ------------------------------------------------------------------ |
| Angular         | 22 (standalone, **zoneless**, signals)                             |
| UI              | Angular Material 22, Material 3 theme (`azure`/`blue`)             |
| Styles          | SCSS, the `--mat-sys-*` system variables                           |
| Tests           | Vitest (`ng test`)                                                 |
| File naming     | Angular 2025 style guide (`shell.ts`, not `shell.component.ts`)    |
| Selector prefix | `mh-`                                                              |

## Commands

```bash
npm start        # ng serve on http://localhost:4200, /api proxied to the backend
npm run build    # production build into dist/management-hub
npm test         # unit tests (vitest)
```

To run the API and the UI together, from the repository root:

```bash
pwsh scripts/run.ps1                  # build .NET -> run the API host -> run Angular
pwsh scripts/run.ps1 -NoBuild         # skip the .NET build
pwsh scripts/run.ps1 -SkipAngular     # API only
pwsh scripts/run.ps1 -SkipWeb         # Angular only (the API is already running)
```

The initial-bundle budget in `angular.json` is raised to 700 kB: Material dialogs
pull in the CDK overlay, and the bundler hoists part of that shared infrastructure
into the eager chunk. The dialog code itself stays in the feature's lazy chunk.

### Instances and API addresses

The hub works against several DX instances. The instance key is part of the URL
rather than a header: HTTP caches key on the address, so a header would let one
instance's response be served for another wherever `Vary` is not set.

| What                                     | Where it goes                    |
| ---------------------------------------- | -------------------------------- |
| data of the selected instance            | `/api/i/{instanceKey}/...`       |
| the instance list (the hub's own data)   | `/api/management/MHInstanceUnit` |

Screens described by the hub's own metadata (the Instances cards) open in the hub's
instance — `hubInstanceKey` in `environment`. A remote instance simply does not
have that metadata and answers 404.

`npm start` uses [proxy.conf.mjs](proxy.conf.mjs): every `/api/**` request goes to
`https://localhost:7097`, the `https` profile of `IV.DX.ManagementHub.ApiService`,
which hosts the Web API. `secure: false` is there because the ASP.NET dev
certificate is not in the OpenSSL trust store on Linux.

The user login is not ported yet, so the same proxy fetches a service token from
`/api/service-auth/token` and puts it into the `Authorization` header. The key
lives in the dev-server config and **never reaches the browser**; the file does not
exist in a production build, so there will be nothing to strip from the client.

## Structure (feature-based)

```
src/
├── environments/            # environment.ts + environment.development.ts (fileReplacements)
└── app/
    ├── app.ts               # root component: just <router-outlet />
    ├── app.config.ts        # application providers (router, http, zoneless)
    ├── app.routes.ts        # root routes, mounting features inside the Shell
    │
    ├── core/                # singleton infrastructure, imported once
    │   ├── api/             # DX response contracts, errors, resource helpers
    │   │   ├── models/      #   PascalCase — as it comes off the wire
    │   │   ├── describe-error.ts
    │   │   ├── resource.ts  #   safe resource reads and error text
    │   │   └── component-view.resources.ts  # the "definition -> data" chain
    │   ├── config/          # AppConfig + APP_CONFIG (values from environment)
    │   ├── instances/       # instance list, current key from the route, guards
    │   ├── layout/          # the application shell
    │   │   ├── instance-switcher/ # instance picker in the toolbar
    │   │   ├── shell/       #   toolbar + sidenav + <router-outlet />
    │   │   └── nav-menu/    #   side navigation
    │   ├── navigation/      # navigation from metadata: loading, tree, links
    │   └── units/           # DX type structure, record, value formatting
    │
    ├── features/            # one folder per functional area
    │   ├── card-view/       # records as cards (/cards/:componentId)
    │   ├── dashboard/
    │   └── dataset-view/    # metadata-driven table (/view/:componentId)
    │       ├── dataset-view.routes.ts
    │       ├── dataset-view.mapper.ts       # DX responses -> table models
    │       ├── sort-rows.ts                 # sorting on the raw values
    │       ├── models/
    │       ├── services/                    # loads the definition and the rows
    │       ├── components/dataset-table/    # MatTable + the actions column
    │       ├── components/unit-preview/     # preview of the selected row + actions
    │       └── pages/dataset-view-page/     # a page is the "smart" component
    │
    └── shared/              # reusable, free of application state
        ├── ui/              # pure presentation
        │   ├── confirm-dialog/ # confirmation of irreversible actions
        │   ├── notice/         # a message in place of content (error, empty)
        │   ├── page-header/
        │   ├── picklist-field/ # picking an object out of a long searchable list
        │   └── split-handle/   # draggable divider between panes
        └── units/           # UI over DX metadata, shared by features
            ├── unit-actions/     # edit / export / delete one record
            ├── unit-edit-dialog/ # editing the main element and collections
            └── unit-field/       # one control per column type
```

### Rules

1. **`core/`** — whatever exists in a single copy: configuration, authentication,
   HTTP interceptors, the shell and the navigation. Features depend on `core/`,
   never the other way round.
2. **`features/<name>/`** — a self-contained piece of functionality with its own
   `<name>.routes.ts`. Features **do not import each other**; anything common moves
   to `shared/`. Inside a feature: `pages/` (smart, routed), `components/`
   (presentational), `services/`, `models/`.
3. **`shared/`** — reusable code that knows nothing about features. `shared/ui/` is
   pure presentation; `shared/units/` is UI over DX metadata and may inject `core/`
   services.
4. Every feature is loaded **lazily** through `loadChildren` — visible in the build
   as separate chunks.
5. Components are `standalone` (the default), use
   `ChangeDetectionStrategy.OnPush`, and `input()` / `signal()` instead of
   decorators and mutable fields.

### Path aliases

Configured in [tsconfig.json](tsconfig.json) so `../../../..` never appears:

| Alias         | Path                 |
| ------------- | -------------------- |
| `@core/*`     | `src/app/core/*`     |
| `@features/*` | `src/app/features/*` |
| `@shared/*`   | `src/app/shared/*`   |
| `@env/*`      | `src/environments/*` |

## Status

Ported:

- navigation built from Web API metadata (`core/navigation/`);
- dataset view: a metadata-driven table — columns from `QueryDefinition`, rows from
  the query, sorting (`features/dataset-view/`). Clicking a row opens the preview of
  that record; the checkboxes build a selection. The two are independent: a row
  click never touches the selection, and a checkbox never opens the preview. With
  several records selected, the preview is replaced by a panel with bulk actions and
  the list of what is selected;
- record preview: the type structure from `unit-structure/{TypeName}` plus the
  record, laid out in sections (main element, required, optional). Values are
  formatted by column type: enums and relations show their label, secrets are masked
  (`core/units/`). Collections inside an element are rendered as a table, and the
  panel width is set by dragging the divider;
- actions: create (POST, a toolbar button), edit of the main element and of the
  collections (a dialog, saved with PUT), delete with confirmation, export to `.dx`.
  They are available in a table row, in the preview header and above the selection —
  all through one `unit-actions` component. Editing needs exactly one record; delete
  and export work with any number (exporting several goes through `by-ids`).
  Collection rows are added, edited in a nested form and removed. Buttons appear
  according to the `isCreatable` / `isEditable` / `isDeletable` / `isExportable`
  flags of the view definition;
- card view: records of one type as cards, with actions and creation. The
  "Instances" menu entry links to it statically — no navigation metadata points at
  that screen, in Angular or in Blazor;
- **working with several DX instances**: the instance key lives in the route
  (`/app/:instanceKey/...`), requests go to `/api/i/{key}/...`, and the toolbar has a
  switcher. Switching is a navigation, so data refetches on its own and one
  instance's cache cannot leak into another.

Still to port from the Blazor version:

- editing single nested elements — the main element and the collections are covered,
  single elements are not;
- the card click action (`DXPClickAction`): in Blazor it switched the active
  instance through a DX action, which cannot work over HTTP because its effect is
  browser navigation;
- downloading Blob columns (a placeholder `file` is shown instead);
- user authentication — the dev proxy supplies the token for now.

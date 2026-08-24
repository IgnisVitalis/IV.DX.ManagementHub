# Agent Instructions (IV.DX.ManagementHub.WebApp)

Rules for the ManagementHub UI: Angular + Material. This is the only UI in the
repository — the Blazor app it replaced has been removed.

## Scope

- The application is driven by metadata from the Web API: navigation, tables, cards
  and forms are built from descriptions rather than hardcoded per entity. Whenever
  something can be derived from metadata, derive it.

## 1. Structure — feature based

```
src/app/
├── core/       # singletons: config, auth, http interceptors, shell, navigation
├── features/   # one folder per functional area
└── shared/     # reusable: ui/ (presentation) and units/ (UI over metadata)
```

1. **The dependency direction is strict:** `features/` → `core/` and `shared/`.
   There are no reverse dependencies: `core/` and `shared/` know nothing about
   features.
2. **Features do not import each other.** When something is needed by two of them,
   lift it into `shared/` or into `core/` (state and infrastructure). A direct
   import from a sibling feature is a reason to stop and discuss.
3. **`shared/` is split by how much it knows**, and neither half depends on
   `features/`:
   - `shared/ui/` — pure presentation: components, pipes and directives that inject
     no application state;
   - `shared/units/` — reusable UI over DX metadata (a field, the edit dialog, the
     actions on a record). It may inject services from `core/`: otherwise every
     feature would reinvent the same thing.

   A component moves into `shared/` when a second feature starts using it, not
   before.
4. Inside a feature: `pages/` (smart, routed), `components/` (presentational),
   `services/`, `models/`. Do not create empty folders up front.
5. Every feature has its own `<name>.routes.ts`, mounted **lazily** through
   `loadChildren`. The check: `ng build` must show the feature as a separate lazy
   chunk.
6. Imports across layers go through the `@core/*`, `@features/*`, `@shared/*` and
   `@env/*` aliases, never `../../../..`. Inside one folder a relative path is fine.
7. Files are named after the Angular 2025 style guide: `shell.ts`, not
   `shell.component.ts`. The selector prefix is `mh-`.

## 2. UI — minimal and functional

1. **Angular Material features come first.** Look for the behaviour in the
   components and their options (`MatTable` with `matSort`/`matPaginator`,
   `MatSidenav`, `MatFormField`, `appearance`, `density`) before writing your own.
   Examples and API: https://material.angular.dev/
2. **Custom CSS is the last resort.** It is justified where the components cannot
   express the behaviour: sticky headers, nested scrolling (`min-height: 0` in a
   flex box), overflow, small spacing fixes.
3. **Theme system tokens only** — `var(--mat-sys-surface)`, `--mat-sys-on-surface`,
   `--mat-sys-outline-variant`, `font: var(--mat-sys-body-medium)`. Hardcoded
   colours, font sizes and shadows are not allowed: theming and dark mode have to
   keep working on their own. Styles live in the `.scss` next to the component (view
   encapsulation), not in the global file.
4. **Minimalism that means something:**
   - density and whitespace instead of borders and fills; separate with space, not
     with lines;
   - one primary action per screen, the rest are secondary buttons or a menu;
   - an icon, a colour or a border is added only when it carries meaning (status,
     type, error), never for decoration;
   - dense data goes into a table, not into cards; cards are for records with visual
     or heterogeneous content;
   - no animation beyond Material's defaults.
5. **Function over decoration.** Every screen must handle three states: loading,
   empty and error. The empty state explains what to do next.
6. **Accessibility:** icon buttons carry an `aria-label`, fields carry a
   `mat-label`, errors go through `mat-error`. Dialogs are opened with `MatDialog`
   (it does the focus trapping); do not hand-roll modals.
7. Dense screens and tables adapt by scrolling horizontally **inside their own
   container** — the page itself must never scroll sideways.
8. The interface language is English. Keep user-facing text, `aria-label`s and
   route titles in English.

## 3. Code

1. Components are **standalone**, without NgModule; dependencies are listed in
   `imports`.
2. `changeDetection: ChangeDetectionStrategy.OnPush` on every component.
3. The application is **zoneless**. State lives in signals: `signal()`,
   `computed()`, `linkedSignal()`, `resource()`. No `setTimeout` to "let Angular
   repaint" and no `ChangeDetectorRef.detectChanges()`.
4. Inputs and outputs are the `input()`, `input.required()`, `model()` and
   `output()` functions, not the `@Input` / `@Output` decorators.
5. Dependencies come from `inject()`, not from constructor parameters.
6. Templates use the current control flow: `@if`, `@for` (with `track`), `@switch`,
   `@let`. `*ngIf` and `*ngFor` are not used.
7. **`resource.value()` throws in the error state.** For `httpResource` / `resource`
   the `value()` signal throws a `ResourceValueError` while the resource has failed,
   and `defaultValue` does not save you. Reading such a signal from a template, or
   from the URL computation of a dependent resource, takes the whole render down:
   the user sees an endless spinner instead of the error. Read it only through the
   shared helper:

   ```ts
   readonly definition = resourceValue(this.definitionResource, null);
   ```

   `hasValue()`, which the helper uses, is the only check that never throws.
   See `core/api/resource.ts`.
8. **Resources with parameters are a factory, not a class.** When a resource needs
   signals owned by a component (its inputs), a DI-created service cannot receive
   them. Write a factory function in `core/` and call it from a field initializer of
   the component (an injection context) — HTTP stays out of the component, and the
   factory is testable through `TestBed.runInInjectionContext`. Example:
   `core/units/unit-record.resources.ts`. For parameters that come from the route,
   keep an ordinary service that reads `ActivatedRoute` and is provided by the page
   component; `core/api/component-view.resources.ts` does exactly that.
9. HTTP lives in services; a component never builds a URL and knows no endpoints.
   The base address comes from `InstancesService.apiBase()`, so every request is
   scoped to the instance in the URL.
10. API response models are typed in `models/`. Do not use `any`; for an unknown
    shape use `unknown` and narrow it explicitly.
11. Do not hold subscriptions by hand: use the `async` pipe, `toSignal()`,
    `resource()` or `takeUntilDestroyed()`.
12. Formatting is the repository's Prettier. Before handing work over, `npm run
    build` and `npm test` must pass.

## The DX API contract (verified against a live server)

Worth knowing before building anything on top. The application addresses these
through the instance prefix (`/api/i/{instanceKey}/...`); the shapes below are what
DX itself returns.

- a record: `GET .../{TypeName}/{id}` → `{Meta, Data:{Items:[record]}}`, where the
  main element's fields sit directly on the record and nested ones live under
  `DXElements`;
- saving: `PUT` with the same body → **204**. The safest approach is to patch what
  `GET` returned: `Meta` and the untouched elements then travel back intact;
- creating: `POST .../{TypeName}` → **201** and `{id}`. The server generates the
  record's `Id`, `Meta.Op` is optional, and collections may be sent along;
- collection rows: **the client generates the `Id`** — a row sent without one is
  stored with an empty GUID. The foreign keys (`DXUnitId`, `{Type}Id`) are filled in
  by the server; do not invent them;
- deleting a collection row means leaving it out of `Items`;
- secrets (`HashedString`, `EncryptedString`) come back empty from the API: never
  send them back empty, or you will wipe the stored value;
- `DXColumnDefinitionElement` rows named `Id`, `TimeStamp` or `DXUnitId` are DX's
  own bookkeeping columns. They are hidden from the view (the Blazor UI did the
  same) but **kept in the editor's state**: a row missing from the payload is
  deleted.

### Known backend limitations

`PUT` for `DXElementDefinitionUnit` answers 500 — reproducible with plain curl by
sending back exactly what `GET` returned, so it is not a UI problem. Saving that
type triggers a schema migration and DX generates invalid SQL (`ALTER COLUMN ...
SET NULL` instead of `DROP NOT NULL`); on another record the same `PUT` complains
that the `update method for DXRelationDefinitionUnit isn't implemented yet`.
Creating (`POST`) works. Show the server's error as it is — see
`core/api/describe-error.ts`.

## Commands

```bash
npm start        # ng serve on 4200, /api proxied to https://localhost:7097
npm run build    # production build
npm test         # vitest

pwsh ../../../scripts/run.ps1   # from the repository root: the API host + Angular
```

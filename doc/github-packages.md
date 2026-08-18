# GitHub Packages: consuming the IV.DX family

This repository is an application, not a package producer. It publishes nothing; it
consumes `IV.DX`, `IV.DX.PostgreSQL`, `IV.DX.Presentation`, `IV.DX.WebApi`,
`IV.DX.WebApi.Auth` and `IV.DX.WebApi.Management`, all of which live on GitHub
Packages under `IgnisVitalis` — none of them are on nuget.org.

## 1. Create the personal access token

Use a **classic** token — the NuGet registry does not accept fine-grained tokens.

1. https://github.com/settings/tokens → *Generate new token* → *Generate new token (classic)*
2. Scope: `read:packages` is all this repository needs (`repo` as well if the source
   repositories are private).
3. Copy the token once; GitHub will not show it again.

## 2. Store it for local builds

```bash
./scripts/setup-github-feed.ps1 -Token ghp_xxx
```

The script checks the token against the feed before storing anything, then writes the
source and credentials to `~/.nuget/NuGet/NuGet.Config` — the user-level config, never
into this repository. Keep that file private:

```bash
chmod 600 ~/.nuget/NuGet/NuGet.Config
```

`GITHUB_PACKAGES_TOKEN` in the environment works too and takes precedence.

## 3. Store it for CI

Repository → *Settings* → *Secrets and variables* → *Actions* → *New repository secret*:

- Name: `GH_PACKAGES_TOKEN`
- Value: the same classic token

The IV.DX packages are published from other repositories, so the built-in `GITHUB_TOKEN`
can read them only if each package grants this repository access (package page →
*Package settings* → *Manage Actions access*). [build-and-test.yml](../.github/workflows/build-and-test.yml)
falls back to `GITHUB_TOKEN` when the secret is absent, so either route works.

## 4. Keeping references current

```bash
./scripts/build.ps1
```

Before building, it rewrites the `PackageReference` versions to the newest available
anywhere — GitHub Packages and the local feed (`~/.nuget/local-feed`) are both
consulted and the higher version wins, so a package you have just packed locally is
picked up ahead of the published one. `IV.DX` and `IV.DX.PostgreSQL` are moved
together, since a provider is only valid against the exact core version it was built
with. It never downgrades a reference.

Resolving to a local-only version prints a warning: it is the right choice while you
are working, but CI can restore only what has been published, so that version has to
reach GitHub Packages before a tag will build.

Pin any family explicitly when you need to: `-DxVersion`, `-DxPresentationVersion`,
`-DxWebApiVersion`, `-DxWebApiAuthVersion`, `-DxWebApiManagementVersion`, or skip the
whole step with `-SkipDxSync`.

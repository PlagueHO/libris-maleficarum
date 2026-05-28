---
title: Pull Request Template
description: Pull request checklist and summary guidance
---

## Summary

Describe what changed and why.

## Change Type

Select all that apply.

* [ ] Feature
* [ ] Bug fix
* [ ] Refactor
* [ ] Performance improvement
* [ ] Security hardening
* [ ] Dependency update
* [ ] Tooling or CI/CD
* [ ] Infrastructure (Bicep, Aspire, Azure)
* [ ] Documentation
* [ ] Tests only
* [ ] Chore or maintenance

## Scope

Select the areas affected by this change.

* [ ] Frontend (`libris-maleficarum-app`)
* [ ] Backend services (`libris-maleficarum-service/src`)
* [ ] Service orchestration (`libris-maleficarum-service/src/Orchestration`)
* [ ] Service tools (`libris-maleficarum-service/src/Tools`)
* [ ] Infrastructure (`infra`)
* [ ] Documentation (`docs`)
* [ ] Smoke tests (`tests/smoke`)
* [ ] GitHub workflows (`.github/workflows`)

## Impact

* [ ] Breaking change
* [ ] Requires config or environment variable updates
* [ ] Requires migration or deployment sequencing
* [ ] No operational impact expected

If there is operational impact, describe rollout, fallback, or mitigation.

## Validation

* [ ] Build succeeds for affected projects
* [ ] Tests added or updated for changed behavior
* [ ] Existing tests pass for affected areas
* [ ] Lint and formatting checks pass
* [ ] Security-sensitive flows validated (auth, access code, logging)
* [ ] Docs updated when API, UX, or operations changed
* [ ] Template placeholders handled correctly

## Release Notes

What should appear in release notes?

* [ ] No release note needed
* [ ] Patch fix
* [ ] New capability
* [ ] Breaking or migration note

Release note summary:

<!-- Add 1-3 bullet points for changelog or release notes. -->

## Related Issues

List related issue numbers.

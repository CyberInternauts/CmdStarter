# 0030. Version Number of the Rewrite

Status: Accepted
Date: 2026-11-04

## Context
The branch rewrites CmdStarter on System.CommandLine 2.0 stable and .NET 10, and the ADRs written so far call it "2.0" ("CmdStarter 2.0" in [ADR 0005](0005-breaking-changes-and-adapter.md)). That name was a working name. The versions of `cints.CmdStarter` published on nuget.org are `0.9.0-beta4.22272.1`, `0.9.0.1-beta4.22272.1`, `0.9.0.2-beta4.22272.1` and `0.9.0.3-beta4.22272.1`; `cints.CmdStarter.Hosting` was never published; `master` carries `0.9.0.3-beta4.22272.1`. There has never been a 1.x release, so a "2.0" would skip a major version and a "migration from 1.x" would describe a version that no user has.

## Decision
The rewrite ships as version 1.0.0, in `ProjectsCommon.props`. From there the numbers follow semantic versioning, and the next major version is 2.0. The version has no pre-release suffix. The publication itself stays a separate decision of the owner of the project.

In the ADRs written before this one, "2.0" names this release, now 1.0, and "3.0" the next major version, now 2.0. Those ADRs are not rewritten, and neither are the statements of a dated decision such as "Version 2.0 does not support…" in [ADR 0025](0025-trimming-and-native-aot.md) and [ADR 0029](0029-trimming-and-native-aot-second-evaluation.md).

## Consequences
- The package metadata (`Version`, the release notes of the two projects) says 1.0.0, and the pages that said "2.0" say "1.0".
- The migration guide starts from what the users have, the 0.9 betas: it is `docs/migration/0.9-to-1.0.md`, and its content is unchanged except for the numbers.
- `AnalyzerReleases.Shipped.md` stays empty until the publication.
- The name of the branch and the Jira project are not changed.
- The mentions of System.CommandLine 2.0 are unchanged: that is the version of the dependency.

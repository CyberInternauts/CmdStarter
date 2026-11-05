# 0001. Target .NET 10 Only

Status: Accepted
Date: 2026-11-03

## Context
CmdStarter targets .NET 10 in ProjectsCommon.props, with SDK 10.0.100 pinned in global.json and latestFeature roll-forward.\n.NET 7 is out of support, and maintaining older targets adds cost without a current need.

## Decision
Target net10.0 only. Do not multi-target unless a concrete need arises.

## Consequences
This keeps the supported runtime and build SDK aligned. Consumers must use .NET 10.

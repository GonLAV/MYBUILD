---
topic: troubleshooting:all-tests-skip-vpn
summary: Every test skips, even ones with no env gate — corporate VPN is down; check DNS before debugging RunIn.
status: ready
---

# Every test skips and it looks like an environment gate

**Symptom:** `Skipped <TestName>` with no output, for a test you expect to run. Forcing the environment
(`ASPNETCORE_ENVIRONMENT`, the `Environment` run parameter, a pinned `RunSettingsFilePath`) changes
nothing.

**Distinguishing check — run a test with a permissive gate.** A `[RunIn(includeStaging: true)]` test
runs in *every* environment, so if that skips too, the gate is not the problem:

```bash
dotnet test --filter "FullyQualifiedName~UNIFY_PL_Renters_FQ_Payment_E2E"
```

**Cause:** with the corporate VPN down, config/tenant resolution cannot reach its sources and the
harness marks tests Inconclusive — indistinguishable from `[RunIn]` skipping them. Confirm with DNS
rather than by reading gate code:

```bash
nslookup sts-qa-unify.boltqa.com
```

`*.boltqa.com` failing to resolve while other hosts resolve means VPN, not test configuration.

**Related setup trap:** `nexus-agent secrets sync` fails with
`Unable to find the "default" profile` because the AWS profile is named `nexus`, not `default`:

```bash
aws sso login --profile nexus
AWS_PROFILE=nexus nexus-agent secrets sync
```

**Cross-references:** [../../framework/test-class.md](../../framework/test-class.md).

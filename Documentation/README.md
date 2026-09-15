# Bolt Automation Documentation

Index of the living docs in this folder. The AI-agent knowledge base lives separately under [agent-knowledge/](./agent-knowledge/INDEX.md).

## Architecture & setup

- [Service Registration](./ServiceRegistration.md) — layered DI extension-method model
- [Configuration](./Configuration.md) — configuration sources and priority order
- [Options Classes](./OptionsClasses.md) — where options classes live
- [Adding New Projects or Services](./AddingNewProjectOrService.md)
- [Add New Project Front End](./AddNewProjectFrontEnd.md)
- [Solution Map](./SOLUTION_MAP.md) — solution-wide project/class map
- [TestDataProvider Architecture](./TestDataProvider_Architecture_Documentation.md)

## Test execution & CI

- [CI/CD Test Execution Guide](./CI-CD-TestExecution-Guide.md) — distributed execution (orchestrator + WorkerAgent)
- [Docker Deployment Guide](./Docker-Deployment-Guide.md) — run tests in Docker

## Reporting & logging

- [MongoDB Logging Structure](./MongoDB-Logging-Structure.md) — document schemas
- [MongoReporting Implementation](./MongoReporting-Implementation.md) — architecture and wiring
- [AI Agent Logging Instructions](./AI-Agent-Logging-Instructions.md) — rules for adding logging
- [API Identifier Mapping](./API-Identifier-Mapping.md)

## API clients

- [ApiClients overview](./ApiClients/ApiClients.md)
- [SSO API](./ApiClients/SSO/SsoApi.md)
- [STS API](./ApiClients/Sts/StsApi.md)

## AI agent extension

- [AI Agent Extension Architecture](./AI-Agent-Extension-Architecture.md) — CLI, KB, skills
- [Agent knowledge base index](./agent-knowledge/INDEX.md)

## QA assist

- [QA Assist Roadmap](./QA-Assist-Roadmap.md)
- [QA Onboarding Checklist](./QA-Onboarding-Checklist.md)

## Specs (historical / partner-specific)

- [PGR Moratorium Buckets Spec](./PGR-Moratorium-Buckets-Spec.md)
- [Secrets Tier-2 User/Twilio Migration Spec](./Secrets-Tier2-UserTwilio-Migration-Spec.md) — implemented; kept as design record
- [Playwright FrontEnds Improvement Analysis](./Playwright-FrontEnds-Improvement-Analysis.md) — open improvement backlog
- [Interview Runtime Baseline](./Interview-Runtime-Baseline.md) — per-test/per-page timing analysis and optimisation proposal for the Interview suite

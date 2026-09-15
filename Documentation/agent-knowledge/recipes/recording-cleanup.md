---
topic: recipe:recording-cleanup
summary: Distill a Playwright codegen recording of exploratory QA testing into a clean replayable scenario — noise heuristics (toggle pairs, refills, dead ends), registry mapping, and the mandatory intent interview.
status: ready
---

# Recording cleanup — exploratory capture → replayable scenario

> **When to read:** A QA has recorded an exploratory session with `browser record`
> (Playwright codegen) and you need to turn the raw `.cs` recording into a clean
> `.qa-scenarios/*.yml` scenario. Companion: `recipe:qa-scenario-format`.

## What a recording is (and isn't)

`browser record` streams the QA's manual actions through Playwright codegen into a C#
file: a linear list of `page.GetByRole(...).ClickAsync()` / `.FillAsync("…")` calls with
auto-picked locators. It is a faithful log of *everything they did* — including
exploration noise — with **no intent**. Your job is to recover the intent and discard
the rest. The QA's needs define what is noise; heuristics only propose.

Live-run facts to rely on (verified):

- **The output file is the ground truth.** The Inspector's on-screen log can look empty
  while actions stream into `--output` just fine — never conclude "nothing recorded"
  before reading the file after the recorder exits.
- **The recorder re-enters where the SPA decides.** Storage state carries auth + the
  quote, but not in-memory position — a session sitting on `/rates` reopens at
  `/overview` in the recorder. Segment the recording from that re-entry point.
- **Codegen always launches its own browser** — it cannot attach to the session's. Open
  the session headless when the plan is recording, so the recorder is the only visible
  window. With a headed session, two look-alike windows are on screen: the recorder is
  the one paired with the Inspector and the floating record toolbar. If the QA closes
  the *session* window by mistake, its page becomes `about:blank` and a re-record from
  that session is refused — reopen a fresh session instead.

## Step 1 — Parse into an action list (use the parser, not your eyes)

Run `nexus-agent browser parse-recording --file <recording.cs>` — it converts every
statement into a structured action: `{action, target{kind,value,name,nth,first,filter},
value, suggested_selector, raw}`. Targets that already have a public selector-engine
equivalent (plain css, `role=…[name=…]`, `text=…`) carry `suggested_selector`, ready for
a `raw:` step; label/placeholder/testid and Filter/Nth-modified targets don't — those
need a registry mapping or a human-chosen selector. Unrecognized statements come back as
`action: unparsed` with the raw line — review them, never assume they were noise.
A `.meta.json` sidecar next to the recording records session/tenant/env/start-URL
provenance. `goto` actions are the navigation boundaries — segment by them.

## Step 1b — Verification markers (`##…##`)

Recordings capture actions, never the looking — unless the QA marks the looking. The
convention: **mid-recording, type `##any text##` into any text input** (e.g.
`##check: premium under $200##`), then keep going. The parser surfaces it as an
`action: marker` with the text extracted; distillation converts each marker into a
`pause:` step at exactly that position (and drops the marker fill itself — also restore
the field if the QA overwrote a real value to type the marker). Teach QAs the trick
when starting a recording; it beats reconstructing checkpoints from memory in the
interview.

Caveat: the detection is shape-based — a **real** value that happens to be fully wrapped
in `##…##` (say a promo code literally entered as `##SAVE10##`) would be misread as a
marker and its fill dropped. Rare, but when a recording contains a marker that doesn't
read like a verification note, raise it in the intent interview instead of assuming.

## Step 2 — Apply the noise heuristics (propose, don't decide)

| Pattern | Signal | Default proposal |
|---|---|---|
| **Toggle pair** — check X … uncheck X (or two clicks on the same toggle/radio) with no dependent action between | QA flipped something to see what it reveals | Drop **both**, unless the final state differs from the initial state → keep only the net change |
| **Refill** — multiple `fill`s on the same field | QA corrected a typo or tried values | Keep only the **last** fill |
| **Dead end** — navigate/click into a section, interact with nothing (or only reads), come back | Peeked, didn't act | Drop the whole excursion |
| **Open-close** — open a dropdown/accordion/dialog then close it without selecting | Looked at the options | Drop both actions |
| **Repeated identical click** with no visible effect between (double-submit, impatient clicking) | Retry noise | Keep one |
| **Scroll/hover/focus-only** artifacts | Never meaningful in codegen output | Drop |
| **Value experiments** — same field filled, submitted, error, refilled | The *error* may be the point! | Ask: was provoking the validation part of the test? |

Order matters when fields reveal conditionally (`troubleshooting:conditional-reveal`):
never reorder a trigger field after its dependent. When in doubt, preserve recorded order.

## Step 3 — The intent interview (mandatory)

Before writing the scenario, present the QA a **plain-language reconstruction** and ask,
via `AskUserQuestion`:

1. "Here's what I think you were doing: … — is that the scenario you want to keep?"
2. Every heuristic drop that isn't trivially safe (especially toggle pairs and value
   experiments): "You switched X on and off — exploration, or should the scenario flip it?"
3. "Where should replays *stop and let you look*?" → these become `pause` steps —
   a recording never captures verification moments, only actions, so pauses must be
   added from the interview.
4. Which values are fixed vs. parameterizable ("always this VIN, or ask each run?").

## Step 4 — Map to the framework, then write the scenario

For each surviving action, in order of preference:

1. **Page-level:** a click that advances the flow → a `continue` step (with `expect`)
   rather than a raw click on the Next button.
2. **Registry field:** `code field-lookup <guess>` (try the label text, camel-cased) —
   if the field exists, use a `fill:` step with the field name. Registry names survive
   UI redesigns; raw selectors don't.
3. **Raw step (new functionality):** no registry entry — keep it as a `raw:` step with
   the recorded selector, preferring `role=`/`text=` engines over brittle css chains.
   Never XPath. Flag these in the scenario as candidates for future registry adoption:
   when the automation team registers the fields, upgrade the steps.
4. Segment boundaries → verify the session opener (`quote_start`/`open`/`navigate`)
   reproduces the recording's starting point; the recording itself starts *after* auth
   and quote creation, which the opener re-establishes at replay.

Save per `recipe:qa-scenario-format`; keep the recording file next to it
(`.qa-scenarios/recordings/`) as provenance until the QA confirms a clean replay.

## Step 5 — Validate by replaying

Offer to replay the cleaned scenario immediately, headed, with the QA watching — the
first replay is the review. A raw step that fails on replay usually means its selector
was auto-picked too tightly (codegen loves `nth()` chains) — loosen to a role/text
selector and retry. Raw steps that keep failing across replays are a signal the new
functionality needs FieldRegistry entries — hand that to the automation team.

## Anti-goals

- Do **not** keep the recording as the scenario ("it replays, ship it") — un-cleaned
  recordings rot instantly and replay noise confuses the next reader.
- Do **not** silently drop ambiguous actions — every non-trivial drop goes through the
  interview (their needs define noise, not the heuristics).
- Do **not** convert recordings into NUnit tests — that path is `nexus-test-author`,
  with the scenario (not the recording) as its input.

---
name: grill-me
description: Interview the user relentlessly about a plan or design until reaching a shared understanding, resolving each branch of the decision tree. Use when the user wants to stress-test a plan, get grilled on their design, or mentions "grill me."
---

Interview the user relentlessly about every aspect of the plan/design until shared understanding is reached. Walk down the decision tree, resolving dependencies between decisions one-by-one.

## Before grilling - collect inputs

**Mandatory pre-flight check.** Before the first decision question, make sure you know the baseline context. Jumping into "Plan A or B" without inputs is like planning in the sand.

Checklist of inputs you need to know or quickly clarify:

- **Target platform.** Desktop / mobile (Android/iOS) / console / web. Significantly changes optimization priorities (mobile per-camera tax, draw call cost, VRAM budget).
- **Scale.** What orders of magnitude are in the main use case? How many objects at a time? What is the frequency? This isn't "scope in the plan," these are **real numbers** at runtime.
- **Third-party packages in the stack.** If the task uses specific packages (UniText instead of TMP, R3 instead of UniRx, BatchRendererGroup, custom shader systems), make sure you understand their features or immediately start researching them.
- **Previous attempts at a solution.** If a feature already exists and is being reworked, what **specifically** was tried before and why it didn't work. Without this, you risk proposing something that has already been rejected and losing rounds on re-negotiation.
- **Hard constraints.** What can't be changed — the public API, config format, deadlines, performance baselines. This ensures the plan doesn't conflict with hidden constraints.

How to collect:
- If the input is already visible from the context of the conversation or recently read code, record it with an explicit message ("Target: Android, 30+ instances, UniText in the stack") and proceed to grilling.
- If it's not visible, ask 2-4 input questions in a single AskUserQuestion (multiple questions per call are supported). This is an exception to the "one question at a time" rule — inputs are not decisions, they can be collected in batches.
- If the input is revealed in the middle of grilling (like "target = mobile" after 8 questions), this is a sign that the pre-flight was incomplete. **Stop grilling, admit the omission, replan**. Don't try to stretch existing answers to fit the new context.

The cost of a pre-flight is 1-2 rounds of questions. The cost of skipping a pre-flight is rewriting the plan after the skipping context is revealed. Pre-flight is always cheaper.

## Rules

1. **One question at a time.** No "and also, to top it off," or "and secondly." One question, one step.

2. **Each question — via `AskUserQuestion`.** It contains:
- 2-4 answer options, each with a short explanation in the `description`.
- **Mark the recommended answer** — it goes first in the list, and add `(Recommended)` to the `label`.
- `header` — a short chip tag (≤12 characters), such as `Scope`, `Lifecycle`, or `API`. - The user can always select "Other" and write a free-form answer.
- If there are really only two options, then okay, two. Don't stretch it artificially.

3. **Adapt the tree based on the answers.** The next question depends on the choice. If the user selected "specifically for height," further questions about general API design are out of the question. If they selected "generally," questions about hardcoded points are out of the question. **Don't ask a question that's already lost its meaning.**

4. **If the answer can be found in the code, go to the code, don't ask.** Read, state the fact, and move on to the next question. This isn't part of the grill cycle, but an introduction.

5. **Don't write long preambles.** Brief: 1-2 lines of context (if necessary), then `AskUserQuestion`.

6. **Don't answer for the user in the "Other" option.** Other is always a transition to free-form text. Don't predict what they'll write there.

## `AskUserQuestion` Format

```
question: "A specific question with a question mark at the end?"
header: "Short tag" # ≤12 characters
multiSelect: false # usually a single choice
options:
  - label: "Option A (Recommended)"
  description: "Why this: 1-2 sentences with reasoning."
  - label: "Option B"
  description: "When it's better: 1-2 sentences with a trade-off."
  - label: "Option C"
  description: "Alternative: 1-2 sentences."
```

## When to Stop

**Default: DO NOT stop early.** Keep asking questions until you've covered **everything** about the main question/mechanic/task:
- Scope (what's included, what's not included).
- API shape (field names, types, configuration).
- Lifecycle (when created, when destroyed, when phases change).
- Edge cases (interruptions, reuses, race conditions, negative values, zero timers).
- Interaction with related mechanics (other modifiers of the same stat, other sources of the same effect, view-side, network).
- Code change locations (which systems are affected, what is the order in the feature).
- Risks (what can break the existing one, how to diagnose it).

Stop only if:
- The user isBut he said "that's enough" / "make a plan" / "enough."
- All the categories listed are closed, and you don't see a single open branch in the tree.

Don't be afraid to "annoy" the user—the purpose of the skill is to draw out all the implicit assumptions from the user before the plan is written.

## Argument and Verification

- **The worst thing you can do in a grill-me is to agree.** If you see a real problem in the user's answer, you are obliged to raise it, even if you're sure they don't like objections. Agreeing "for the sake of it" = a skill failure. It's better to get +1 irritation from the user than to release a plan with a hole.
- **Argue only when you see a real problem,** not for the sake of arguing. The real problem is a specific contradiction with: research (/web-research), rules in .claude/rules/, existing code, or an internal inconsistency in the plan itself. "I think otherwise" is not an excuse. If there are no objections, silently accept and move on.
- **Check between questions.** Before each subsequent question, quickly check: "Does what the user just answered contradict what I know from the code/rules/research?" If it does, raise it with a specific citation/file, rather than "I have a vague feeling."
- **If you doubt your own recommendation, run research**, don't guess. The web-research skill is available; run it before generating options if the question is industry-specific (patterns, conventions).
- **If the user objects to the recommendation**, update the mental model; in subsequent questions, **don't offer the same thing again**.

## What NOT to do

- **Don't give a "menu without a recommendation."** If there's no recommendation, it means they haven't thought about it yet. Think, choose, and justify it.
- **Don't comment on the process** ("I'll ask Q3 now," "That's a difficult question"). Just ask.
- **Don't cram questions into a single `AskUserQuestion`.** The `questions` field accepts up to 4 questions—but this is for unrelated parameters of a single step (for example, choosing name + visibility at once). For a grill loop, ask one question at a time.
- **Don't ignore counterarguments.** If the user objects to a recommendation, update the mental model and continue without offering the same recommendation again.
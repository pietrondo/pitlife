# PitLife Code Review Checklist

Derived from `requesting-code-review` skill, tailored for this codebase.

## When to Review

- After completing any epic/refactor issue
- Before merging non-trivial changes
- When stuck or before a large refactor (baseline check)

## Review Process

```bash
BASE_SHA=$(git rev-parse HEAD~1)  # or origin/main
HEAD_SHA=$(git rev-parse HEAD)
git diff --stat $BASE_SHA..$HEAD_SHA
git diff $BASE_SHA..$HEAD_SHA
```

## Review Categories

### Architecture & Design
- [ ] Sound design decisions, no silent assumptions (AGENTS.md rule 1)
- [ ] Brainstorming artifact exists under `docs/decisions/` if epic/refactor >=2 files (rule 1b)
- [ ] No god classes created: new files <= 250 LOC, existing files trending down
- [ ] Separation of concerns maintained (Simulation/Systems/ vs Entities/ vs Generation/)
- [ ] No new tight coupling between unrelated modules
- [ ] Scalability: adding a new system/species/biome is configuration-only

### Data-Driven Configuration (AGENTS.md rule 6)
- [ ] New species, biomes, items, behaviors defined in external config (JSON/YAML), not hardcoded
- [ ] No magic numbers or strings introduced; existing ones not duplicated
- [ ] Content changes don't require recompilation
- [ ] Config loading has error handling for malformed files

### Testing
- [ ] Tests verify behavior, not implementation details
- [ ] Edge cases covered (empty state, boundary values, error paths)
- [ ] No accidental behavior change: existing tests still pass
- [ ] New logic has corresponding test coverage
- [ ] Tests use `docs/code-review-checklist.md` conventions where applicable

### Code Quality
- [ ] Clean separation of concerns
- [ ] Proper error handling (no swallowed exceptions)
- [ ] DRY: no duplicated logic blocks
- [ ] Type safety: no unnecessary casts or nullable suppression
- [ ] Follows existing codebase conventions (naming, structure, patterns)

### Requirements
- [ ] All acceptance criteria from the bd issue are met
- [ ] No scope creep: implementation matches the issue, not extra features
- [ ] Breaking changes identified and documented

### Production Readiness
- [ ] `graphify update .` run after file changes
- [ ] No obvious bugs, null reference risks, or race conditions
- [ ] Backward compatibility preserved for save/load (WorldState serialization)

## Output Format

### Strengths
[What's well done? Be specific with file:line references.]

### Issues

#### Critical (Must Fix)
[Bugs, security issues, data loss risks, broken functionality, test failures]

#### Important (Should Fix)
[Architecture problems, missing requirements, test gaps, config vs code violations]

#### Minor (Nice to Have)
[Code style, optimization opportunities, documentation improvements]

### Assessment
**Ready to merge?** Yes / With fixes / No
**Reasoning:** 1-2 sentences.

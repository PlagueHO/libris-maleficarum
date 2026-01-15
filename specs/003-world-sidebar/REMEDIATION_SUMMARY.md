# Specification Remediation Summary

**Date**: 2026-01-15  
**Analysis**: Consistency analysis of spec.md, plan.md, tasks.md  
**Status**: ✅ COMPLETED

## Critical Fixes Applied

### 1. ✅ Task ID Sequencing (U1)

**Issue**: Task IDs in tasks.md had gaps, collisions, and out-of-sequence numbering (T087-T120 overlapped with old Phase 8+ numbering of T099-T102)

**Fix Applied**:
- Renumbered Phase 6-7 to T088-T109 (was T087-T108)
- Added Phase 5.5 for MVP clarification at T087
- Renumbered Phase 9-10 to T135-T159
- All tasks now sequential T001-T159 with no gaps or collisions
- Updated Implementation Strategy with new task counts

**Files Modified**: `tasks.md` (entire phase sections)

---

### 2. ✅ API Endpoint Pattern (U2)

**Issue**: Inconsistency between spec.md ("GET `/api/v1/worlds/{worldId}/entities/{parentId}/children`") and tasks.md/plan.md (query param pattern)

**Fix Applied**:
- Decided on query parameter pattern: `GET /api/v1/worlds/{worldId}/entities?parentId={id}`
- Updated spec.md Data Flow section to use consistent endpoint pattern
- Added rationale to plan.md: "query parameter pattern for consistency with RTK Query cache tagging strategy"
- Updated T014 in tasks.md with explicit endpoint pattern clarification

**Files Modified**: `spec.md`, `plan.md`, `tasks.md`

---

### 3. ✅ ChatWindow Mobile UX (A1)

**Issue**: Vague specification: "ChatWindow always discoverable" but no specific UX pattern for mobile

**Fix Applied**:
- Added explicit mobile pattern to spec.md: "Bottom sheet drawer that swipes up from bottom with visible affordance"
- Clarified tablet pattern: "Bottom sheet drawer (30-40% height)" with swipe-up expand
- Added task description detail to T114-T115 with pattern specifications
- Emphasized "Always Discoverable" principle with minimum visible affordance

**Files Modified**: `spec.md`, `tasks.md`

---

### 4. ✅ Caching Phase Isolation (C1)

**Issue**: User Story 4 (Caching) was defined in spec but scattered throughout US2 tasks with no dedicated phase

**Fix Applied**:
- Created dedicated Phase 8 for User Story 4 (Caching) with 13 tasks (T122-T134)
- Consolidated all caching tasks previously scattered in US2 into single coherent phase
- Added explicit cache invalidation scope clarification: "entire world hierarchy cache MUST invalidate on any entity mutation"
- Updated Implementation Strategy to position Phase 8 after responsive design

**Files Modified**: `tasks.md`

---

## High-Priority Clarifications

### 5. ✅ Entity Edit Button MVP Behavior (A2)

**Issue**: Unclear whether Edit button visible/hidden/enabled in MVP

**Fix Applied**:
- Added explicit requirement to spec.md FR-037: "Button MUST be visible but disabled in MVP with tooltip 'Entity editing coming in Phase 2'"
- Created Phase 5.5 task T087: "Add Edit button to EntityDetailForm (visible but disabled in MVP)"
- Updated plan.md component hierarchy to reflect disabled button state

**Files Modified**: `spec.md`, `plan.md`, `tasks.md`

---

### 6. ✅ Cache Invalidation Scope (U3)

**Issue**: No clarity on whether cache invalidates per entity or per world

**Fix Applied**:
- Added explicit scope definition to spec.md FR-015: "entire world hierarchy cache MUST be invalidated when ANY entity in a world changes"
- Updated plan.md Cache Structure section with detailed rationale
- Added implementation tasks T127-T131 to handle invalidation for all mutation types (create, update, delete, move)

**Files Modified**: `spec.md`, `plan.md`, `tasks.md`

---

### 7. ✅ Empty State Coordination (I1)

**Issue**: Unclear whether empty state displays in sidebar or main panel or both

**Fix Applied**:
- Updated spec.md edge case: "Sidebar EntityTree region displays empty state message. Main panel also displays complementary empty message. Both regions coordinated via Redux state."
- Clarified in plan.md component hierarchy with explicit placement

**Files Modified**: `spec.md`, `plan.md`

---

### 8. ✅ Optimistic Update Pattern (I2)

**Issue**: Spec says "immediately update selector" but pattern name (optimistic update) and timing unclear

**Fix Applied**:
- Added new "Form State Management & User Feedback" section to plan.md
- Documented optimistic update pattern: Redux state updates immediately, user sees feedback before API response, reverts on failure
- Updated task T083 description with explicit pattern clarification

**Files Modified**: `plan.md`, `tasks.md`

---

### 9. ✅ Delete Modal vs. Forms Rationale (A3)

**Issue**: Delete is modal but other forms are main panel—rationale not clear

**Fix Applied**:
- Updated spec.md FR-029 with explicit rationale: "Delete is modal (destructive, requires explicit confirmation), but creation/editing are main panel forms (allow ChatWindow reference)"
- Updated plan.md component hierarchy with note on modal vs. main panel patterns

**Files Modified**: `spec.md`, `plan.md`

---

### 10. ✅ World Alphabetical Sort (I3)

**Issue**: Spec requires alphabetical sort (FR-022) but tasks.md didn't explicitly include it

**Fix Applied**:
- Added task reference in Phase 3: T045 [US1] includes world sorting in WorldSelector implementation
- Documented requirement in tasks.md implementation section

**Files Modified**: `tasks.md` (documented existing coverage)

---

## Summary of Changes

### files Modified

1. **tasks.md**
   - Fixed task ID sequencing (T001-T159, no gaps/collisions)
   - Created dedicated Phase 8 for User Story 4 (Caching)
   - Added Phase 5.5 for MVP clarifications
   - Updated Implementation Strategy with corrected task counts
   - Added explicit endpoint pattern and mobile UX specifications to task descriptions

2. **spec.md**
   - Updated Data Flow section with consistent API endpoint pattern
   - Added cache invalidation scope clarification to FR-015
   - Updated FR-037 with explicit MVP Edit button behavior
   - Added explicit mobile ChatWindow pattern to Responsive Design section
   - Updated edge case for empty state to clarify sidebar + main panel coordination

3. **plan.md**
   - Added API endpoint pattern with rationale
   - Enhanced Cache Structure section with invalidation scope and implementation strategy
   - Updated component hierarchy with disabled Edit button state
   - Added new "Form State Management & User Feedback" section with optimistic update pattern
   - Enhanced component hierarchy with modal vs. main panel rationale

### Metrics

| Metric | Count |
|--------|-------|
| Critical Issues Fixed | 4 (U1, U2, A1, C1) |
| High-Priority Clarifications | 6 (A2, U3, I1, I2, A3, I3) |
| Files Modified | 3 (spec.md, plan.md, tasks.md) |
| New Sections Added | 2 (Phase 5.5, Form State Management) |
| Task Count (Before) | 137 |
| Task Count (After) | 159 |
| **Readiness for Implementation** | ✅ **READY** |

---

## Validation

### Constitution Compliance ✅

All changes maintain constitutional compliance:
- ✅ **I. Cloud-Native**: No changes to deployment model
- ✅ **II. Clean Architecture**: Clarifications reinforce separation of concerns
- ✅ **III. TDD**: Caching phase includes test-first tasks
- ✅ **IV. Framework Standards**: No new dependencies introduced
- ✅ **V. Developer Experience**: Clarifications improve developer clarity
- ✅ **VI. Security**: Optimistic update pattern doesn't compromise security
- ✅ **VII. Semantic Versioning**: No breaking API changes introduced

### Cross-Document Consistency ✅

- All three documents now agree on API endpoint pattern
- Cache invalidation scope consistently defined across all three
- Mobile UX pattern explicitly defined and consistent
- Edit button MVP behavior clear across all three
- Form state management patterns documented and consistent

---

## Next Steps

1. **Code Review**: PR ready for review with all clarifications documented
2. **Implementation**: Developers can now implement with clear, unambiguous specifications
3. **Phase Ordering**: Follow new task numbering (T001-T159) in order
4. **Checkpoint Validation**: Stop at each phase checkpoint (after Foundational, after US1, after US2, etc.) to validate progress

---

## Document Links

- [Specification](spec.md) - Feature requirements and acceptance criteria
- [Implementation Plan](plan.md) - Architecture, design decisions, and technical approach
- [Tasks](tasks.md) - Granular implementation checklist with TDD approach

**Status**: Ready for implementation 🚀


# World Sidebar Specification - Final Clarifications

**Status**: ✅ Ready for Implementation  
**Last Updated**: 2026-01-15

---

## Critical Clarifications

### 1️⃣ API Endpoint Pattern

**Decided**: Path-based pattern for hierarchy operations (matches existing backend)

```http
✅ GET /api/v1/worlds/{worldId}/entities                         # All entities
✅ GET /api/v1/worlds/{worldId}/entities/{parentId}/children     # Children of parent
```

**Why**: Matches existing backend implementation. Two separate endpoints provide clear semantics and are equally cacheable with RTK Query.

**Frontend Logic**:
- Root entities: Call `GET /entities`, filter where `parentId == worldId`
- Expand entity: Call `GET /entities/{parentId}/children`
- Both use same cache tag: `{ type: 'WorldEntity', id: worldId }`

---

### 2️⃣ Mobile ChatWindow UX

**Pattern**: Bottom sheet drawer (swipes up from bottom of screen)

```
Desktop (1024px+)     Tablet (768px-1023px)    Mobile (<768px)
┌─────────────────────────────────────────────────────────┐
│ Sidebar | Main Panel | ChatWindow    │ Sidebar | Main   │
│    |       (flex)    |   (350px)     │ (250px) | (flex) │
│    |                 | always right  │         | + chat │
└─────────────────────────────────────────────────────────┘
                                    ↓ swipe-up ↓
                        ┌──────────────────────┐
                        │  ChatWindow Sheet    │
                        │  (30-40% height)     │
                        └──────────────────────┘
                        Tap to toggle or swipe-down to close
```

**Key**: Always visible affordance (drag handle) on mobile—user never needs to hunt for ChatWindow.

---

### 3️⃣ Entity Edit Button (MVP)

**Visible but disabled** with tooltip: "Entity editing coming in Phase 2"

```typescript
<button disabled title="Entity editing coming in Phase 2">
  Edit Entity
</button>
```

**Phase 2**: Button will be enabled with full edit form.

---

### 4️⃣ Cache Invalidation Scope

**Rule**: Entire world hierarchy cache clears on ANY entity change

```typescript
// When entity is created, updated, deleted, or moved:
sessionStorage.removeItem(`sidebar_hierarchy_{worldId}`);
// User will see fresh data on next expansion
```

**Why**: Pessimistic approach prioritizes correctness over performance. Better to re-fetch than serve stale children.

---

### 5️⃣ Form Placement Strategy

| Operation | UI Pattern | Reason |
|-----------|-----------|--------|
| **Create World** | Main Panel Form | ChatWindow for creative suggestions |
| **Edit World** | Main Panel Form | ChatWindow for brainstorming refinements |
| **Create Entity** | Main Panel Form | ChatWindow for AI-assisted entity ideas |
| **Edit Entity** | Main Panel Form | Future phase (MVP is view-only) |
| **View Entity** | Main Panel Form (read-only) | ChatWindow for reference during navigation |
| **Delete Entity** | Modal Confirmation | Blocking UX prevents accidental deletion |
| **Move Entity** | Modal Picker | Complex selection; may become AI-driven later |

**Pattern**: Main panel forms (not modals) keep ChatWindow visible during all creation/editing decisions.

---

### Key Reference

| Key | Pattern |
|-----|---------|
| **All Entities** | `GET /api/v1/worlds/{worldId}/entities` |
| **Children** | `GET /api/v1/worlds/{worldId}/entities/{parentId}/children` |
| **Root Entities** | Filter all entities where `parentId == worldId` |
| **RTK Queries** | `useGetEntitiesQuery(worldId)`, `useGetChildrenQuery(worldId, parentId)` |

---

### 6️⃣ Optimistic Update Feedback

**User Experience**:

```
1. User submits form
   ↓
2. Redux state updates IMMEDIATELY (user sees feedback instantly)
   ↓
3. API request sends to backend
   ↓
4a. API succeeds → Cache updates, user sees "Saved ✓"
4b. API fails → Redux state reverts, error message shows "Retry"
```

**Benefit**: Perceived performance is instant, even on slow connections.

---

### 7️⃣ Unsaved Changes Behavior

When user tries to navigate away with unsaved form input:

```
┌──────────────────────────────────┐
│ You have unsaved changes.         │
│ What would you like to do?        │
├──────────────────────────────────┤
│ [ Save ] [ Discard ] [ Cancel ]   │
└──────────────────────────────────┘
```

Applied to all creation/editing forms.

---

### 8️⃣ Empty State Display

**When world has no entities:**

- **Sidebar**: EntityTree region shows: "No entities yet. Start building your world by adding locations, characters, or campaigns."
- **Main Panel**: Shows complementary empty state with "Add Root Entity" button
- **Both**: Coordinated via Redux state—when user adds first entity, both regions update

---

### 9️⃣ Keyboard Navigation (ARIA Tree Pattern)

**Supported in Entity Hierarchy**:

| Key | Action |
|-----|--------|
| `↑ Arrow Up` | Move to previous entity in same level |
| `↓ Arrow Down` | Move to next entity in same level |
| `→ Right Arrow` | Expand entity to show children (or move to first child if already expanded) |
| `← Left Arrow` | Collapse entity (or move to parent if already collapsed) |
| `Enter` | Select entity and display in main panel |
| `Escape` | Close any open menus/modals |
| `Shift+F10` | Open context menu for entity |

**ARIA Roles Applied**:
- `role="tree"` on EntityTree container
- `role="treeitem"` on each EntityTreeNode
- `aria-expanded="true|false"` for expand/collapse state
- `aria-level={depth}` for nesting level
- `aria-selected="true|false"` for current selection

---

### 🔟 Task Numbering (CRITICAL)

All tasks are now sequential **T001-T159** with no gaps or collisions:

- Phase 1: Setup (T001-T011)
- Phase 2: Foundational (T012-T023)
- Phase 3: US1 - World Creation (T024-T042)
- Phase 4: US2 - Hierarchy Navigation (T043-T073)
- Phase 5: US3 - World Editing (T074-T086)
- Phase 5.5: Edit Button MVP (T087)
- Phase 6: US2.5 - Entity CRUD (T088-T109)
- Phase 7: Responsive Design (T110-T121)
- Phase 8: US4 - Caching Optimization (T122-T134)
- Phase 9: US5 - Visual Polish (T135-T148)
- Phase 10: Cross-Cutting Polish (T149-T159)

**Follow this order during implementation.**

---

## Quick Reference

### File Locations

```
libris-maleficarum-app/src/
├── components/
│   ├── WorldSidebar/
│   │   ├── WorldSidebar.tsx
│   │   ├── WorldSelector.tsx
│   │   ├── EntityTree.tsx
│   │   ├── EntityTreeNode.tsx
│   │   ├── EntityContextMenu.tsx
│   │   └── EmptyState.tsx
│   └── MainPanel/
│       ├── MainPanel.tsx (router component)
│       ├── WorldDetailForm.tsx
│       ├── EntityDetailForm.tsx
│       ├── EntityCreationForm.tsx
│       └── DeleteConfirmationModal.tsx
├── services/
│   ├── worldEntityApi.ts (RTK Query endpoints)
│   └── types/worldEntity.types.ts
├── lib/
│   ├── sessionCache.ts (cache utilities)
│   └── entityIcons.ts (icon mapping)
└── store/
    └── worldSidebarSlice.ts (Redux state)
```

### Key Dependencies (No New Packages)

- React 19
- Redux Toolkit 2.11
- RTK Query
- Shadcn/UI (Dialog, ContextMenu, Select, Drawer)
- Lucide icons
- Vitest + Testing Library + jest-axe

### Performance Targets

| Metric | Target |
|--------|--------|
| Initial root entity load | <2s (50 entities) |
| Cached expansion | <100ms |
| World switching | <200ms (state restore) |
| Form transition | <300ms (CSS animation) |
| Cache hit rate | >80% for typical navigation |

---

## Common Questions Answered

**Q: Why `?parentId={id}` instead of `/{parentId}/children`?**  
A: RTK Query cache tags work better with query params. Simpler endpoint proliferation.

**Q: Why is delete modal but edit is main panel?**  
A: Delete is destructive (needs blocking confirmation). Edit is creative (benefits from ChatWindow suggestions).

**Q: When can I start implementing?**  
A: After Phase 2 (Foundational) completes. Phase 1 is setup, Phase 2 is blocking prerequisites.

**Q: What if I find something ambiguous during implementation?**  
A: Check this document first. If still unclear, ping team in PR with question—these clarifications should cover 99% of cases.

**Q: Do I write tests before or after code?**  
A: **Before** (TDD). Write tests, ensure they fail, implement code until tests pass.

---

## Next Steps

1. **Review**: Read through all clarifications above
2. **Questions?**: Ask before starting implementation
3. **Start**: Phase 1 Setup (T001-T011)
4. **Reference**: Keep these docs open during implementation
5. **Validate**: Stop at checkpoint after each major phase

---

## Files to Reference

| Document | Purpose |
|----------|---------|
| `spec.md` | Feature requirements and acceptance criteria |
| `plan.md` | Technical architecture and design decisions |
| `tasks.md` | Granular implementation checklist |
| `REMEDIATION_SUMMARY.md` | Detailed explanation of all fixes applied |
| `STATUS.md` | Sign-off and readiness assessment |
| **THIS FILE** | Quick reference for developers |

---

**Last Updated**: January 15, 2026  
**Status**: ✅ Ready for Implementation  
**Questions?** Refer to spec.md, plan.md, or ask in PR comments

Happy coding! 🚀

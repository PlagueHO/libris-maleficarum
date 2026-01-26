# Implementation Tasks: Schema-Driven Entity Properties (Phase 1)

**Plan**: [plan.md](plan.md)  
**Status**: Not Started

## Phase 0: Schema Foundation ✅ 0/3

- [ ] Define PropertyFieldSchema interface in entityTypeRegistry.ts
- [ ] Extend EntityTypeConfig with optional propertySchema array
- [ ] Add propertySchema to 4 Regional types (Geographic, Political, Cultural, Military)

## Phase 1: Dynamic Field Renderer ✅ 0/3

- [ ] Create DynamicPropertyField component
- [ ] Create propertyValidation utility
- [ ] Add duplicate tag prevention to TagInput (if needed)

## Phase 2: Edit Mode Integration ✅ 0/3

- [ ] Create DynamicPropertiesForm component
- [ ] Update WorldEntityForm to use DynamicPropertiesForm
- [ ] Handle empty/undefined properties on save

## Phase 3: Read-Only Mode Integration ✅ 0/2

- [ ] Create DynamicPropertiesView component
- [ ] Update EntityDetailReadOnlyView to use DynamicPropertiesView

## Phase 4: Testing & Validation ✅ 0/5

- [ ] Unit tests for DynamicPropertyField
- [ ] Unit tests for DynamicPropertiesForm
- [ ] Unit tests for DynamicPropertiesView
- [ ] Integration tests for all user stories
- [ ] Update existing tests (WorldEntityForm, EntityDetailReadOnlyView)

## Phase 5: Migration & Cleanup ✅ 0/4

- [ ] Verify existing entity data compatibility
- [ ] Delete old custom property components (9 files)
- [ ] Update imports and remove unused code
- [ ] Run code quality checks (lint, type-check, test, deadcode)

---

**Total Tasks**: 20  
**Completed**: 0  
**Remaining**: 20

**Progress**: 0%

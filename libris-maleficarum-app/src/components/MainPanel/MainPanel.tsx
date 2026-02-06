import { useSelector, useDispatch } from 'react-redux';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { selectSelectedEntityId, selectSelectedWorldId, selectMainPanelMode, selectEditingWorldId, openEntityFormEdit } from '@/store/worldSidebarSlice';
import { useGetWorldEntityByIdQuery } from '@/services/worldEntityApi';
import { useGetWorldByIdQuery } from '@/services/worldApi';
import { WorldDetailForm } from './WorldDetailForm';
import { EntityDetailForm as WorldEntityForm } from './WorldEntityForm';
import { EntityDetailReadOnlyView } from './EntityDetailReadOnlyView';
import { Loader2 } from 'lucide-react';

export function MainPanel() {
  const dispatch = useDispatch();
  const selectedWorldId = useSelector(selectSelectedWorldId);
  const selectedEntityId = useSelector(selectSelectedEntityId);
  const mainPanelMode = useSelector(selectMainPanelMode);
  const editingWorldId = useSelector(selectEditingWorldId);

  // Get editing world data if in edit mode
  const { data: editingWorld } = useGetWorldByIdQuery(editingWorldId!, {
    skip: !editingWorldId,
  });

  const { data: entity, isLoading, error } = useGetWorldEntityByIdQuery(
    { worldId: selectedWorldId!, entityId: selectedEntityId! },
    { skip: !selectedWorldId || !selectedEntityId }
  );

  const handleEditClick = () => {
    if (selectedEntityId) {
      dispatch(openEntityFormEdit(selectedEntityId));
    }
  };

  // Creating World Mode
  if (mainPanelMode === 'creating_world') {
    return <WorldDetailForm mode="create" world={undefined} />;
  }

  // Editing World Mode
  if (mainPanelMode === 'editing_world' && editingWorldId && editingWorld) {
    return <WorldDetailForm mode="edit" world={editingWorld} />;
  }

  // Creating Entity Mode
  if (mainPanelMode === 'creating_entity') {
    return <WorldEntityForm />;
  }

  // Editing Entity Mode
  if (mainPanelMode === 'editing_entity') {
    return <WorldEntityForm />;
  }

  // Initial Welcome State (No Entity Selected)
  if (!selectedEntityId) {
    return (
      <main className="flex-1 p-6 overflow-auto">
        <div className="max-w-4xl mx-auto space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-2xl">Welcome, Chronicler</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground">
                Your arcane tome for chronicling worlds, shaping realms, and weaving
                the tales of your campaigns. Consult the Codex to explore your realms,
                or summon Malleus, your arcane advisor, for aid in crafting new lore.
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Begin Your Chronicle</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <p className="text-muted-foreground">
                Forge your first realm, inscribe its lands, denizens, and quests.
                Malleus will help you conjure rich, detailed lore for your adventures.
              </p>
            </CardContent>
          </Card>
        </div>
      </main>
    );
  }

  // Loading State
  if (isLoading) {
    return (
      <main className="flex-1 p-6 overflow-auto flex items-center justify-center h-full">
        <div role="status" aria-label="Consulting the tome" className="flex flex-col items-center gap-2 text-muted-foreground">
          <Loader2 className="h-8 w-8 animate-spin" />
          <p>Consulting the tome...</p>
        </div>
      </main>
    );
  }

  // Error State
  if (error || !entity) {
    return (
      <main className="flex-1 p-6 overflow-auto">
         <div className="max-w-4xl mx-auto">
          <Card className="border-destructive/50">
            <CardHeader>
              <CardTitle className="text-destructive">The Scroll Is Damaged</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground">
                The entry you seek could not be recovered from the grimoire. It may have been lost to the void or sealed beyond your reach.
              </p>
            </CardContent>
          </Card>
         </div>
      </main>
    );
  }

  // Entity Details View (mainPanelMode === 'viewing_entity')
  return (
    <main className="flex-1 p-6 overflow-auto">
      <EntityDetailReadOnlyView entity={entity} onEditClick={handleEditClick} />
    </main>
  );
}

import { useSelector } from 'react-redux';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { selectSelectedEntityId, selectSelectedWorldId, selectMainPanelMode, selectEditingWorldId } from '@/store/worldSidebarSlice';
import { useGetWorldEntityByIdQuery } from '@/services/worldEntityApi';
import { useGetWorldByIdQuery } from '@/services/worldApi';
import { WorldDetailForm } from './WorldDetailForm';
import { EntityDetailForm } from './EntityDetailForm';
import { Loader2 } from 'lucide-react';

export function MainPanel() {
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
    return <EntityDetailForm />;
  }

  // Editing Entity Mode
  if (mainPanelMode === 'editing_entity') {
    return <EntityDetailForm />;
  }

  // Initial Welcome State (No Entity Selected)
  if (!selectedEntityId) {
    return (
      <main className="flex-1 p-6 overflow-auto">
        <div className="max-w-4xl mx-auto space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-2xl">Welcome to Libris Maleficarum</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground">
                Your personal grimoire for world-building and campaign management.
                Use the sidebar to navigate between different aspects of your world,
                or ask the AI assistant for help creating new content.
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Getting Started</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <p className="text-muted-foreground">
                Create your first world, add locations, characters, and quests.
                The AI will help you generate rich, detailed content for your campaigns.
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
        <div role="status" aria-label="Loading entity details" className="flex flex-col items-center gap-2 text-muted-foreground">
          <Loader2 className="h-8 w-8 animate-spin" />
          <p>Loading entity details...</p>
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
              <CardTitle className="text-destructive">Error Loading Entity</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground">
                Unable to load the selected entity. It may have been deleted or you don't have permission to view it.
              </p>
            </CardContent>
          </Card>
         </div>
      </main>
    );
  }

  // Entity Details View
  return (
    <main className="flex-1 p-6 overflow-auto">
      <div className="max-w-4xl mx-auto space-y-6">
        <Card>
          <CardHeader className="space-y-4">
            <div className="flex items-start justify-between">
              <div className="space-y-1">
                <CardTitle className="text-3xl font-bold" role="heading" aria-level={1}>{entity.name}</CardTitle>
                <div className="flex items-center gap-2">
                  <Badge variant="outline">{entity.entityType}</Badge>
                  {entity.tags.map(tag => (
                    <Badge key={tag} variant="secondary" className="text-xs">
                      {tag}
                    </Badge>
                  ))}
                </div>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {entity.description ? (
              <div className="prose dark:prose-invert max-w-none">
                 <p className="whitespace-pre-wrap">{entity.description}</p>
              </div>
            ) : (
                <p className="text-muted-foreground italic">No description available.</p>
            )}
          </CardContent>
        </Card>
      </div>
    </main>
  );
}

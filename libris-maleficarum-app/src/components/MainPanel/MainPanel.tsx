import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export function MainPanel() {
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

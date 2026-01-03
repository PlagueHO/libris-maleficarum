import { Globe, MapPin, Users, Scroll, Sword } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import { useSelector } from 'react-redux';
import type { RootState } from '@/store/store';

export function SidePanel() {
  const isExpanded = useSelector((state: RootState) => state.sidePanel.isExpanded);

  if (!isExpanded) {
    return null;
  }

  return (
    <aside className="w-64 border-r border-border bg-card" aria-label="Side Panel">
      <ScrollArea className="h-full">
        <div className="p-4 space-y-4">
          <div>
            <h2 className="text-sm font-semibold text-muted-foreground mb-2">WORLD</h2>
            <div className="space-y-1">
              <Button variant="ghost" className="w-full justify-start gap-2">
                <Globe className="h-4 w-4" />
                <span>Realms</span>
              </Button>
              <Button variant="ghost" className="w-full justify-start gap-2">
                <MapPin className="h-4 w-4" />
                <span>Locations</span>
              </Button>
            </div>
          </div>

          <Separator />

          <div>
            <h2 className="text-sm font-semibold text-muted-foreground mb-2">CAMPAIGN</h2>
            <div className="space-y-1">
              <Button variant="ghost" className="w-full justify-start gap-2">
                <Users className="h-4 w-4" />
                <span>Characters</span>
              </Button>
              <Button variant="ghost" className="w-full justify-start gap-2">
                <Scroll className="h-4 w-4" />
                <span>Quests</span>
              </Button>
              <Button variant="ghost" className="w-full justify-start gap-2">
                <Sword className="h-4 w-4" />
                <span>Encounters</span>
              </Button>
            </div>
          </div>
        </div>
      </ScrollArea>
    </aside>
  );
}

import { Menu, Sparkles } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { useDispatch } from 'react-redux';
import { toggle } from '@/store/store';

export function TopToolbar() {
  const dispatch = useDispatch();

  return (
    <header data-testid="top-toolbar" className="border-b border-border bg-card">
      <div className="flex h-14 items-center px-4 gap-2">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => dispatch(toggle())}
          aria-label="Toggle sidebar"
        >
          <Menu className="h-5 w-5" />
        </Button>

        <Separator orientation="vertical" className="h-6" />

        <div className="flex items-center gap-2">
          <Sparkles className="h-5 w-5 text-primary" />
          <h1 className="text-lg font-semibold">Libris Maleficarum</h1>
        </div>

        <div className="ml-auto flex items-center gap-2">
          {/* Additional toolbar buttons can go here */}
        </div>
      </div>
    </header>
  );
}

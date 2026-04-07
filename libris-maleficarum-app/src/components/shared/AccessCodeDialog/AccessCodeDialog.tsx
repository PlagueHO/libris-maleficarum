/**
 * AccessCodeDialog Component
 *
 * Non-dismissible modal dialog that prompts for an access code
 * when the application is configured to require one.
 */

import { useState, type FormEvent, type KeyboardEvent } from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Lock, Loader2 } from 'lucide-react';

export interface AccessCodeDialogProps {
  /** Whether the dialog is open */
  open: boolean;
  /** Callback when the user submits an access code */
  onSubmit: (code: string) => void;
  /** Whether the submission is in progress */
  isLoading?: boolean;
  /** Error message to display */
  error?: string | null;
}

export function AccessCodeDialog({
  open,
  onSubmit,
  isLoading = false,
  error,
}: AccessCodeDialogProps) {
  const [code, setCode] = useState('');

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (code.trim() && !isLoading) {
      onSubmit(code.trim());
    }
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && code.trim() && !isLoading) {
      e.preventDefault();
      onSubmit(code.trim());
    }
  };

  return (
    <Dialog
      open={open}
      onOpenChange={() => {
        /* prevent dismissal */
      }}
    >
      <DialogContent
        showCloseButton={false}
        onPointerDownOutside={(e) => e.preventDefault()}
        onEscapeKeyDown={(e) => e.preventDefault()}
        aria-labelledby="access-code-dialog-title"
        aria-describedby="access-code-dialog-description"
      >
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <div className="flex items-center gap-2">
              <Lock className="size-5 text-muted-foreground" aria-hidden="true" />
              <DialogTitle id="access-code-dialog-title">
                Access Code Required
              </DialogTitle>
            </div>
            <DialogDescription id="access-code-dialog-description">
              Enter the access code to continue using the application.
            </DialogDescription>
          </DialogHeader>

          <div className="py-4">
            <label htmlFor="access-code-input" className="sr-only">
              Access code
            </label>
            <Input
              id="access-code-input"
              type="password"
              placeholder="Enter access code"
              value={code}
              onChange={(e) => setCode(e.target.value)}
              onKeyDown={handleKeyDown}
              disabled={isLoading}
              autoFocus
              aria-invalid={!!error}
              aria-describedby={error ? 'access-code-error' : undefined}
            />
            {error && (
              <p
                id="access-code-error"
                className="mt-2 text-sm text-destructive"
                role="alert"
              >
                {error}
              </p>
            )}
          </div>

          <DialogFooter>
            <Button
              type="submit"
              disabled={!code.trim() || isLoading}
            >
              {isLoading ? (
                <>
                  <Loader2 className="size-4 animate-spin" aria-hidden="true" />
                  Verifying...
                </>
              ) : (
                'Submit'
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

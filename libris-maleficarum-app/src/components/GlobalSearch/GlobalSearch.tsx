import { useMemo, useRef, useState } from 'react';
import { Loader2, Search, X } from 'lucide-react';

import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useEntitySearch, MIN_QUERY_LENGTH } from '@/hooks/useEntitySearch';
import { useSearchHistory } from '@/hooks/useSearchHistory';
import { formatEntityType } from '@/lib/entityTypeHelpers';
import { getAllEntityTypes } from '@/lib/entityIcons';
import { useAppDispatch, useAppSelector } from '@/store/store';
import { selectSelectedWorldId, setExpandedNodes, setSelectedEntity, setSelectedWorld } from '@/store/worldSidebarSlice';

export function GlobalSearch() {
  const dispatch = useAppDispatch();
  const selectedWorldId = useAppSelector(selectSelectedWorldId);
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [entityTypeFilter, setEntityTypeFilter] = useState('all');
  const inputRef = useRef<HTMLInputElement>(null);
  const closeTimerRef = useRef<ReturnType<typeof setTimeout>>(undefined);

  const entityTypeQuery = entityTypeFilter === 'all' ? undefined : entityTypeFilter;
  const { results, isSearching, isEmptyQuery, isError } = useEntitySearch(query, entityTypeQuery);
  const { history, recentEntities, pinnedEntities, addRecentEntity, addSearchQuery } = useSearchHistory(selectedWorldId);

  const placeholder = useMemo(
    () => (selectedWorldId ? 'Search this world…' : 'Select a world to search'),
    [selectedWorldId],
  );

  const isIdleQuery = query.trim().length === 0;
  const hasHistory = history.length > 0 || recentEntities.length > 0 || pinnedEntities.length > 0;
  const showDropdown = open && (!isIdleQuery || isSearching || (isIdleQuery && hasHistory));

  const scheduleClose = () => {
    closeTimerRef.current = setTimeout(() => setOpen(false), 150);
  };

  const cancelClose = () => {
    clearTimeout(closeTimerRef.current);
  };

  const handleSelect = (entityId: string) => {
    const entity = results.find((item) => item.id === entityId);
    if (!entity) return;
    addSearchQuery(query);
    addRecentEntity(entity);
    dispatch(setSelectedWorld(entity.worldId));
    dispatch(setSelectedEntity(entity.id));
    dispatch(setExpandedNodes(Array.from(new Set([...(entity.path ?? []), entity.id]))));
    setQuery('');
    setOpen(false);
    inputRef.current?.blur();
  };

  const handleClear = () => {
    setQuery('');
    inputRef.current?.focus();
  };

  return (
    <div className="relative w-full">
      {/* Search input — always visible in the header */}
      <div className="relative">
        <Search
          className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
          aria-hidden="true"
        />
        <Input
          ref={inputRef}
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onFocus={() => { cancelClose(); setOpen(true); }}
          onBlur={scheduleClose}
          placeholder={placeholder}
          disabled={!selectedWorldId}
          className="h-9 rounded-full border-border/70 bg-background pl-9 pr-8 text-sm shadow-sm"
          aria-label="Search entities"
          aria-expanded={showDropdown}
          aria-haspopup="listbox"
          role="combobox"
          aria-autocomplete="list"
        />
        {isSearching ? (
          <Loader2
            className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 animate-spin text-muted-foreground"
            aria-hidden="true"
          />
        ) : query ? (
          <button
            type="button"
            className="absolute right-2.5 top-1/2 -translate-y-1/2 rounded text-muted-foreground hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            onMouseDown={(e) => e.preventDefault()}
            onClick={handleClear}
            aria-label="Clear search"
            tabIndex={-1}
          >
            <X className="h-3.5 w-3.5" aria-hidden="true" />
          </button>
        ) : null}
      </div>

      {/* Results dropdown */}
      {showDropdown ? (
        <div
          aria-label="Search results"
          className="absolute right-0 top-full z-50 mt-1 w-full rounded-lg border border-border bg-popover text-popover-foreground shadow-md"
          onMouseDown={(e) => e.preventDefault()}
          onFocus={cancelClose}
        >
          {/* Entity type filter */}
          <div className="flex items-center gap-2 border-b px-3 py-2">
            <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Type</span>
            <Select value={entityTypeFilter} onValueChange={setEntityTypeFilter} disabled={!selectedWorldId}>
              <SelectTrigger className="h-7 w-36 text-xs" aria-label="Filter search results by entity type">
                <SelectValue placeholder="All types" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All types</SelectItem>
                {getAllEntityTypes().map((type) => (
                  <SelectItem key={type} value={type}>
                    {formatEntityType(type)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Content */}
          <div className="max-h-80 overflow-y-auto">
            {isSearching ? (
              <div className="px-3 py-4 text-sm text-muted-foreground" role="status" aria-live="polite">
                Searching…
              </div>
            ) : isError ? (
              <div className="px-3 py-4 text-sm text-destructive" role="alert">
                Search failed. Please try again.
              </div>
            ) : !isSearching && isIdleQuery && hasHistory ? (
              <div className="space-y-3 px-3 py-3">
                {pinnedEntities.length > 0 ? (
                  <div>
                    <p className="mb-1 text-xs uppercase tracking-wide text-muted-foreground">Pinned</p>
                    {pinnedEntities.map((item) => (
                      <button
                        key={`pinned-${item.id}`}
                        type="button"
                        className="block w-full rounded-md px-2 py-1.5 text-left text-sm hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                        onClick={() => setQuery(item.name)}
                      >
                        {item.name}
                      </button>
                    ))}
                  </div>
                ) : null}
                {recentEntities.length > 0 ? (
                  <div>
                    <p className="mb-1 text-xs uppercase tracking-wide text-muted-foreground">Recently opened</p>
                    {recentEntities.map((item) => (
                      <button
                        key={`recent-${item.id}`}
                        type="button"
                        className="block w-full rounded-md px-2 py-1.5 text-left text-sm hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                        onClick={() => setQuery(item.name)}
                      >
                        {item.name}
                      </button>
                    ))}
                  </div>
                ) : null}
                {history.length > 0 ? (
                  <div>
                    <p className="mb-1 text-xs uppercase tracking-wide text-muted-foreground">Recent searches</p>
                    {history.map((item) => (
                      <button
                        key={`history-${item.query}`}
                        type="button"
                        className="block w-full rounded-md px-2 py-1.5 text-left text-sm hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                        onClick={() => setQuery(item.query)}
                      >
                        {item.query}
                      </button>
                    ))}
                  </div>
                ) : null}
              </div>
            ) : !isSearching && !isIdleQuery && isEmptyQuery ? (
              <div className="px-3 py-4 text-sm text-muted-foreground">
                Type at least {MIN_QUERY_LENGTH} characters to search this world.
              </div>
            ) : !isSearching && !isEmptyQuery && results.length === 0 ? (
              <div className="px-3 py-4 text-sm text-muted-foreground" role="status" aria-live="polite">
                No entities matched that search.
              </div>
            ) : !isSearching && results.length > 0 ? (
              <>
                <div className="px-3 pt-2 pb-1">
                  <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Matches</span>
                </div>
                {results.map((item) => (
                  <button
                    key={item.id}
                    type="button"
                    className="flex w-full flex-col items-start gap-0.5 px-3 py-2 text-left hover:bg-accent focus-visible:bg-accent focus-visible:outline-none focus-visible:ring-inset focus-visible:ring-2 focus-visible:ring-ring"
                    onClick={() => handleSelect(item.id)}
                  >
                    <span className="text-sm font-medium text-foreground">{item.name}</span>
                    <span className="text-xs text-muted-foreground">{item.entityType}</span>
                    {item.descriptionSnippet ? (
                      <span className="text-xs text-muted-foreground">{item.descriptionSnippet}</span>
                    ) : null}
                  </button>
                ))}
              </>
            ) : null}
          </div>
        </div>
      ) : null}
    </div>
  );
}

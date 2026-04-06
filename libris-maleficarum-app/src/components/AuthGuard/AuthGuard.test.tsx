import type { ReactNode } from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';
import { AuthGuard } from './AuthGuard';

expect.extend(toHaveNoViolations);

// Mock the auth config module
vi.mock('@/auth/authConfig', () => ({
  isAuthConfigured: false,
  loginRequest: { scopes: ['api://libris-maleficarum-api/access_as_user'] },
}));

function renderAuthGuard(children: ReactNode = <div>Protected content</div>) {
  return render(
    <AuthGuard>{children}</AuthGuard>
  );
}

describe('AuthGuard', () => {
  it('renders children in anonymous mode (auth not configured)', () => {
    renderAuthGuard(<p>Protected content</p>);
    expect(screen.getByText('Protected content')).toBeInTheDocument();
  });

  it('has no accessibility violations in anonymous mode', async () => {
    const { container } = renderAuthGuard(<p>Protected content</p>);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});

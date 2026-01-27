import { describe, it, test, beforeAll, afterEach, afterAll, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { store } from '../../store/store';
import App from '../../App';

// Setup mock server
import { setupServer } from 'msw/node';
import { handlers } from '../mocks/handlers';

const server = setupServer(...handlers);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

describe('Entity Creation Integration', () => {
  test('should create a new entity successfully', async () => {
    vi.setConfig({ testTimeout: 30000 });
    render(
      <Provider store={store}>
        <App />
      </Provider>
    );

    const user = userEvent.setup();

    // 1. Select a world to enable entity creation
    const worldTrigger = await screen.findByRole('combobox', { name: /select world/i });
    await user.click(worldTrigger);
    
    // Select the option explicitly using role 'option'
    const worldOption = await screen.findByRole('option', { name: 'Forgotten Realms' });
    await user.click(worldOption);

    // 2. Open the Create Entity modal
    // Find the "+" button next to "Entities" header in sidebar using correct aria-label
    const createBtn = await screen.findByRole('button', { name: /add root entity/i });
    await user.click(createBtn);

    // 3. Fill out the form
    const nameInput = await screen.findByLabelText(/name/i);
    await user.type(nameInput, 'New Castle');

    const typeSelectTrigger = await screen.findByLabelText(/type/i); // "Type" label
    await user.click(typeSelectTrigger);
    
    // Select a valid type (Continent is valid for root)
    const continentOption = await screen.findByRole('option', { name: /continent/i });
    await user.click(continentOption);

    const descInput = await screen.findByLabelText(/description/i);
    await user.type(descInput, 'A newly created castle');

    // 4. Submit
    const createSubmitBtn = await screen.findByRole('button', { name: /^create$/i });
    await user.click(createSubmitBtn);

    // 5. Verify it appears in the tree (EntityTree updates automatically via tag invalidation)
    // The mock handler adds it to the map and returns it.
    // The tree should re-render with "New Castle"
    await waitFor(() => {
      expect(screen.getByText('New Castle')).toBeInTheDocument();
    });
  });
});

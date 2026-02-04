import '@testing-library/jest-dom/vitest';
import { vi } from 'vitest';

// Mock scrollIntoView for Radix UI components
window.HTMLElement.prototype.scrollIntoView = function() {
  // No-op in test environment
};

// Mock pointer capture methods for Radix UI components (not available in JSDOM)
window.HTMLElement.prototype.hasPointerCapture = function() {
  return false;
};

window.HTMLElement.prototype.setPointerCapture = function() {
  // No-op in test environment
};

window.HTMLElement.prototype.releasePointerCapture = function() {
  // No-op in test environment
};

// Mock PointerEvent for Radix UI (not available in JSDOM)
if (!globalThis.PointerEvent) {
  globalThis.PointerEvent = class PointerEvent extends MouseEvent {
    height: number;
    isPrimary: boolean;
    pointerId: number;
    pointerType: string;
    pressure: number;
    tangentialPressure: number;
    tiltX: number;
    tiltY: number;
    twist: number;
    width: number;
    
    constructor(type: string, params: PointerEventInit = {}) {
      super(type, params);
      this.height = params.height ?? 0;
      this.isPrimary = params.isPrimary ?? false;
      this.pointerId = params.pointerId ?? 0;
      this.pointerType = params.pointerType ?? 'mouse';
      this.pressure = params.pressure ?? 0;
      this.tangentialPressure = params.tangentialPressure ?? 0;
      this.tiltX = params.tiltX ?? 0;
      this.tiltY = params.tiltY ?? 0;
      this.twist = params.twist ?? 0;
      this.width = params.width ?? 0;
    }
  } as unknown as typeof PointerEvent;
}

// Mock window.matchMedia for responsive components
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(), // deprecated
    removeListener: vi.fn(), // deprecated
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

// Mock getComputedStyle for vaul Drawer (needs CSS transform parsing)
const originalGetComputedStyle = window.getComputedStyle;
window.getComputedStyle = function(element: Element) {
  const styles = originalGetComputedStyle(element);
  // Return a proxy that provides defaults for missing CSS properties  
  return new Proxy(styles, {
    get(target, prop) {
      if (prop === 'transform' && !target[prop as keyof CSSStyleDeclaration]) {
        return 'none';
      }
      return target[prop as keyof CSSStyleDeclaration];
    }
  });
};

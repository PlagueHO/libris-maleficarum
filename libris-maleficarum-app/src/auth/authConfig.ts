import { PublicClientApplication, type Configuration } from '@azure/msal-browser';

declare const __MSAL_CLIENT_ID__: string;
declare const __MSAL_TENANT_ID__: string;

/**
 * Whether Entra ID authentication is configured.
 * When false, the app operates in anonymous single-user mode.
 */
export const isAuthConfigured =
  typeof __MSAL_CLIENT_ID__ !== 'undefined' &&
  __MSAL_CLIENT_ID__ !== '' &&
  __MSAL_CLIENT_ID__ !== 'undefined';

const msalConfig: Configuration = {
  auth: {
    clientId: isAuthConfigured ? __MSAL_CLIENT_ID__ : 'placeholder',
    authority: isAuthConfigured
      ? `https://login.microsoftonline.com/${__MSAL_TENANT_ID__}`
      : undefined,
    redirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',
  },
};

/**
 * MSAL PublicClientApplication instance.
 * Only meaningful when isAuthConfigured is true.
 */
export const msalInstance = new PublicClientApplication(msalConfig);

/**
 * Scopes requested during login for API access.
 */
export const loginRequest = {
  scopes: ['api://libris-maleficarum-api/access_as_user'],
};

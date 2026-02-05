/**
 * Structured Logger with OpenTelemetry Integration
 *
 * Provides environment-aware logging with structured output and telemetry integration:
 * - Development: Console logs with emoji prefixes
 * - Production: Silent console, traces sent to Application Insights via OpenTelemetry
 *
 * Features:
 * - Categorized logging (API, STATE, UI, AUTH, PERF)
 * - OpenTelemetry span integration
 * - Automatic log level filtering (debug only in dev)
 * - Rich context objects
 *
 * @module lib/logger
 */

import { trace, SpanStatusCode } from '@opentelemetry/api';

export type LogLevel = 'debug' | 'info' | 'warn' | 'error';
export type LogCategory = 'API' | 'STATE' | 'UI' | 'AUTH' | 'PERF' | 'TELEMETRY';

interface LogContext {
  [key: string]: unknown;
}

const EMOJI_MAP: Record<LogCategory, string> = {
  API: '🌐',
  STATE: '🔄',
  UI: '🎨',
  AUTH: '🔐',
  PERF: '⚡',
  TELEMETRY: '📊',
};

class Logger {
  private isDevelopment = import.meta.env.DEV;

  constructor() {
    // Verify logger is initialized and environment is detected correctly
    if (this.isDevelopment) {
      console.log('📝 [LOGGER] Initialized in development mode');
    }
  }

  /**
   * Format log message with emoji and category
   */
  private formatMessage(
    category: LogCategory,
    message: string
  ): string {
    const emoji = EMOJI_MAP[category];
    return `${emoji} [${category}] ${message}`;
  }

  /**
   * Add log event to current OpenTelemetry span if active
   */
  private addSpanEvent(
    level: LogLevel,
    category: LogCategory,
    message: string,
    context?: LogContext
  ): void {
    const span = trace.getActiveSpan();
    if (span) {
      span.addEvent(`log.${level}`, {
        'log.level': level,
        'log.category': category,
        'log.message': message,
        ...context,
      });

      // Mark span as error if logging error
      if (level === 'error') {
        span.setStatus({ 
          code: SpanStatusCode.ERROR, 
          message 
        });
      }
    }
  }

  /**
   * Log debug message (development only)
   */
  debug(category: LogCategory, message: string, context?: LogContext): void {
    if (!this.isDevelopment) return;

    const formattedMessage = this.formatMessage(category, message);
    if (context && Object.keys(context).length > 0) {
      console.log(formattedMessage, context);
    } else {
      console.log(formattedMessage);
    }

    this.addSpanEvent('debug', category, message, context);
  }

  /**
   * Log info message (development only)
   */
  info(category: LogCategory, message: string, context?: LogContext): void {
    if (!this.isDevelopment) return;

    const formattedMessage = this.formatMessage(category, message);
    if (context && Object.keys(context).length > 0) {
      console.log(formattedMessage, context);
    } else {
      console.log(formattedMessage);
    }

    this.addSpanEvent('info', category, message, context);
  }

  /**
   * Log warning message (always logged)
   */
  warn(category: LogCategory, message: string, context?: LogContext): void {
    const formattedMessage = this.formatMessage(category, message);
    if (context && Object.keys(context).length > 0) {
      console.warn(formattedMessage, context);
    } else {
      console.warn(formattedMessage);
    }

    this.addSpanEvent('warn', category, message, context);
  }

  /**
   * Log error message (always logged)
   */
  error(category: LogCategory, message: string, context?: LogContext): void {
    const formattedMessage = this.formatMessage(category, message);
    if (context && Object.keys(context).length > 0) {
      console.error(formattedMessage, context);
    } else {
      console.error(formattedMessage);
    }

    this.addSpanEvent('error', category, message, context);

    // Record exception in active span if error object provided
    if (context?.error instanceof Error) {
      const span = trace.getActiveSpan();
      if (span) {
        span.recordException(context.error);
      }
    }
  }

  // ==================== Convenience Methods ====================

  /**
   * Log API request (info level - always visible in development)
   */
  apiRequest(method: string, endpoint: string, context?: LogContext): void {
    this.info('API', `${method} ${endpoint}`, context);
  }

  /**
   * Log API response (info level for success, error level for failures)
   */
  apiResponse(endpoint: string, status: number, context?: LogContext): void {
    if (status >= 400) {
      this.error('API', `${endpoint} → ${status}`, context);
    } else {
      this.info('API', `${endpoint} → ${status}`, context);
    }
  }

  /**
   * Log API error (error level)
   */
  apiError(endpoint: string, error: unknown, context?: LogContext): void {
    this.error('API', `${endpoint} failed`, {
      ...context,
      error,
    });
  }

  /**
   * Log Redux state change (debug level)
   */
  stateChange(action: string, context?: LogContext): void {
    this.debug('STATE', action, context);
  }

  /**
   * Log user interaction (info level)
   */
  userAction(action: string, context?: LogContext): void {
    this.info('UI', action, context);
  }

  /**
   * Log performance metric (debug level)
   */
  performance(metric: string, value: number, unit: string = 'ms', context?: LogContext): void {
    this.debug('PERF', `${metric}: ${value}${unit}`, context);
  }

  /**
   * Log authentication event (info level)
   */
  authEvent(event: string, context?: LogContext): void {
    // Never log sensitive data like tokens or passwords
    const sanitizedContext = context ? { ...context } : undefined;
    if (sanitizedContext) {
      delete sanitizedContext.token;
      delete sanitizedContext.password;
      delete sanitizedContext.secret;
    }
    this.info('AUTH', event, sanitizedContext);
  }
}

/**
 * Global logger instance
 *
 * @example
 * ```typescript
 * import { logger } from '@/lib/logger';
 *
 * // Simple log
 * logger.info('UI', 'User clicked button');
 *
 * // With context
 * logger.error('API', 'Failed to fetch entities', {
 *   worldId,
 *   error,
 * });
 *
 * // Convenience methods
 * logger.apiRequest('GET', '/api/v1/worlds');
 * logger.userAction('Delete entity', { entityId });
 * logger.stateChange('openDeleteConfirmation', { entityId });
 * ```
 */
export const logger = new Logger();

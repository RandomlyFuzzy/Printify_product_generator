import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUTPUT_DIR = path.join(__dirname, '..', 'output');
const ERROR_LOG = path.join(OUTPUT_DIR, 'error.log');

const getTimestamp = () => new Date().toISOString();

const PROJECT_ROOT = path.resolve(__dirname, '..', '..', '..', '..');

const MAX_CAUSE_DEPTH = 5;

const normalizeStack = (stack) => {
  if (!stack) {
    return {
      fullStack: 'No stack trace',
      projectStack: 'No stack trace',
      location: 'unknown',
    };
  }

  const lines = stack.split('\n').map((line) => line.trimEnd());
  const projectLines = lines.filter((line) =>
    line.includes(PROJECT_ROOT) || line.includes('file://')
  );

  const locationLine = lines.find((line) => line.trim().startsWith('at ')) || lines[0] || 'unknown';

  return {
    fullStack: lines.join('\n'),
    projectStack: projectLines.length > 0 ? projectLines.join('\n') : lines.join('\n'),
    location: locationLine,
  };
};

const buildCauseChain = (error, depth = 0) => {
  if (!error || depth >= MAX_CAUSE_DEPTH) return null;

  const stackInfo = normalizeStack(error.stack);
  const code = error?.code !== undefined ? String(error.code) : undefined;

  return {
    name: error.name,
    message: error.message,
    code,
    location: stackInfo.location,
    stack: stackInfo.projectStack,
    cause: buildCauseChain(error.cause, depth + 1),
  };
};

const safeJson = (value) => {
  const seen = new WeakSet();
  return JSON.stringify(value, (key, val) => {
    if (typeof val === 'bigint') return val.toString();
    if (val instanceof Error) {
      return {
        name: val.name,
        message: val.message,
        stack: val.stack,
        code: val.code,
      };
    }
    if (typeof val === 'object' && val !== null) {
      if (seen.has(val)) return '[Circular]';
      seen.add(val);
    }
    return val;
  });
};

const logToFile = (level, message, meta = {}) => {
  const logEntry = {
    timestamp: getTimestamp(),
    level,
    message,
    ...meta,
  };

  fs.mkdirSync(OUTPUT_DIR, { recursive: true });
  fs.appendFileSync(ERROR_LOG, safeJson(logEntry) + '\n');
};

export const logError = (message, error = null, meta = {}) => {
  const stackInfo = normalizeStack(error?.stack || new Error().stack);
  const cause = buildCauseChain(error);
  const code = error?.code !== undefined ? String(error.code) : undefined;

  const errorInfo = {
    errorName: error?.name,
    errorMessage: error?.message,
    errorCode: code,
    location: stackInfo.location,
    stack: stackInfo.projectStack,
    fullStack: stackInfo.fullStack,
    cause,
    ...meta,
  };

  console.error(`[${getTimestamp()}] ERROR: ${message}`);
  console.error(`  Location: ${errorInfo.location || 'unknown'}`);
  if (errorInfo.errorMessage) {
    const codeSuffix = errorInfo.errorCode ? ` (code=${errorInfo.errorCode})` : '';
    console.error(`  Cause: ${errorInfo.errorName}: ${errorInfo.errorMessage}${codeSuffix}`);
  }
  if (Object.keys(meta).length > 0) {
    console.error(`  Meta: ${safeJson(meta)}`);
  }
  if (errorInfo.stack) {
    console.error('  Stack:');
    console.error(errorInfo.stack.split('\n').map((line) => `    ${line}`).join('\n'));
  }
  if (cause?.cause) {
    console.error('  Nested Cause Chain:');
    let current = cause.cause;
    let level = 1;
    while (current) {
      const indent = '    '.repeat(level);
      const codeText = current.code ? ` (code=${current.code})` : '';
      console.error(`${indent}${current.name}: ${current.message}${codeText}`);
      if (current.location) {
        console.error(`${indent}at ${current.location}`);
      }
      current = current.cause;
      level += 1;
    }
  }
  console.error('');

  logToFile('error', message, errorInfo);
};

export const logWarn = (message, meta = {}) => {
  console.warn(`[${getTimestamp()}] WARN: ${message}`, meta);
  logToFile('warn', message, meta);
};

export const logInfo = (message, meta = {}) => {
  console.log(`[${getTimestamp()}] INFO: ${message}`, meta);
  logToFile('info', message, meta);
};

export const logDebug = (message, meta = {}) => {
  if (process.env.DEBUG) {
    console.debug(`[${getTimestamp()}] DEBUG: ${message}`, meta);
    logToFile('debug', message, meta);
  }
};

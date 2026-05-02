import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { loadSearchedQueries, loadSuggestions, normalizeQuery } from './csvLogger.js';
import { logError, logWarn, logDebug } from './logger.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export const OUTPUT_DIR   = path.join(__dirname, '..', 'output');
export const PRODUCTS_DIR = path.join(OUTPUT_DIR, 'products');
export const STATE_FILE   = path.join(OUTPUT_DIR, 'state.json');

/**
 * Write current in-memory sets and any extra fields to state.json.
 * @param {Set<string>} searchedSet
 * @param {Set<string>} suggestionsSet
 * @param {object}      extra - additional fields to merge into the snapshot
 */
export function persistRealtimeState(searchedSet, suggestionsSet, extra = {}) {
  if (!searchedSet || !suggestionsSet) {
    logWarn('persistRealtimeState called with invalid sets', { searchedSet: !!searchedSet, suggestionsSet: !!suggestionsSet });
    return;
  }

  try {
    const state = {
      searched:   [...searchedSet],
      suggestions: [...suggestionsSet],
      updatedAt:  new Date().toISOString(),
      ...extra,
    };

    fs.writeFileSync(STATE_FILE, JSON.stringify(state, null, 2));
    logDebug('State persisted', { searchedCount: searchedSet.size, suggestionsCount: suggestionsSet.size });
  } catch (err) {
    logError('Failed to persist state', err, { stateFile: STATE_FILE });
  }
}

/**
 * Load and merge state from state.json + CSV files into two Sets.
 * @returns {{ searched: Set<string>, suggestions: Set<string> }}
 */
export function loadStateSets() {
  let searchedFromState    = [];
  let suggestionsFromState = [];

  if (fs.existsSync(STATE_FILE)) {
    try {
      const state = JSON.parse(fs.readFileSync(STATE_FILE, 'utf8'));
      if (Array.isArray(state.searched))    searchedFromState    = state.searched;
      if (Array.isArray(state.suggestions)) suggestionsFromState = state.suggestions;
    } catch (e) {
      console.log('Warning: state.json is invalid, continuing with CSV state only');
    }
  }

  const searched = loadSearchedQueries();
  searchedFromState.forEach((q) => {
    const cleanQuery = normalizeQuery(q);
    if (cleanQuery) searched.add(cleanQuery);
  });

  const suggestions = loadSuggestions();
  suggestionsFromState.forEach((s) => {
    const cleanSuggestion = normalizeQuery(s);
    if (cleanSuggestion) suggestions.add(cleanSuggestion);
  });

  return { searched, suggestions };
}

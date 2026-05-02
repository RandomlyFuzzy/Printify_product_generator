import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { loadSearchedQueries, loadSuggestions } from './csvLogger.js';

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
  if (!searchedSet || !suggestionsSet) return;

  const state = {
    searched:   [...searchedSet],
    suggestions: [...suggestionsSet],
    updatedAt:  new Date().toISOString(),
    ...extra,
  };

  fs.writeFileSync(STATE_FILE, JSON.stringify(state, null, 2));
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
  searchedFromState.forEach(q => searched.add(String(q).trim().toLowerCase()));

  const suggestions = loadSuggestions();
  suggestionsFromState.forEach(s => suggestions.add(String(s).trim().toLowerCase()));

  return { searched, suggestions };
}

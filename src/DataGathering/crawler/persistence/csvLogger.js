import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUTPUT_DIR = path.join(__dirname, '..', 'output');

export const SUGGESTIONS_CSV = path.join(OUTPUT_DIR, 'search_suggestions.csv');
export const QUERIES_CSV     = path.join(OUTPUT_DIR, 'queries_searched.csv');
export const CARDS_TXT       = path.join(OUTPUT_DIR, 'cards.txt');

// ── Initialisation ────────────────────────────────────────────────────────────

export function initCSVFiles() {
  if (!fs.existsSync(SUGGESTIONS_CSV)) {
    fs.writeFileSync(SUGGESTIONS_CSV, 'shop,partial_query,suggestion\n');
  }
  if (!fs.existsSync(QUERIES_CSV)) {
    fs.writeFileSync(QUERIES_CSV, 'shop,query,utc_time\n');
  }
}

// ── Writers ───────────────────────────────────────────────────────────────────

/**
 * Append a searched query to the CSV and persist state.
 * @param {string}   query
 * @param {function} persist - bound persistRealtimeState(extra)
 */
export function logQuery(query, persist) {
  const utcTime = new Date().toISOString();
  fs.appendFileSync(QUERIES_CSV, `ebay,${query},${utcTime}\n`);
  persist({ lastQuery: query, lastEvent: 'query_logged' });
}

/**
 * Append an autocomplete suggestion and persist state.
 * Returns the cleaned suggestion string.
 * @param {string}   partialQuery
 * @param {string}   suggestion
 * @param {function} persist - bound persistRealtimeState(extra)
 */
export function logSuggestion(partialQuery, suggestion, persist) {
  const clean = suggestion.replace(/\n.*/g, '').trim().toLowerCase();
  if (clean) {
    fs.appendFileSync(SUGGESTIONS_CSV, `ebay,${partialQuery},${clean}\n`);
    persist({ lastSuggestion: clean, lastEvent: 'suggestion_logged' });
  }
  return clean;
}

/**
 * Append a product card's raw markup to cards.txt and return its URL as ID.
 * Returns null if the card has no URL.
 * @param {object} cardInfo - { url, card_innerHTML }
 * @param {string} query
 */
export function saveCardData(cardInfo, query) {
  const utcTime = new Date().toISOString();
  const rawCardMarkup = cardInfo.card_innerHTML || '';
  fs.appendFileSync(CARDS_TXT, `${utcTime},ebay,${query},${rawCardMarkup}\n`);

  const productId = cardInfo.url;
  if (!productId) {
    console.log('  Skipping card without valid product ID');
    return null;
  }
  return productId;
}

// ── Readers ───────────────────────────────────────────────────────────────────

/** Load all previously searched queries from CSV into a Set. */
export function loadSearchedQueries() {
  const searched = new Set();
  if (fs.existsSync(QUERIES_CSV)) {
    const lines = fs.readFileSync(QUERIES_CSV, 'utf8').split('\n').slice(1);
    lines.forEach(line => {
      const parts = line.split(',');
      if (parts[1]) searched.add(parts[1].trim());
    });
  }
  return searched;
}

/** Load all logged suggestions from CSV into a Set. */
export function loadSuggestions() {
  const suggestions = new Set();
  if (fs.existsSync(SUGGESTIONS_CSV)) {
    const lines = fs.readFileSync(SUGGESTIONS_CSV, 'utf8').split('\n').slice(1);
    lines.forEach(line => {
      const parts = line.split(',');
      if (parts[2]) suggestions.add(parts[2].trim());
    });
  }
  return suggestions;
}

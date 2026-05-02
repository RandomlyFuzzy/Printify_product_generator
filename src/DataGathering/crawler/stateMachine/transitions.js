import { States } from './states.js';

/**
 * Weighted transition tables for probabilistic human-like navigation.
 *
 * Each entry is  [nextState, weight].
 * Weights are relative integers — they do NOT need to sum to 100.
 *
 * Intuition behind the numbers:
 *   - From BROWSE_PAGE we avoid immediate product visits; we usually do an
 *     in-page click interaction first, then continue browsing or wandering.
 *   - CLICK_ON_PAGE is the gate before VISIT_PRODUCT so navigation feels less
 *     abrupt and more human-like.
 *   - From VISIT_PRODUCT we usually return to the same results page (60),
 *     sometimes jump straight to the next page (25), and occasionally wander (15).
 *   - After a NEXT_PAGE we almost always browse the fresh results (85) with
 *     a small chance of an idle detour (15).
 *   - IDLE_WANDER is a "cool-down" that mostly leads back to browsing (60),
 *     occasionally paginates without browsing first (30), or ends the query
 *     early (10).
 */
export const TRANSITIONS = {
  [States.BROWSE_PAGE]: [
    [States.CLICK_ON_PAGE, 25],
    [States.HOVER_CARD,    20],
    [States.PAUSE,        15],
    [States.SCROLL_EXPLORE,10],
    [States.FILTER_CLICK,  10],
    [States.SCROLL_TO_BOTTOM, 10],
    [States.IDLE_WANDER,   10],
  ],

  [States.CLICK_ON_PAGE]: [
    [States.BROWSE_PAGE,  40],
    [States.PAUSE,        20],
    [States.VISIT_PRODUCT, 20],
    [States.HOVER_CARD,   10],
    [States.IDLE_WANDER,  10],
  ],

  [States.HOVER_CARD]: [
    [States.BROWSE_PAGE,  35],
    [States.PAUSE,        20],
    [States.CLICK_ON_PAGE,15],
    [States.VISIT_PRODUCT,15],
    [States.SCROLL_EXPLORE,10],
    [States.IDLE_WANDER,   5],
  ],

  [States.SORT_CHANGE]: [
    [States.BROWSE_PAGE,  50],
    [States.PAUSE,        20],
    [States.SCROLL_EXPLORE,15],
    [States.IDLE_WANDER,  15],
  ],

  [States.FILTER_CLICK]: [
    [States.BROWSE_PAGE,  40],
    [States.PAUSE,        20],
    [States.SCROLL_EXPLORE,20],
    [States.HOVER_CARD,   10],
    [States.IDLE_WANDER,  10],
  ],

  [States.SCROLL_EXPLORE]: [
    [States.BROWSE_PAGE,  35],
    [States.PAUSE,        15],
    [States.HOVER_CARD,   20],
    [States.CLICK_ON_PAGE,15],
    [States.IDLE_WANDER,  15],
  ],

  [States.VISIT_PRODUCT]: [
    [States.PAUSE,        30],
    [States.BROWSE_PAGE,  40],
    [States.NEXT_PAGE,    15],
    [States.SCROLL_EXPLORE,10],
    [States.IDLE_WANDER,   5],
  ],

  [States.NEXT_PAGE]: [
    [States.BROWSE_PAGE,  70],
    [States.PAUSE,        15],
    [States.SORT_CHANGE,  10],
    [States.IDLE_WANDER,   5],
  ],

  [States.SCROLL_TO_BOTTOM]: [
    [States.NEXT_PAGE, 100],
  ],

  [States.SCROLL_TO_TOP]: [
    [States.TYPE_SEARCH, 100],
  ],

  [States.IDLE_WANDER]: [
    [States.BROWSE_PAGE,  45],
    [States.PAUSE,        20],
    [States.SCROLL_EXPLORE,15],
    [States.NEXT_PAGE,    15],
    [States.QUERY_DONE,    5],
  ],

  [States.PAUSE]: [
    [States.BROWSE_PAGE,  40],
    [States.HOVER_CARD,   20],
    [States.SCROLL_EXPLORE,15],
    [States.CLICK_ON_PAGE,10],
    [States.VISIT_PRODUCT, 5],
    [States.IDLE_WANDER,  10],
  ],
};

/**
 * Weighted-random next-state selection.
 *
 * @param {Array<[string, number]>} candidates - [[state, weight], ...]
 * @returns {string} the chosen state key
 */
export function pickNextState(candidates) {
  const total = candidates.reduce((sum, [, w]) => sum + w, 0);
  let rand = Math.random() * total;
  for (const [state, weight] of candidates) {
    rand -= weight;
    if (rand <= 0) return state;
  }
  // Fallback: last entry (guards against floating-point drift)
  return candidates[candidates.length - 1][0];
}

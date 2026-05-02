/**
 * All possible states in the traversal state machine.
 * Frozen to prevent accidental mutation.
 */
export const States = Object.freeze({
  /** Check whether the query was already searched; log it and initialise context. */
  START_QUERY:   'START_QUERY',

  /** Ensure the browser is on the eBay homepage and locate the search bar. */
  ENSURE_HOME:   'ENSURE_HOME',

  /** Scroll to the top of the page before typing a new search query. */
  SCROLL_TO_TOP: 'SCROLL_TO_TOP',

  /** Click and focus the search input, select any existing text, ready to type. */
  SELECT_SEARCH: 'SELECT_SEARCH',

  /** Type the query with human-like keystrokes, submit, await results. */
  TYPE_SEARCH:   'TYPE_SEARCH',

  /** Scroll and hover on the current results page; scrape and save cards. */
  BROWSE_PAGE:   'BROWSE_PAGE',

  /** Perform a light in-page click/interaction before deeper navigation. */
  CLICK_ON_PAGE: 'CLICK_ON_PAGE',

  /** Navigate into a random product page, save its HTML, then go back. */
  VISIT_PRODUCT: 'VISIT_PRODUCT',

  /** Scroll to the bottom of the page before clicking next page. */
  SCROLL_TO_BOTTOM: 'SCROLL_TO_BOTTOM',

  /** Click the next-page button and wait for the new results page to load. */
  NEXT_PAGE:     'NEXT_PAGE',

  /** Take an unstructured idle pause with extra scrolling / hovering. */
  IDLE_WANDER:   'IDLE_WANDER',

  /** Hover over a specific card to simulate examining it. */
  HOVER_CARD:    'HOVER_CARD',

  /** Change sort order to simulate exploring different result views. */
  SORT_CHANGE:   'SORT_CHANGE',

  /** Click a filter checkbox to simulate refining results. */
  FILTER_CLICK:  'FILTER_CLICK',

  /** Scroll up and down to simulate reading and exploring. */
  SCROLL_EXPLORE: 'SCROLL_EXPLORE',

  /** Pause to simulate human reading/thinking time when viewing products. */
  PAUSE:          'PAUSE',

  /** Terminal state: query traversal is complete. */
  QUERY_DONE:    'QUERY_DONE',
});

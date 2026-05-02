import { States } from '../states.js';
import { randomBetween } from '../../statelessfunctions/timing.js';

export async function stateStartQuery(ctx) {
  const { query, searchedSet, csvLogger, persist } = ctx;

  if (searchedSet.has(query)) {
    console.log(`Already searched: "${query}", skipping`);
    return States.QUERY_DONE;
  }

  console.log(`\nSearching: "${query}"`);
  csvLogger.logQuery(query, persist);
  searchedSet.add(query);

  // Reset per-query counters
  ctx.maxPages       = randomBetween(3, 11);
  ctx.currentPageNum = 0;
  ctx.cardsScraped   = 0;
  ctx.cardsInteracted = 0;
  ctx.newSuggestions = [];
  ctx.links          = [];
  ctx.searchSelector = null;
  ctx.readyForNextPage = false;
  ctx.lastScrapedResultsUrl = null;
  ctx.pageVisitCount = 0;
  ctx.pageEnteredAt  = null;
  ctx.pageBudgetMs   = null;

  // Always try searching from the current page first.
  // If no search bar is available, SCROLL_TO_TOP/TYPE_SEARCH will reroute as needed.
  return States.SCROLL_TO_TOP;
}

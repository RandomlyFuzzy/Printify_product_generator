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

  // First query goes to homepage, subsequent ones scroll to top then type search
  return ctx.skipEnsureHome ? States.SCROLL_TO_TOP : States.ENSURE_HOME;
}

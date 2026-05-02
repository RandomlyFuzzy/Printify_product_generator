import { States } from '../states.js';
import { TRANSITIONS, pickNextState } from '../transitions.js';
import { humanScroll } from '../../statelessfunctions/humanBehavior/humanScroll.js';
import { humanHover } from '../../statelessfunctions/humanBehavior/humanHover.js';
import { simulateMistake } from '../../statelessfunctions/humanBehavior/simulateMistake.js';
import { getCardLinks } from '../../statelessfunctions/humanBehavior/getCardLinks.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

export async function stateBrowsePage(ctx) {
  const { page, query, csvLogger } = ctx;
  const currentResultsUrl = page.url();
  const firstVisitToThisPage = ctx.lastScrapedResultsUrl !== currentResultsUrl;

  // Set a fresh time budget when we land on a new page
  if (firstVisitToThisPage) {
    ctx.pageEnteredAt  = Date.now();
    ctx.pageBudgetMs   = randomBetween(10_000, 30_000);
    ctx.pageVisitCount = 0;
  }

  const timeOnPage = Date.now() - (ctx.pageEnteredAt ?? Date.now());
  const budgetExceeded = timeOnPage >= (ctx.pageBudgetMs ?? 20_000);

  console.log(`\nPage ${ctx.currentPageNum + 1}/${ctx.maxPages}  (+${Math.round(timeOnPage / 1000)}s / ${Math.round((ctx.pageBudgetMs ?? 20_000) / 1000)}s budget)`);
  await delay(randomBetween(500, 1000));
  await humanScroll(page);
  await humanHover(page);
  await simulateMistake(page);

  let links = ctx.links ?? [];
  if (firstVisitToThisPage || links.length === 0) {
    links = await getCardLinks(page);
    console.log(`Found ${links.length} filtered product cards on page ${ctx.currentPageNum + 1}`);
    console.log(`Saving all ${links.length} cards on this page`);

    for (let i = 0; i < links.length; i++) {
      const cardInfo = links[i];
      console.log(`  [${i + 1}/${links.length}] Saving card...`);
      const savedId = csvLogger.saveCardData(cardInfo, query);
      if (savedId) ctx.cardsScraped++;
    }

    ctx.lastScrapedResultsUrl = currentResultsUrl;
  } else {
    console.log(`Revisiting current results page; reusing ${links.length} previously captured cards`);
  }

  ctx.links = links;
  ctx.cardsInteracted = (ctx.cardsInteracted || 0) + links.length;
  ctx.readyForNextPage = false;
  ctx.pageVisitCount = (ctx.pageVisitCount || 0) + 1;

  // Hard stop conditions
  if (ctx.currentPageNum >= ctx.maxPages || links.length === 0) {
    return States.QUERY_DONE;
  }

  // Force pagination when time budget is used up
  if (budgetExceeded) {
    console.log(`Page time budget exhausted (${Math.round(timeOnPage / 1000)}s), moving to next page`);
    return States.SCROLL_TO_BOTTOM;
  }

  return pickNextState(TRANSITIONS[States.BROWSE_PAGE]);
}

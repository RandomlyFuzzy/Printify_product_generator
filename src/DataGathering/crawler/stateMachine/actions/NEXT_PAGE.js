import { States } from '../states.js';
import { TRANSITIONS, pickNextState } from '../transitions.js';
import { humanScroll } from '../../statelessfunctions/humanBehavior/humanScroll.js';
import { simulateReading } from '../../statelessfunctions/humanBehavior/simulateReading.js';
import { goToNextPage } from '../../statelessfunctions/humanBehavior/goToNextPage.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

export async function stateNextPage(ctx) {
  const { page } = ctx;

  if (!ctx.readyForNextPage) {
    return States.SCROLL_TO_BOTTOM;
  }

  ctx.readyForNextPage = false;

  await humanScroll(page);
  await delay(randomBetween(500, 1000));

  const hasNext = await goToNextPage(page);
  if (!hasNext) {
    console.log('No more pages available');
    return States.QUERY_DONE;
  }

  ctx.currentPageNum++;
  ctx.lastScrapedResultsUrl = null;
  ctx.links = [];
  ctx.pageVisitCount = 0;
  ctx.pageEnteredAt  = null;
  ctx.pageBudgetMs   = null;

  await simulateReading(page);

  if (ctx.currentPageNum >= ctx.maxPages) return States.QUERY_DONE;

  return States.BROWSE_PAGE;
}

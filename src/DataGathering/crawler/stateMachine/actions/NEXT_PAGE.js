import { States } from '../states.js';
import { TRANSITIONS, pickNextState } from '../transitions.js';
import { humanScroll, simulateReading, goToNextPage } from '../../statelessfunctions/humanBehavior.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

export async function stateNextPage(ctx) {
  const { page } = ctx;

  await humanScroll(page);
  await delay(randomBetween(500, 1000));

  const hasNext = await goToNextPage(page);
  if (!hasNext) {
    console.log('No more pages available');
    return States.QUERY_DONE;
  }

  await simulateReading(page);

  if (ctx.currentPageNum >= ctx.maxPages) return States.QUERY_DONE;

  return States.BROWSE_PAGE;
}

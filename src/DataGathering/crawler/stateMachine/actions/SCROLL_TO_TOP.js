import { States } from '../states.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

export async function stateScrollToTop(ctx) {
  const { page } = ctx;

  await page.evaluate(() => window.scrollTo(0, 0));
  await delay(randomBetween(300, 800));

  return States.TYPE_SEARCH;
}

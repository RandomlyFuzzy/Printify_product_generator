import { States } from '../states.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

export async function stateScrollToBottom(ctx) {
  const { page } = ctx;

  await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
  await delay(randomBetween(300, 800));

  return States.NEXT_PAGE;
}

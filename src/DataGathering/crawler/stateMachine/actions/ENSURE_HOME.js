import { States } from '../states.js';
import { findSearchbar, varyViewport } from '../../statelessfunctions/humanBehavior.js';
import { delay, randomBetween, randomDelay } from '../../statelessfunctions/timing.js';

export async function stateEnsureHome(ctx) {
  const { page } = ctx;

  const currentUrl = page.url();
  const isHome = currentUrl === 'https://www.ebay.com/' ||
                 currentUrl === 'https://www.ebay.com';

  if (!isHome) {
    await page.evaluate(() => window.scrollTo(0, 0));
    await delay(randomBetween(500, 1000));
  }

  let searchSelector = await findSearchbar(page);

  if (!searchSelector) {
    await page.goto('https://www.ebay.com', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await randomDelay();
    await varyViewport(page);
    searchSelector = await findSearchbar(page);
  }

  if (!searchSelector) {
    console.error('[StateMachine] Searchbar not found — aborting query');
    return States.QUERY_DONE;
  }

  ctx.searchSelector = searchSelector;
  return States.SCROLL_TO_TOP;
}

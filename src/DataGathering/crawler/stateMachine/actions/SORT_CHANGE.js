import { States } from '../states.js';
import { TRANSITIONS, pickNextState } from '../transitions.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

export async function stateSortChange(ctx) {
  const { page } = ctx;

  console.log('  [sort change — exploring different result orderings]');

  try {
    const sortChanged = await page.evaluate(() => {
      const sortSelectors = [
        'select.s-item__sort-select',
        'select[aria-label*="Sort"]',
        'select[name*="sort"]',
        '.srp-sort select',
      ];

      for (const selector of sortSelectors) {
        const select = document.querySelector(selector);
        if (select && select.options.length > 1) {
          const randomIndex = Math.floor(Math.random() * select.options.length);
          select.selectedIndex = randomIndex;
          select.dispatchEvent(new Event('change', { bubbles: true }));
          return true;
        }
      }
      return false;
    });

    if (sortChanged) {
      await delay(randomBetween(2000, 4000));
      await page.waitForSelector('.s-item, .srp-results li', { timeout: 10000 }).catch(() => {});
    } else {
      await delay(randomBetween(500, 1000));
    }
  } catch (_) {
    // Non-critical: continue state flow
  }

  if (ctx.currentPageNum >= ctx.maxPages) return States.QUERY_DONE;

  return pickNextState(TRANSITIONS[States.SORT_CHANGE]);
}

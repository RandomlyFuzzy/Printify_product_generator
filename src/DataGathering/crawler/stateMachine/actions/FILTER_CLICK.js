import { States } from '../states.js';
import { TRANSITIONS, pickNextState } from '../transitions.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

export async function stateFilterClick(ctx) {
  const { page } = ctx;

  console.log('  [filter click — refining search results]');

  try {
    const filterClicked = await page.evaluate(() => {
      const filterSelectors = [
        '.srp-filters input[type="checkbox"]',
        '.x-refine__sidebar input[type="checkbox"]',
        'input[aria-label*="checkbox"]',
        '.srp-filters button',
      ];

      const allFilters = [];
      for (const selector of filterSelectors) {
        allFilters.push(...Array.from(document.querySelectorAll(selector)));
      }

      if (allFilters.length === 0) return false;

      const randomFilter = allFilters[Math.floor(Math.random() * allFilters.length)];
      const rect = randomFilter.getBoundingClientRect();
      if (!rect || rect.width <= 1 || rect.height <= 1) return false;

      randomFilter.click();
      return true;
    });

    if (filterClicked) {
      await delay(randomBetween(1500, 3000));
      await page.waitForSelector('.s-item, .srp-results li', { timeout: 10000 }).catch(() => {});
    } else {
      await delay(randomBetween(500, 1000));
    }
  } catch (_) {
    // Non-critical: continue state flow
  }

  if (ctx.currentPageNum >= ctx.maxPages) return States.QUERY_DONE;

  return pickNextState(TRANSITIONS[States.FILTER_CLICK]);
}

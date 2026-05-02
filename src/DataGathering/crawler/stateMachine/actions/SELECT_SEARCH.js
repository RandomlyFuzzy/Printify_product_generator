import { States } from '../states.js';
import { findSearchbar } from '../../statelessfunctions/humanBehavior/findSearchbar.js';
import { humanClick } from '../../statelessfunctions/humanBehavior/humanClick.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

/**
 * Click the search input, select any existing text (Ctrl+A), and prepare for typing.
 * Falls back to ENSURE_HOME if the search bar cannot be located.
 */
export async function stateSelectSearch(ctx) {
  const { page } = ctx;

  // Re-discover selector if lost
  if (!ctx.searchSelector) {
    ctx.searchSelector = await findSearchbar(page);
  }
  if (!ctx.searchSelector) {
    return States.ENSURE_HOME;
  }

  try {
    // Human-like click to focus the input
    await humanClick(page, ctx.searchSelector);
    await delay(randomBetween(100, 300));

    // Select all existing text so typing replaces it cleanly
    await page.keyboard.down('Control');
    await page.keyboard.press('a');
    await page.keyboard.up('Control');
    await delay(randomBetween(80, 200));
  } catch (e) {
    // If click fails, try a direct focus as fallback
    try {
      await page.focus(ctx.searchSelector);
      await delay(randomBetween(100, 250));
    } catch (_) {
      ctx.searchSelector = null;
      return States.SCROLL_TO_TOP;
    }
  }

  return States.TYPE_SEARCH;
}

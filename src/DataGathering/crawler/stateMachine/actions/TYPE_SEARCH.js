import { States } from '../states.js';
import { humanType } from '../../statelessfunctions/humanBehavior/humanType.js';
import { simulateMistake } from '../../statelessfunctions/humanBehavior/simulateMistake.js';
import { detectCaptcha } from '../../statelessfunctions/humanBehavior/detectCaptcha.js';
import { simulateReading } from '../../statelessfunctions/humanBehavior/simulateReading.js';
import { idlePause } from '../../statelessfunctions/humanBehavior/idlePause.js';
import { findSearchbar } from '../../statelessfunctions/humanBehavior/findSearchbar.js';
import { randomDelay } from '../../statelessfunctions/timing.js';

export async function stateTypeSearch(ctx) {
  const { page, query, suggestionsSet, searchedSet, csvLogger, persist } = ctx;

  if (!ctx.searchSelector) {
    ctx.searchSelector = await findSearchbar(page);
  }

  if (!ctx.searchSelector) {
    console.warn('[StateMachine] Missing search selector in TYPE_SEARCH, retrying via SCROLL_TO_TOP');
    return States.SCROLL_TO_TOP;
  }

  // Wrap logSuggestion to match the signature humanType expects: (partial, suggestion)
  const logSuggestionBound = (partial, s) => csvLogger.logSuggestion(partial, s, persist);

  try {
    ctx.newSuggestions = await humanType(
      page,
      ctx.searchSelector,
      query,
      true,
      suggestionsSet,
      searchedSet,
      logSuggestionBound,
    );
  } catch (err) {
    const msg = err?.message || '';
    const isSelectorIssue = msg.includes('No element found for selector') ||
      msg.includes('Cannot find search input') ||
      msg.includes('Invalid search input selector');

    if (!isSelectorIssue) throw err;

    ctx.searchSelector = await findSearchbar(page);
    if (!ctx.searchSelector) {
      console.warn('[StateMachine] Search input unavailable after retry, returning to SCROLL_TO_TOP');
      return States.SCROLL_TO_TOP;
    }

    ctx.newSuggestions = await humanType(
      page,
      ctx.searchSelector,
      query,
      true,
      suggestionsSet,
      searchedSet,
      logSuggestionBound,
    );
  }

  await randomDelay();
  await simulateMistake(page);
  await page.keyboard.press('Enter');

  await page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 60000 });
  await detectCaptcha(page);
  await simulateReading(page);
  await idlePause(page);

  return States.BROWSE_PAGE;
}

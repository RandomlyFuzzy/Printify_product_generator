import { States } from '../states.js';
import { humanType, simulateMistake, detectCaptcha, simulateReading, idlePause } from '../../statelessfunctions/humanBehavior.js';
import { randomDelay } from '../../statelessfunctions/timing.js';

export async function stateTypeSearch(ctx) {
  const { page, query, suggestionsSet, searchedSet, csvLogger, persist } = ctx;

  // Wrap logSuggestion to match the signature humanType expects: (partial, suggestion)
  const logSuggestionBound = (partial, s) => csvLogger.logSuggestion(partial, s, persist);

  ctx.newSuggestions = await humanType(
    page,
    ctx.searchSelector,
    query,
    true,
    suggestionsSet,
    searchedSet,
    logSuggestionBound,
  );

  await randomDelay();
  await simulateMistake(page);
  await page.keyboard.press('Enter');

  await page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 60000 });
  await detectCaptcha(page);
  await simulateReading(page);
  await idlePause();

  return States.BROWSE_PAGE;
}

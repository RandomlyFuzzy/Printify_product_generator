import { States } from '../states.js';
import { TRANSITIONS, pickNextState } from '../transitions.js';
import { humanScroll, humanHover, simulateMistake, getCardLinks } from '../../statelessfunctions/humanBehavior.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

export async function stateBrowsePage(ctx) {
  const { page, query, csvLogger } = ctx;

  console.log(`\nPage ${ctx.currentPageNum + 1}/${ctx.maxPages}`);
  await delay(randomBetween(500, 1000));
  await humanScroll(page);
  await humanHover(page);
  await simulateMistake(page);

  const links = await getCardLinks(page);
  console.log(`Found ${links.length} filtered product cards on page ${ctx.currentPageNum + 1}`);
  console.log(`Saving all ${links.length} cards on this page`);

  for (let i = 0; i < links.length; i++) {
    const cardInfo = links[i];
    console.log(`  [${i + 1}/${links.length}] Saving card...`);
    const savedId = csvLogger.saveCardData(cardInfo, query);
    if (savedId) ctx.cardsScraped++;
  }

  ctx.links = links;
  ctx.currentPageNum++;
  ctx.cardsInteracted = (ctx.cardsInteracted || 0) + links.length;

  // Hard stop conditions
  if (ctx.currentPageNum >= ctx.maxPages || links.length === 0) {
    return States.QUERY_DONE;
  }

  // Ensure at least 1 card interaction before allowing NEXT_PAGE
  const candidates = TRANSITIONS[States.BROWSE_PAGE].filter(([state]) => {
    if (state === States.NEXT_PAGE && (ctx.cardsInteracted || 0) < 1) {
      return false;
    }
    return true;
  });

  return pickNextState(candidates.length > 0 ? candidates : TRANSITIONS[States.BROWSE_PAGE].filter(([s]) => s !== States.NEXT_PAGE));
}

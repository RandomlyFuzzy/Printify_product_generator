import { States } from '../states.js';
import { TRANSITIONS, pickNextState } from '../transitions.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

export async function stateHoverCard(ctx) {
  const { page } = ctx;

  console.log('  [hover card — simulating examination of a product]');

  try {
    const cardHandle = await page.evaluateHandle(() => {
      const cards = Array.from(document.querySelectorAll('.s-item, .srp-results li'));
      if (cards.length === 0) return null;
      const randomIndex = Math.floor(Math.random() * cards.length);
      return cards[randomIndex];
    });

    const element = cardHandle.asElement();
    if (!element) {
      await cardHandle.dispose();
      return pickNextState(TRANSITIONS[States.HOVER_CARD]);
    }

    const rect = await element.boundingBox();
    await cardHandle.dispose();
    if (!rect) return pickNextState(TRANSITIONS[States.HOVER_CARD]);

    const x = rect.x + randomBetween(10, Math.max(11, Math.floor(rect.width) - 10));
    const y = rect.y + randomBetween(10, Math.max(11, Math.floor(rect.height) - 10));

    await page.mouse.move(x, y, { steps: randomBetween(5, 15) });
    await delay(randomBetween(300, 1200));

    if (Math.random() > 0.6) {
      await page.mouse.wheel({ deltaY: randomBetween(50, 200) });
      await delay(randomBetween(100, 400));
    }

    if (Math.random() > 0.7) {
      await page.mouse.move(x + randomBetween(-20, 20), y + randomBetween(-20, 20), { steps: randomBetween(2, 5) });
      await delay(randomBetween(200, 600));
    }
  } catch (_) {
    // Non-critical: continue state flow
  }

  if (ctx.currentPageNum >= ctx.maxPages) return States.QUERY_DONE;

  return pickNextState(TRANSITIONS[States.HOVER_CARD]);
}

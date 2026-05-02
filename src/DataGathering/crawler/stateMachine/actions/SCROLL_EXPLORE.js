import { States } from '../states.js';
import { TRANSITIONS, pickNextState } from '../transitions.js';
import { humanHover } from '../../statelessfunctions/humanBehavior/humanHover.js';
import { simulateReading } from '../../statelessfunctions/humanBehavior/simulateReading.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

export async function stateScrollExplore(ctx) {
  const { page } = ctx;

  console.log('  [scroll explore — reading and exploring the page]');

  try {
    const scrollCount = randomBetween(2, 4);
    for (let i = 0; i < scrollCount; i++) {
      const direction = Math.random() > 0.5 ? 1 : -1;
      const amount = randomBetween(200, 800);
      await page.mouse.wheel({ deltaY: direction * amount });
      await delay(randomBetween(300, 800));

      if (Math.random() > 0.6) {
        await simulateReading(page);
        await delay(randomBetween(500, 1500));
      }
    }

    if (Math.random() > 0.5) {
      await humanHover(page);
      await delay(randomBetween(400, 1000));
    }
  } catch (_) {
    // Non-critical: continue state flow
  }

  if (ctx.currentPageNum >= ctx.maxPages) return States.QUERY_DONE;

  return pickNextState(TRANSITIONS[States.SCROLL_EXPLORE]);
}

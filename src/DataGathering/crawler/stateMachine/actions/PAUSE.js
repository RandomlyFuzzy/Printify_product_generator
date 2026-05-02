import { States } from '../states.js';
import { TRANSITIONS, pickNextState } from '../transitions.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

export async function statePause(ctx) {
  const { page } = ctx;

  console.log('  [pause — simulating human thinking/reading time]');

  try {
    const pauseType = randomBetween(1, 4);

    switch (pauseType) {
      case 1: // Short pause - just wait
        console.log('    Pause type: short contemplation');
        await delay(randomBetween(2000, 5000));
        break;

      case 2: // Medium pause with minor mouse movement
        console.log('    Pause type: reading with slight movement');
        await delay(randomBetween(3000, 7000));
        if (Math.random() > 0.5) {
          const viewport = page.viewport() ?? { width: 1280, height: 720 };
          const x = randomBetween(100, Math.max(120, viewport.width - 100));
          const y = randomBetween(100, Math.max(120, viewport.height - 100));
          await page.mouse.move(x, y, { steps: randomBetween(3, 8) });
          await delay(randomBetween(500, 1500));
        }
        break;

      case 3: // Long pause simulating deep reading
        console.log('    Pause type: deep reading/product examination');
        await delay(randomBetween(5000, 12000));
        if (Math.random() > 0.6) {
          await page.mouse.wheel({ deltaY: randomBetween(100, 300) });
          await delay(randomBetween(1000, 3000));
        }
        break;

      case 4: // Pause with hover on something
        console.log('    Pause type: hovering while thinking');
        await delay(randomBetween(1500, 4000));
        try {
          const cardHandle = await page.evaluateHandle(() => {
            const cards = Array.from(document.querySelectorAll('.s-item, .srp-results li'));
            if (cards.length === 0) return null;
            return cards[Math.floor(Math.random() * cards.length)];
          });

          const element = cardHandle.asElement();
          if (element) {
            const rect = await element.boundingBox();
            await cardHandle.dispose();
            if (rect) {
              const x = rect.x + randomBetween(10, Math.max(11, Math.floor(rect.width) - 10));
              const y = rect.y + randomBetween(10, Math.max(11, Math.floor(rect.height) - 10));
              await page.mouse.move(x, y, { steps: randomBetween(5, 15) });
              await delay(randomBetween(2000, 6000));
            }
          } else {
            await cardHandle.dispose();
          }
        } catch (_) {
          // Non-critical
        }
        break;
    }
  } catch (_) {
    // Non-critical: continue state flow
  }

  if (ctx.currentPageNum >= ctx.maxPages) return States.QUERY_DONE;

  return pickNextState(TRANSITIONS[States.PAUSE]);
}

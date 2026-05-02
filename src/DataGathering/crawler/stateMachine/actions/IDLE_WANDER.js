import { States } from '../states.js';
import { TRANSITIONS, pickNextState } from '../transitions.js';
import { humanScroll, humanHover, idlePause } from '../../statelessfunctions/humanBehavior.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

async function performMissScroll(page) {
  await page.mouse.wheel({ deltaY: randomBetween(80, 260) });
  await delay(randomBetween(50, 180));
  await page.mouse.wheel({ deltaY: randomBetween(-180, -60) });
}

async function performPageDown(page) {
  await page.keyboard.press('PageDown');
  await delay(randomBetween(120, 300));
}

async function hoverRandomFilteredCard(page) {
  const cardHandle = await page.evaluateHandle(() => {
    const cards = Array.from(document.querySelectorAll('.s-item, .srp-results li'))
      .filter((card) => card.querySelector('svg') !== null);
    if (cards.length === 0) return null;
    const randomIndex = Math.floor(Math.random() * cards.length);
    return cards[randomIndex];
  });

  const element = cardHandle.asElement();
  if (!element) {
    await cardHandle.dispose();
    return;
  }

  const rect = await element.boundingBox();
  await cardHandle.dispose();
  if (!rect) return;

  const x = rect.x + randomBetween(8, Math.max(9, Math.floor(rect.width) - 8));
  const y = rect.y + randomBetween(8, Math.max(9, Math.floor(rect.height) - 8));
  await page.mouse.move(x, y, { steps: randomBetween(4, 12) });
  await delay(randomBetween(250, 900));
}

export async function stateIdleWander(ctx) {
  const { page } = ctx;

  console.log('  [idle wander — taking an unstructured break]');
  await idlePause();

  const wanderActions = [
    { name: 'scroll', run: () => humanScroll(page) },
    { name: 'wait', run: () => delay(randomBetween(1200, 4500)) },
    { name: 'miss-scroll', run: () => performMissScroll(page) },
    { name: 'pgdown', run: () => performPageDown(page) },
    { name: 'hover-filtered-card', run: () => hoverRandomFilteredCard(page) },
    { name: 'generic-hover', run: () => humanHover(page) },
  ];

  const actionCount = randomBetween(2, 4);
  for (let i = 0; i < actionCount; i++) {
    const selected = wanderActions[randomBetween(0, wanderActions.length - 1)];
    console.log(`    [idle action] ${selected.name}`);
    try {
      await selected.run();
      await delay(randomBetween(120, 700));
    } catch (_) {
      // Keep wander resilient: a failed micro-action should not stop traversal.
    }
  }

  if (ctx.currentPageNum >= ctx.maxPages) return States.QUERY_DONE;

  const candidates = TRANSITIONS[States.IDLE_WANDER].filter(([state]) => {
    if (state === States.NEXT_PAGE && (ctx.cardsInteracted || 0) < 1) {
      return false;
    }
    return true;
  });

  return pickNextState(candidates.length > 0 ? candidates : TRANSITIONS[States.IDLE_WANDER].filter(([s]) => s !== States.NEXT_PAGE));
}

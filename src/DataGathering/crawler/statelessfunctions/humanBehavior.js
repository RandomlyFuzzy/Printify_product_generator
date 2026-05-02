import { delay, randomBetween, randomDelay } from './timing.js';

export async function humanMouseMove(page, selector, directMove = false) {
  try {
    const rect = await page.evaluate((sel) => {
      const el = document.querySelector(sel);
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return { x: r.x, y: r.y, width: r.width, height: r.height };
    }, selector);

    if (!rect) return;

    const targetX = rect.x + randomBetween(5, rect.width - 5);
    const targetY = rect.y + randomBetween(5, rect.height - 5);

    if (directMove) {
      await page.mouse.move(targetX, targetY, { steps: randomBetween(5, 15) });
    } else {
      const startX = randomBetween(0, 100);
      const startY = randomBetween(0, 100);
      await page.mouse.move(startX, startY);
      await delay(randomBetween(100, 300));

      const midPoints = randomBetween(2, 4);
      for (let i = 0; i < midPoints; i++) {
        const midX = randomBetween(Math.min(startX, targetX), Math.max(startX, targetX));
        const midY = randomBetween(Math.min(startY, targetY), Math.max(startY, targetY));
        await page.mouse.move(midX, midY, { steps: randomBetween(3, 8) });
        await delay(randomBetween(100, 300));
      }

      await page.mouse.move(targetX, targetY, { steps: randomBetween(5, 15) });
    }

    await randomDelay();
  } catch (e) {}
}

export async function humanHover(page) {
  try {
    const elements = await page.$$('a, button, .s-item, .srp-results li');
    if (elements.length > 0) {
      const randIdx = randomBetween(0, Math.min(10, elements.length - 1));
      const element = elements[randIdx];

      const rect = await element.boundingBox();
      if (rect) {
        const x = rect.x + randomBetween(5, rect.width - 5);
        const y = rect.y + randomBetween(5, rect.height - 5);
        await page.mouse.move(x, y, { steps: randomBetween(3, 8) });
        await delay(randomBetween(300, 800));
      }
    }
  } catch (e) {}
}

export async function humanClick(page, selector, directMove = false) {
  try {
    await humanMouseMove(page, selector, directMove);

    const rect = await page.evaluate((sel) => {
      const el = document.querySelector(sel);
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return { x: r.x, y: r.y, width: r.width, height: r.height };
    }, selector);

    if (rect) {
      const x = rect.x + randomBetween(5, rect.width - 5);
      const y = rect.y + randomBetween(5, rect.height - 5);

      await page.mouse.move(x, y);
      await delay(randomBetween(50, 150));
      await page.mouse.down();
      await delay(randomBetween(50, 150));
      await page.mouse.up();
      await randomDelay();
    }
  } catch (e) {}
}

export async function simulateReading(page) {
  const readTime = randomBetween(2000, 5000);
  const steps = randomBetween(2, 4);
  const stepTime = readTime / steps;

  for (let i = 0; i < steps; i++) {
    await delay(stepTime);
    if (Math.random() > 0.5) {
      const scrollAmount = randomBetween(50, 200);
      await page.evaluate((amt) => {
        window.scrollBy({ top: amt, behavior: 'smooth' });
      }, scrollAmount);
    }
  }
}

export async function simulateMistake(page) {
  if (Math.random() > 0.85) {
    try {
      const elements = await page.$$('a, button');
      if (elements.length > 0) {
        const randomEl = elements[randomBetween(0, Math.min(5, elements.length - 1))];
        await randomEl.click().catch(() => {});
        await delay(randomBetween(500, 1500));
        await page.goBack({ waitUntil: 'domcontentloaded' }).catch(() => {});
        await delay(randomBetween(1000, 2000));
      }
    } catch (e) {}
  }
}

export async function varyViewport(page) {
  if (Math.random() > 0.9) {
    try {
      const width = 1920 + randomBetween(-50, 50);
      const height = 1080 + randomBetween(-30, 30);
      await page.setViewport({ width, height });
      await delay(randomBetween(500, 1000));
    } catch (e) {}
  }
}

export async function idlePause() {
  if (Math.random() > 0.8) {
    await delay(randomBetween(5000, 15000));
  }
}

export async function detectCaptcha(page) {
  try {
    const captchaDetected = await page.evaluate(() => {
      const pageText = document.body.innerText.toLowerCase();
      const hasCaptchaText = pageText.includes('captcha') ||
        pageText.includes('robot') ||
        pageText.includes('verify you are human') ||
        pageText.includes('automated requests');

      const hasCaptchaElement = document.querySelector('iframe[src*="captcha"], iframe[src*="recaptcha"], [class*="captcha"], [id*="captcha"], [class*="hcaptcha"]') !== null;

      return hasCaptchaText || hasCaptchaElement;
    });

    if (captchaDetected) {
      console.log('\n!!! CAPTCHA DETECTED !!!');
      console.log('Waiting 15 minutes before continuing...');
      await delay(15 * 60 * 1000);
      console.log('Resuming after CAPTCHA wait...\n');
      return true;
    }
    return false;
  } catch (e) {
    return false;
  }
}

export async function findSearchbar(page) {
  const selectors = [
    '#gh-ac',
    '#srp-gh-ac',
    'input[type="search"]',
    'input[placeholder*="Search"]',
    '.gh-search-input',
    '[data-testid="search-input"]',
    'input[name="_nkw"]',
    '.yth-searchbox-input'
  ];

  for (const sel of selectors) {
    try {
      await page.waitForSelector(sel, { timeout: 3000, visible: true });
      return sel;
    } catch (e) {}
  }
  return null;
}

export async function humanType(page, selector, text, captureSuggestions, suggestionsSet, searchedSet, logSuggestion) {
  await humanClick(page, selector, true);
  await page.evaluate((sel) => { document.querySelector(sel).value = ''; }, selector);
  await delay(randomBetween(300, 800));

  const newSuggestions = [];

  for (let i = 0; i < text.length; i++) {
    const char = text[i];
    await page.type(selector, char, { delay: randomBetween(80, 200) });
    await delay(randomBetween(200, 500));

    if (Math.random() > 0.9) {
      await page.keyboard.press('Backspace');
      await delay(randomBetween(200, 400));
      await page.type(selector, char, { delay: randomBetween(80, 200) });
    }

    if (captureSuggestions && i >= Math.floor(text.length / 2)) {
      try {
        const suggestions = await page.evaluate(() => {
          const items = document.querySelectorAll('.ui-autocomplete-category, [role="option"], .gh-sch-ajax__item');
          return [...items].map((el) => el.innerText?.trim()).filter((t) => t && t.length > 2 && !t.toLowerCase().includes('search'));
        });

        const partial = text.substring(0, i + 1).toLowerCase();
        for (const s of suggestions) {
          if (!suggestionsSet.has(s)) {
            suggestionsSet.add(s);
            logSuggestion(partial, s);
            if (!searchedSet.has(s.toLowerCase())) {
              console.log(`  + New suggestion: "${s.toLowerCase()}"`);
              newSuggestions.push(s.toLowerCase());
            }
          }
        }
      } catch (e) {}
    }
  }
  return newSuggestions;
}

export async function humanScroll(page) {
  const scrolls = randomBetween(2, 5);
  const scrollAmount = 200;
  for (let i = 0; i < scrolls; i++) {
    await page.mouse.wheel({ deltaY: scrollAmount });
    await delay(randomBetween(20, 50));
  }
  await randomDelay();
}

/// Extracts product links from the current page, filtering out non-product cards by checking for SVG presence.
/// IMPORTANT: eBay injects fake/placeholder cards (ads, banners, separators) that do NOT contain SVG elements.
/// The input is a Puppeteer page instance
/// this function should return an array of objects with { url, card_innerHTML } for each detected product card.
export async function getCardLinks(page) {
  try {
    return await page.evaluate(() => {
      const cardsInfo = [];
      const seenUrls = new Set();
      const cardSelectors = ['.s-item', '.srp-results li', '[data-viewport-container] > div', '.s-item__wrapper'];

      const cards = new Set();
      cardSelectors.forEach((sel) => {
        document.querySelectorAll(sel).forEach((el) => cards.add(el));
      });

      cards.forEach((card) => {
        // IMPORTANT: SVG check is required - real eBay product cards always contain an SVG element.
        // Cards without SVG are fake/placeholder elements injected by eBay (ads, banners, separators).
        // Do NOT remove this check or garbage non-product entries will be saved.
        const hasSvg = card.querySelector('svg') !== null;
        if (!hasSvg) return;

        const linkEl = card.querySelector('a[href*="/itm/"]');
        if (!linkEl || !linkEl.href) return;

        // Deduplicate: strip tracking params and collapse cards matched by multiple selectors
        let url;
        try {
          const u = new URL(linkEl.href);
          url = u.origin + u.pathname;
        } catch (_) {
          url = linkEl.href;
        }
        if (seenUrls.has(url)) return;
        seenUrls.add(url);

        cardsInfo.push({
          url,
          card_innerHTML: card.innerHTML
        });
      });

      return cardsInfo;
    });
  } catch (e) {
    return [];
  }
}

export async function goToNextPage(page) {
  try {
    const nextBtn = await page.$('a.pagination__next, a[aria-label*="Next"], a[rel="next"]');
    if (nextBtn) {
      const rect = await nextBtn.boundingBox();
      if (rect) {
        await page.mouse.move(rect.x + rect.width / 2, rect.y + rect.height / 2, { steps: randomBetween(5, 15) });
        await delay(randomBetween(50, 150));
        await page.mouse.down();
        await delay(randomBetween(50, 150));
        await page.mouse.up();
        await delay(randomBetween(3000, 5000));
        return true;
      }
    }
  } catch (e) {}
  return false;
}

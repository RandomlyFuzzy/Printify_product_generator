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

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

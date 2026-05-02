import { delay, randomBetween, scaleDuration } from '../timing.js';
import { logError } from '../../persistence/logger.js';
import { humanClick } from './humanClick.js';
import { findSearchbar } from './findSearchbar.js';

export async function humanType(page, selector, text, captureSuggestions, suggestionsSet, searchedSet, logSuggestion) {
  if (!selector || typeof selector !== 'string') {
    logError('Search input selector is missing or invalid', null, {
      file: 'statelessfunctions/humanBehavior/humanType.js',
      function: 'humanType',
      selector,
      query: text,
      selectorType: typeof selector
    });
    throw new Error(`Invalid search input selector: ${selector}`);
  }

  const resolveSelector = async (preferredSelector) => {
    if (preferredSelector) {
      const preferredExists = await page.$(preferredSelector);
      if (preferredExists) return preferredSelector;
    }
    return await findSearchbar(page);
  };

  let activeSelector = await resolveSelector(selector);
  if (!activeSelector) {
    logError('Search input element not found before typing', null, {
      file: 'statelessfunctions/humanBehavior/humanType.js',
      function: 'humanType',
      selector,
      query: text
    });
    throw new Error(`Cannot find search input: ${selector}`);
  }

  await humanClick(page, activeSelector, true);

  const elementExists = await page.evaluate((sel) => {
    const el = document.querySelector(sel);
    if (el) {
      el.value = '';
      return true;
    }
    return false;
  }, activeSelector);

  if (!elementExists) {
    logError('Search input element not found', null, {
      file: 'statelessfunctions/humanBehavior/humanType.js',
      function: 'humanType',
      selector: activeSelector,
      query: text
    });
    throw new Error(`Cannot find search input: ${activeSelector}`);
  }

  await delay(randomBetween(300, 800));

  const newSuggestions = [];

  // Adjacent keys on a QWERTY layout for fat-finger simulation
  const ADJACENT = {
    a:'sqwz', b:'vghn', c:'xdfv', d:'serfcx', e:'wsdr', f:'drtgvc', g:'ftyhbv',
    h:'gyujbn', i:'ujko', j:'huikmn', k:'jiolm', l:'kop', m:'njk', n:'bhjm',
    o:'iklp', p:'ol', q:'wa', r:'edft', s:'aqwedxz', t:'rfgy', u:'yhji',
    v:'cfgb', w:'qase', x:'zsdc', y:'tghu', z:'asx',
  };

  const fatFingerMistake = async (char) => {
    const adj = ADJACENT[char.toLowerCase()];
    if (!adj) return;
    const wrong = adj[Math.floor(Math.random() * adj.length)];
    await page.type(activeSelector, wrong, { delay: scaleDuration(randomBetween(60, 130)) });
    await delay(randomBetween(120, 350));   // pause — "wait, that's wrong"
    await page.keyboard.press('Backspace');
    await delay(randomBetween(80, 200));
  };

  const typeCharWithRetry = async (char) => {
    for (let attempt = 0; attempt < 2; attempt++) {
      try {
        const exists = await page.$(activeSelector);
        if (!exists) {
          throw new Error(`No element found for selector: ${activeSelector}`);
        }
        await page.type(activeSelector, char, { delay: scaleDuration(randomBetween(80, 200)) });
        return;
      } catch (err) {
        if (attempt === 1) throw err;

        const recoveredSelector = await resolveSelector(activeSelector);
        if (!recoveredSelector) throw err;

        activeSelector = recoveredSelector;
        await humanClick(page, activeSelector, true);
        await delay(randomBetween(100, 250));
      }
    }
  };

  for (let i = 0; i < text.length; i++) {
    const char = text[i];

    // ~12% chance: type a wrong adjacent key first, then correct
    if (Math.random() < 0.12) {
      await fatFingerMistake(char);
    }

    await typeCharWithRetry(char);
    await delay(randomBetween(50, 200));

    // ~5% chance: pause as if distracted mid-word
    if (Math.random() < 0.05) {
      await delay(randomBetween(800, 2500));
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

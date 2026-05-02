import { delay } from '../timing.js';

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

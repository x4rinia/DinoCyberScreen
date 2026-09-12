const puppeteer = require('puppeteer');
const path = require('path');

(async () => {
  const browser = await puppeteer.launch();
  const page = await browser.newPage();
  await page.setViewport({ width: 1920, height: 1080 });
  const fileUrl = 'file:///' + path.resolve(__dirname, 'src/DinoCyberScreen/Web/index.html').replace(/\\/g, '/');
  console.log('Loading:', fileUrl);
  await page.goto(fileUrl, { waitUntil: 'networkidle0' });
  await new Promise(resolve => setTimeout(resolve, 3000)); // wait for animations/pulse
  await page.screenshot({ path: 'docs/rainbow-screen.png' });
  await browser.close();
  console.log('Screenshot saved to docs/rainbow-screen.png');
})();

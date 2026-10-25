// DinoCyberScreen - Cosmic Starfield Module with Planets & Comets
// Responsive 3D starfield with color coordination (Rainbow or Theme)

const PLANET_PALETTES = [
  // 1. Cyber Cyan & Oceanic Abyss
  { name: 'cyan', primary: '#00f0ff', secondary: '#005f73', ring: '#00f0ff', glow: '#00f0ff', bands: ['#00f0ff', '#0a9396'] },
  // 2. Cosmic Magenta & Deep Violet
  { name: 'magenta', primary: '#ff007f', secondary: '#7209b7', ring: '#ff70ec', glow: '#ff007f', bands: ['#ff007f', '#b5179e'] },
  // 3. Solar Gold & Molten Amber
  { name: 'gold', primary: '#ffb703', secondary: '#c1121f', ring: '#ffd166', glow: '#ffb703', bands: ['#ffb703', '#fb8500'] },
  // 4. Emerald & Matrix Mint
  { name: 'emerald', primary: '#2ec4b6', secondary: '#004b49', ring: '#80ffdb', glow: '#2ec4b6', bands: ['#2ec4b6', '#00f5d4'] },
  // 5. Crimson Mars & Flame Red
  { name: 'crimson', primary: '#ff3366', secondary: '#800020', ring: '#ff758f', glow: '#ff3366', bands: ['#ff3366', '#ff5400'] },
  // 6. Electric Cobalt & Ultraviolet
  { name: 'cobalt', primary: '#4361ee', secondary: '#3a0ca3', ring: '#4cc9f0', glow: '#4361ee', bands: ['#4361ee', '#4895ef'] },
  // 7. Toxic Lime & Alien Moss
  { name: 'lime', primary: '#9ef01a', secondary: '#005f00', ring: '#ccff33', glow: '#9ef01a', bands: ['#9ef01a', '#70e000'] },
  // 8. Celestial Silver & Pale Orchid
  { name: 'silver', primary: '#e0aaff', secondary: '#3c096c', ring: '#c77dff', glow: '#e0aaff', bands: ['#e0aaff', '#9d4edd'] },
  // 9. Prismatic Rainbow (special)
  { name: 'rainbow', primary: '#ff4fd8', secondary: '#29bfff', ring: '#dfff3f', glow: '#26f7c7', isPrismatic: true, bands: ['#ff4fd8', '#29bfff', '#dfff3f'] }
];

export class Starfield {
  constructor(canvas, options = {}) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d');
    this.screenIndex = options.screenIndex || 0;
    this.isMirrored = !!options.isMirrored;
    this.stars = [];
    this.comets = [];
    this.planet = null;
    this.nebulae = [];
    this.width = 0;
    this.height = 0;
    this.cx = 0;
    this.cy = 0;
    this.fov = 350;
    this.numStars = 550;
    this.baseSpeed = 1.4;

    // Asynchronous desynchronized timers per monitor
    const screenOffsetMs = this.screenIndex * 14000;
    this.nextCometAt = performance.now() + 2000 + (this.screenIndex * 3500) + Math.random() * 4000;

    // Never spawn planet simultaneously on multiple screens!
    if (this.screenIndex === 0) {
      this.spawnPlanet(true);
      this.nextPlanetAt = performance.now() + 26000 + Math.random() * 18000;
    } else {
      this.planet = null;
      // Stagger subsequent screens so planets appear at completely different times
      this.nextPlanetAt = performance.now() + 15000 + screenOffsetMs + Math.random() * 15000;
    }

    this.animId = null;

    this.resize = this.resize.bind(this);
    this.render = this.render.bind(this);

    window.addEventListener('resize', this.resize);
    this.resize();
    this.initStars();
    this.initNebulae();
    this.start();
  }

  resize() {
    this.width = this.canvas.width = window.innerWidth || 1920;
    this.height = this.canvas.height = window.innerHeight || 1080;
    this.cx = this.width / 2;
    this.cy = this.height / 2;
    this.fov = Math.min(this.width, this.height) * 0.45;
  }

  getThemeColors() {
    const app = document.querySelector('#app') || document.body;
    const theme = app.dataset.theme || 'rainbow';
    const isRainbow = theme === 'rainbow';

    const rainbowPalette = [
      '#29bfff', '#ff4fd8', '#dfff3f', '#26f7c7',
      '#8b5cff', '#ff9f1c', '#ff3864', '#ffffff'
    ];

    const themePalettes = {
      blue: ['#26d9ff', '#00aaff', '#70caff', '#d9f5ff', '#ffffff', '#66b5cb'],
      green: ['#4dff88', '#c4ff33', '#dbff66', '#e0ffeb', '#ffffff', '#70cf92'],
      red: ['#ff3b5c', '#ff8533', '#ffb333', '#ffe5ea', '#ffffff', '#ff5368'],
      white: ['#ffffff', '#eef5f8', '#ddeaf0', '#b8c8d0', '#e2edf2', '#ffffff'],
      pink: ['#ff66cc', '#cc44ff', '#dd88ff', '#ffe6f9', '#ffffff', '#ff4fd8']
    };

    const palette = isRainbow ? rainbowPalette : (themePalettes[theme] || themePalettes.blue);
    const primary = isRainbow ? '#29bfff' : palette[0];
    const secondary = isRainbow ? '#ff4fd8' : (palette[1] || palette[0]);

    return { isRainbow, palette, primary, secondary };
  }

  initStars() {
    this.stars = [];
    const { palette } = this.getThemeColors();
    for (let i = 0; i < this.numStars; i++) {
      this.stars.push(this.createStar(palette, Math.random() * 1400));
    }
  }

  createStar(palette, z = 1400) {
    const angle = Math.random() * Math.PI * 2;
    const distance = Math.pow(Math.random(), 0.7) * Math.max(this.width, this.height) * 0.85;
    return {
      x: Math.cos(angle) * distance,
      y: Math.sin(angle) * distance,
      z: z,
      prevZ: z,
      color: palette[Math.floor(Math.random() * palette.length)],
      size: Math.random() * 1.5 + 0.6,
      twinkle: Math.random() * Math.PI * 2,
      twinkleSpeed: 0.03 + Math.random() * 0.05
    };
  }

  initNebulae() {
    this.nebulae = [
      { x: this.width * 0.25, y: this.height * 0.35, r: 350, phase: 0 },
      { x: this.width * 0.75, y: this.height * 0.65, r: 420, phase: Math.PI },
      { x: this.width * 0.5, y: this.height * 0.2, r: 280, phase: Math.PI / 2 }
    ];
  }

  spawnComet() {
    const { isRainbow, palette, primary, secondary } = this.getThemeColors();
    const fromLeft = Math.random() > 0.3;
    const startX = fromLeft ? Math.random() * (this.width * 0.6) : this.width + 50;
    const startY = Math.random() * (this.height * 0.45);
    const angle = fromLeft
      ? (Math.PI * 0.15) + (Math.random() * 0.25)
      : (Math.PI * 0.75) + (Math.random() * 0.25);
    const speed = 14 + Math.random() * 10;

    this.comets.push({
      x: startX,
      y: startY,
      vx: Math.cos(angle) * speed,
      vy: Math.sin(angle) * speed,
      length: 140 + Math.random() * 120,
      radius: 3.5 + Math.random() * 2.5,
      life: 0,
      maxLife: 90 + Math.random() * 60,
      isRainbow,
      color: primary,
      tailColor: secondary,
      sparks: []
    });
  }

  spawnPlanet(initial = false) {
    const { isRainbow, palette: themePalette, primary: themePrimary, secondary: themeSecondary } = this.getThemeColors();
    const types = ['ringed', 'crater', 'tech', 'gas'];
    const type = types[Math.floor(Math.random() * types.length)];
    const radius = 55 + Math.random() * 45;

    // Pick a diverse, vibrant planet color palette
    let planetPalette;
    if (isRainbow) {
      planetPalette = PLANET_PALETTES[Math.floor(Math.random() * PLANET_PALETTES.length)];
    } else {
      if (Math.random() < 0.4) {
        planetPalette = {
          name: 'theme',
          primary: themePrimary,
          secondary: themeSecondary,
          ring: themePrimary,
          glow: themePrimary,
          bands: [themePrimary, themeSecondary]
        };
      } else {
        planetPalette = PLANET_PALETTES[Math.floor(Math.random() * (PLANET_PALETTES.length - 1))];
      }
    }

    // Place planet across top-half or mid background
    const startX = initial ? this.width * (0.65 + Math.random() * 0.2) : -radius * 2;
    const startY = this.height * (0.15 + Math.random() * 0.35);

    this.planet = {
      x: startX,
      y: startY,
      vx: 0.12 + Math.random() * 0.18,
      vy: 0.02 * (Math.random() - 0.5),
      radius,
      type,
      palette: planetPalette,
      isRainbow,
      rotation: Math.random() * Math.PI * 2,
      ringTilt: -0.35 + Math.random() * 0.2
    };
  }

  update(now) {
    const { palette, primary, secondary, isRainbow } = this.getThemeColors();

    // 1. Update stars
    for (let i = 0; i < this.stars.length; i++) {
      const s = this.stars[i];
      s.prevZ = s.z;
      s.z -= this.baseSpeed;
      s.twinkle += s.twinkleSpeed;

      if (s.z <= 1) {
        Object.assign(s, this.createStar(palette, 1400));
        s.prevZ = s.z;
      }
    }

    // 2. Update comets
    if (now >= this.nextCometAt) {
      this.spawnComet();
      this.nextCometAt = now + 6000 + Math.random() * 9000;
    }

    for (let i = this.comets.length - 1; i >= 0; i--) {
      const c = this.comets[i];
      c.x += c.vx;
      c.y += c.vy;
      c.life++;

      // Emit trailing spark particles
      if (c.life % 2 === 0) {
        c.sparks.push({
          x: c.x - c.vx * 0.5 + (Math.random() - 0.5) * 6,
          y: c.y - c.vy * 0.5 + (Math.random() - 0.5) * 6,
          vx: (Math.random() - 0.5) * 1.5,
          vy: (Math.random() - 0.5) * 1.5,
          alpha: 1,
          size: Math.random() * 2 + 1,
          color: isRainbow ? palette[Math.floor(Math.random() * palette.length)] : c.tailColor
        });
      }

      // Update sparks
      for (let s = c.sparks.length - 1; s >= 0; s--) {
        const sp = c.sparks[s];
        sp.x += sp.vx;
        sp.y += sp.vy;
        sp.alpha -= 0.04;
        if (sp.alpha <= 0) c.sparks.splice(s, 1);
      }

      // Remove comet if past screen or expired
      if (c.x < -200 || c.x > this.width + 300 || c.y > this.height + 200 || c.life >= c.maxLife) {
        this.comets.splice(i, 1);
      }
    }

    // 3. Update Planet
    if (this.planet) {
      this.planet.x += this.planet.vx;
      this.planet.y += this.planet.vy;
      this.planet.rotation += 0.0015;

      if (this.planet.x - this.planet.radius * 2 > this.width + 100) {
        this.planet = null;
        this.nextPlanetAt = now + 15000 + Math.random() * 25000;
      }
    } else if (now >= this.nextPlanetAt) {
      this.spawnPlanet(false);
    }
  }

  drawNebulae(ctx) {
    const { primary, secondary } = this.getThemeColors();
    const t = performance.now() * 0.0004;

    for (let i = 0; i < this.nebulae.length; i++) {
      const n = this.nebulae[i];
      const pulse = 1 + Math.sin(t + n.phase) * 0.15;
      const grad = ctx.createRadialGradient(
        n.x, n.y, 0,
        n.x, n.y, n.r * pulse
      );
      const col = i % 2 === 0 ? primary : secondary;
      grad.addColorStop(0, hexToRgba(col, 0.065));
      grad.addColorStop(0.5, hexToRgba(col, 0.025));
      grad.addColorStop(1, 'transparent');

      ctx.fillStyle = grad;
      ctx.beginPath();
      ctx.arc(n.x, n.y, n.r * pulse, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  drawPlanet(ctx, p) {
    if (!p) return;
    const { x, y, radius, type, palette, isRainbow, rotation, ringTilt } = p;
    const primaryColor = palette?.primary || '#00f0ff';
    const secondaryColor = palette?.secondary || '#005f73';
    const ringColor = palette?.ring || primaryColor;
    const glowColor = palette?.glow || primaryColor;
    const bands = palette?.bands || [primaryColor, secondaryColor];

    ctx.save();
    ctx.translate(x, y);

    // Planet atmosphere glow
    const atmoGrad = ctx.createRadialGradient(0, 0, radius * 0.85, 0, 0, radius * 1.3);
    atmoGrad.addColorStop(0, hexToRgba(glowColor, 0.35));
    atmoGrad.addColorStop(0.5, hexToRgba(secondaryColor, 0.15));
    atmoGrad.addColorStop(1, 'transparent');
    ctx.fillStyle = atmoGrad;
    ctx.beginPath();
    ctx.arc(0, 0, radius * 1.3, 0, Math.PI * 2);
    ctx.fill();

    // Back half of rings (drawn behind planet)
    if (type === 'ringed') {
      ctx.save();
      ctx.rotate(ringTilt);
      ctx.scale(1, 0.28);
      ctx.beginPath();
      ctx.arc(0, 0, radius * 2.1, Math.PI, Math.PI * 2);
      ctx.lineWidth = radius * 0.32;
      ctx.strokeStyle = palette?.isPrismatic
        ? 'rgba(255, 79, 216, 0.55)'
        : hexToRgba(ringColor, 0.45);
      ctx.stroke();
      ctx.restore();
    }

    // Planet body
    ctx.save();
    ctx.beginPath();
    ctx.arc(0, 0, radius, 0, Math.PI * 2);
    ctx.clip();

    // Base sphere lighting (spherical shader effect)
    const sphereGrad = ctx.createRadialGradient(
      -radius * 0.35, -radius * 0.35, radius * 0.1,
      0, 0, radius * 1.05
    );
    sphereGrad.addColorStop(0, '#ffffff');
    sphereGrad.addColorStop(0.3, primaryColor);
    sphereGrad.addColorStop(0.7, secondaryColor);
    sphereGrad.addColorStop(1, '#020508');
    ctx.fillStyle = sphereGrad;
    ctx.fill();

    // Surface details based on type
    if (type === 'gas' || type === 'ringed') {
      // Atmospheric bands
      ctx.rotate(rotation);
      ctx.lineWidth = radius * 0.08;
      for (let b = -radius * 0.8; b < radius * 0.8; b += radius * 0.22) {
        const bandCol = (b > 0) ? bands[0] : (bands[1] || bands[0]);
        ctx.strokeStyle = hexToRgba(bandCol, 0.28);
        ctx.beginPath();
        ctx.moveTo(-radius, b);
        ctx.lineTo(radius, b);
        ctx.stroke();
      }
    } else if (type === 'tech') {
      // Sci-Fi Cyber Grid Lines
      ctx.rotate(rotation);
      ctx.strokeStyle = hexToRgba(primaryColor, 0.45);
      ctx.lineWidth = 1.2;
      for (let g = -radius; g < radius; g += 16) {
        ctx.beginPath();
        ctx.arc(0, 0, Math.abs(g), 0, Math.PI * 2);
        ctx.stroke();
      }
    } else if (type === 'crater') {
      // Lunar craters
      ctx.rotate(rotation);
      const craters = [
        { cx: -radius * 0.2, cy: -radius * 0.1, r: radius * 0.18 },
        { cx: radius * 0.3, cy: radius * 0.25, r: radius * 0.22 },
        { cx: -radius * 0.1, cy: radius * 0.4, r: radius * 0.12 }
      ];
      for (const cr of craters) {
        ctx.beginPath();
        ctx.arc(cr.cx, cr.cy, cr.r, 0, Math.PI * 2);
        ctx.fillStyle = 'rgba(0, 0, 0, 0.3)';
        ctx.fill();
        ctx.strokeStyle = hexToRgba(primaryColor, 0.4);
        ctx.lineWidth = 1.4;
        ctx.stroke();
      }
    }

    // Shadow terminator (night side)
    const shadowGrad = ctx.createLinearGradient(-radius, -radius, radius * 0.8, radius * 0.8);
    shadowGrad.addColorStop(0, 'rgba(0, 0, 0, 0)');
    shadowGrad.addColorStop(0.55, 'rgba(0, 0, 0, 0.4)');
    shadowGrad.addColorStop(1, 'rgba(0, 0, 0, 0.94)');
    ctx.fillStyle = shadowGrad;
    ctx.fillRect(-radius, -radius, radius * 2, radius * 2);

    ctx.restore(); // end planet clip

    // Front half of rings (drawn in front of planet)
    if (type === 'ringed') {
      ctx.save();
      ctx.rotate(ringTilt);
      ctx.scale(1, 0.28);
      ctx.beginPath();
      ctx.arc(0, 0, radius * 2.1, 0, Math.PI);
      ctx.lineWidth = radius * 0.32;
      ctx.strokeStyle = palette?.isPrismatic
        ? 'rgba(41, 191, 255, 0.75)'
        : hexToRgba(ringColor, 0.65);
      ctx.stroke();
      ctx.restore();
    }

    ctx.restore();
  }

  drawComets(ctx) {
    for (const c of this.comets) {
      // Draw tail gradient
      const tailX = c.x - (c.vx / Math.hypot(c.vx, c.vy)) * c.length;
      const tailY = c.y - (c.vy / Math.hypot(c.vx, c.vy)) * c.length;

      const grad = ctx.createLinearGradient(tailX, tailY, c.x, c.y);
      grad.addColorStop(0, 'transparent');
      if (c.isRainbow) {
        grad.addColorStop(0.3, '#ff4fd8');
        grad.addColorStop(0.6, '#29bfff');
        grad.addColorStop(0.85, '#dfff3f');
      } else {
        grad.addColorStop(0.4, hexToRgba(c.tailColor, 0.25));
        grad.addColorStop(0.8, hexToRgba(c.color, 0.7));
      }
      grad.addColorStop(1, '#ffffff');

      ctx.save();
      ctx.strokeStyle = grad;
      ctx.lineWidth = c.radius * 2;
      ctx.lineCap = 'round';
      ctx.beginPath();
      ctx.moveTo(tailX, tailY);
      ctx.lineTo(c.x, c.y);
      ctx.stroke();

      // Glowing head
      const headGrad = ctx.createRadialGradient(c.x, c.y, 0, c.x, c.y, c.radius * 3.5);
      headGrad.addColorStop(0, '#ffffff');
      headGrad.addColorStop(0.35, c.isRainbow ? '#29bfff' : c.color);
      headGrad.addColorStop(1, 'transparent');
      ctx.fillStyle = headGrad;
      ctx.beginPath();
      ctx.arc(c.x, c.y, c.radius * 3.5, 0, Math.PI * 2);
      ctx.fill();

      // Sparks
      for (const sp of c.sparks) {
        ctx.fillStyle = hexToRgba(sp.color, sp.alpha);
        ctx.beginPath();
        ctx.arc(sp.x, sp.y, sp.size, 0, Math.PI * 2);
        ctx.fill();
      }
      ctx.restore();
    }
  }

  render(now = performance.now()) {
    const ctx = this.ctx;
    ctx.clearRect(0, 0, this.width, this.height);

    // Deep cosmic background
    ctx.fillStyle = '#010306';
    ctx.fillRect(0, 0, this.width, this.height);

    // 1. Nebulae
    this.drawNebulae(ctx);

    // 2. Planet (behind stars for majestic depth)
    if (this.planet) {
      this.drawPlanet(ctx, this.planet);
    }

    // 3. Stars (3D perspective warp)
    for (let i = 0; i < this.stars.length; i++) {
      const s = this.stars[i];
      const k = this.fov / s.z;
      const px = this.cx + s.x * k;
      const py = this.cy + s.y * k;

      if (px < -10 || px > this.width + 10 || py < -10 || py > this.height + 10) continue;

      const size = Math.max(0.6, (1 - s.z / 1400) * s.size * 2.8);
      const twinkleOpacity = 0.55 + Math.sin(s.twinkle) * 0.45;
      const depthOpacity = Math.max(0.15, 1 - s.z / 1400);
      const alpha = twinkleOpacity * depthOpacity;

      ctx.fillStyle = hexToRgba(s.color, alpha);
      ctx.beginPath();
      ctx.arc(px, py, size, 0, Math.PI * 2);
      ctx.fill();

      // Subtle flare for large close stars
      if (size > 2.2) {
        ctx.fillStyle = hexToRgba('#ffffff', alpha * 0.5);
        ctx.fillRect(px - size * 1.8, py - 0.5, size * 3.6, 1);
        ctx.fillRect(px - 0.5, py - size * 1.8, 1, size * 3.6);
      }
    }

    // 4. Comets (shooting stars)
    this.drawComets(ctx);

    this.update(now);
    this.animId = requestAnimationFrame(this.render);
  }

  start() {
    if (!this.animId) this.animId = requestAnimationFrame(this.render);
  }

  stop() {
    if (this.animId) {
      cancelAnimationFrame(this.animId);
      this.animId = null;
    }
  }

  destroy() {
    this.stop();
    window.removeEventListener('resize', this.resize);
  }
}

function hexToRgba(hex, alpha = 1) {
  if (!hex || typeof hex !== 'string') return `rgba(41, 191, 255, ${alpha})`;
  if (hex.startsWith('rgba') || hex.startsWith('rgb')) return hex;
  let clean = hex.replace('#', '');
  if (clean.length === 3) clean = clean.split('').map(c => c + c).join('');
  const num = parseInt(clean, 16);
  if (isNaN(num)) return `rgba(41, 191, 255, ${alpha})`;
  const r = (num >> 16) & 255;
  const g = (num >> 8) & 255;
  const b = num & 255;
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}


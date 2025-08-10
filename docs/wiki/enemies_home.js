document.addEventListener('DOMContentLoaded', () => {
  // Data mapping derived from Scriptable Objects (flattened for the web)
  // Slime sizes in order and display name
  const sizes = [
    { key: 'small', name: 'Small' },
    { key: 'medium', name: 'Medium' },
    { key: 'large', name: 'Large' },
  ];

  // Spawn range strings for convenience
  const spawn = {
    small: '50 - 1000',
    medium: '500 - 2000',
    large: '1000 - ∞'
  };

  const slimeStatsByColor = {
    green: {
      small:  { exp: 1,  hp: 5,   dmg: 1,   def: 0,   atkSpd: 1,   atkRange: 1, moveSpd: 3, vision: 7, assist: 5, wander: 2, spawn: spawn.small,  img: '../images/enemies/slimes/Slime_Small_Green.png',  hpPerLvl: 2,  dmgPerLvl: 0.1, defPerLvl: 0.01, distPerLvl: 20 },
      medium: { exp: 4,  hp: 66,  dmg: 4.2, def: 0.3, atkSpd: 1.5, atkRange: 1, moveSpd: 5, vision: 7, assist: 5, wander: 3, spawn: spawn.medium, img: '../images/enemies/slimes/Slime_Medium_Green.png', hpPerLvl: 4,  dmgPerLvl: 0.5, defPerLvl: 0.02, distPerLvl: 20 },
      large:  { exp: 15, hp: 200, dmg: 20,  def: 1,   atkSpd: 2,   atkRange: 1, moveSpd: 6, vision: 7, assist: 5, wander: 4, spawn: spawn.large,  img: '../images/enemies/slimes/Slime_Big_Green.png',   hpPerLvl: 8,  dmgPerLvl: 5,   defPerLvl: 0.04, distPerLvl: 20 },
    },
    yellow: {
      small:  { exp: 2,  hp: 5,   dmg: 1,   def: 0,   atkSpd: 1,   atkRange: 1, moveSpd: 3, vision: 7, assist: 5, wander: 2, spawn: spawn.small,  img: '../images/enemies/slimes/Slime_Small_Yellow.png', hpPerLvl: 3,  dmgPerLvl: 0.1, defPerLvl: 0.01, distPerLvl: 20 },
      medium: { exp: 5,  hp: 96,  dmg: 4.2, def: 0.3, atkSpd: 1.5, atkRange: 1, moveSpd: 5, vision: 7, assist: 5, wander: 3, spawn: spawn.medium, img: '../images/enemies/slimes/Slime_Medium_Yellow.png', hpPerLvl: 6,  dmgPerLvl: 0.5, defPerLvl: 0.02, distPerLvl: 20 },
      large:  { exp: 20, hp: 295, dmg: 20,  def: 1,   atkSpd: 2,   atkRange: 1, moveSpd: 6, vision: 7, assist: 5, wander: 4, spawn: spawn.large,  img: '../images/enemies/slimes/Slime_Big_Yellow.png',  hpPerLvl: 12, dmgPerLvl: 5,   defPerLvl: 0.04, distPerLvl: 20 },
    },
    blue: {
      small:  { exp: 3,  hp: 5,   dmg: 1,   def: 0,   atkSpd: 1,   atkRange: 1, moveSpd: 3, vision: 7, assist: 5, wander: 2, spawn: spawn.small,  img: '../images/enemies/slimes/Slime_Small_Blue.png',   hpPerLvl: 4,  dmgPerLvl: 0.2, defPerLvl: 0.01, distPerLvl: 20 },
      medium: { exp: 7,  hp: 126, dmg: 7.2, def: 0.3, atkSpd: 1.5, atkRange: 1, moveSpd: 5, vision: 7, assist: 5, wander: 3, spawn: spawn.medium, img: '../images/enemies/slimes/Slime_Medium_Blue.png',  hpPerLvl: 8,  dmgPerLvl: 1,   defPerLvl: 0.02, distPerLvl: 20 },
      large:  { exp: 25, hp: 391, dmg: 38,  def: 1,   atkSpd: 2,   atkRange: 1, moveSpd: 6, vision: 7, assist: 5, wander: 4, spawn: spawn.large,  img: '../images/enemies/slimes/Slime_Big_Blue.png',   hpPerLvl: 16, dmgPerLvl: 10,  defPerLvl: 0.04, distPerLvl: 20 },
    },
    red: {
      small:  { exp: 5,  hp: 5,   dmg: 1,    def: 0,   atkSpd: 1,   atkRange: 1, moveSpd: 3, vision: 7, assist: 5, wander: 2, spawn: spawn.small,  img: '../images/enemies/slimes/Slime_Small_Red.png',   hpPerLvl: 5,  dmgPerLvl: 0.3,  defPerLvl: 0.02, distPerLvl: 20 },
      medium: { exp:10,  hp:156, dmg:10.2,  def:0.6,  atkSpd:1.5,  atkRange:1, moveSpd:5, vision:7, assist:5, wander:3, spawn: spawn.medium, img: '../images/enemies/slimes/Slime_Medium_Red.png',  hpPerLvl: 10, dmgPerLvl: 1.5, defPerLvl: 0.04, distPerLvl: 20 },
      large:  { exp:30,  hp:487, dmg:57,    def:2,    atkSpd:2,    atkRange:1, moveSpd:6, vision:7, assist:5, wander:4, spawn: spawn.large,  img: '../images/enemies/slimes/Slime_Big_Red.png',    hpPerLvl: 20, dmgPerLvl: 15,  defPerLvl: 0.08, distPerLvl: 20 },
    },
    pink: {
      small:  { exp:10,  hp: 5,  dmg: 1,   def: 0,   atkSpd: 1,   atkRange: 1, moveSpd: 3, vision: 7, assist: 5, wander: 2, spawn: spawn.small,  img: '../images/enemies/slimes/Slime_Small_Pink.png',  hpPerLvl: 7,  dmgPerLvl: 0.5, defPerLvl: 0.03, distPerLvl: 20 },
      medium: { exp:25,  hp:216, dmg:16.2, def:0.9,  atkSpd:1.5,  atkRange:1, moveSpd:5, vision:7, assist:5, wander:3, spawn: spawn.medium, img: '../images/enemies/slimes/Slime_Medium_Pink.png',  hpPerLvl: 14, dmgPerLvl: 2.5, defPerLvl: 0.06, distPerLvl: 20 },
      large:  { exp:50,  hp:679, dmg:94,   def:3,    atkSpd:2,    atkRange:1, moveSpd:6, vision:7, assist:5, wander:4, spawn: spawn.large,  img: '../images/enemies/slimes/Slime_Big_Pink.png',   hpPerLvl: 28, dmgPerLvl: 25,  defPerLvl: 0.12, distPerLvl: 20 },
    }
  };

  const skeletons = [
    { name: 'Swordsman', exp: 4, hp: 20,  dmg: 2,  def: 0.5, atkSpd: 1.3, atkRange: 1, moveSpd: 5, vision: 7, assist: 5, wander: 3, spawn: '50 - ∞',   hpPerLvl: 5,  dmgPerLvl: 0.6,  defPerLvl: 0.12, distPerLvl: 20 },
    { name: 'Archer',    exp: 4, hp: 100, dmg: 8,  def: 0.6, atkSpd: 1.3, atkRange: 6, moveSpd: 5, vision: 7, assist: 5, wander: 3, spawn: '500 - ∞', hpPerLvl: 8,  dmgPerLvl: 1.5, defPerLvl: 0.12, distPerLvl: 20 },
    { name: 'Mage',      exp: 7, hp: 300, dmg: 30, def: 1,   atkSpd: 1.5, atkRange: 8, moveSpd: 5, vision: 8, assist: 6, wander: 4, spawn: '1000 - ∞',hpPerLvl: 20, dmgPerLvl: 5,   defPerLvl: 0.15, distPerLvl: 20 },
  ];

  // Initialize all slime cards
  document.querySelectorAll('.enemy-card.slime-card').forEach(card => {
    // default active tab
    card.setAttribute('data-active-tab', 'base');

    const color = card.dataset.color;
    const dots = card.querySelector('.dots');
    const variantName = card.querySelector('.variant-name');
    const prevBtn = card.querySelector('.prev');
    const nextBtn = card.querySelector('.next');
    const imgEl = card.querySelector('.slime-img');
    const tabBtns = card.querySelectorAll('.tab-btn');

    prevBtn.textContent = '‹';
    nextBtn.textContent = '›';

    const statsEls = mapStatEls(card);
    let idx = 0; // start at Small

    // build dots with thumbnails/swatch backgrounds
    sizes.forEach((s, i) => {
      const dot = document.createElement('span');
      dot.className = 'dot has-thumb' + (i === idx ? ' active' : '');
      const datum = slimeStatsByColor[color][s.key];
      if (datum && datum.img) {
        dot.style.backgroundImage = `url(${datum.img})`;
      } else if (datum && datum.color) {
        dot.style.background = datum.color;
        dot.classList.remove('has-thumb'); // solid color dot
      }
      dots.appendChild(dot);
    });

    const update = () => {
      const sizeKey = sizes[idx].key;
      const data = slimeStatsByColor[color][sizeKey];
      variantName.textContent = sizes[idx].name;
      setStats(statsEls, data);
      syncDots(dots, idx);
      if (imgEl) {
        if (data.img) {
          imgEl.src = data.img;
          imgEl.alt = `${sizes[idx].name} ${capitalize(color)} Slime`;
          imgEl.style.display = '';
        } else {
          imgEl.style.display = 'none';
        }
      }
    };

    prevBtn.addEventListener('click', () => { idx = (idx - 1 + sizes.length) % sizes.length; update(); });
    nextBtn.addEventListener('click', () => { idx = (idx + 1) % sizes.length; update(); });

    update();

    // tabs
    tabBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        tabBtns.forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        const tab = btn.getAttribute('data-tab');
        card.setAttribute('data-active-tab', tab);
        // update aria-selected
        tabBtns.forEach(b => b.setAttribute('aria-selected', b === btn ? 'true' : 'false'));
      });
    });
  });

  // Initialize skeleton card
  document.querySelectorAll('.enemy-card.skeleton-card').forEach(card => {
    // default active tab
    card.setAttribute('data-active-tab', 'base');

    const dots = card.querySelector('.dots');
    const variantName = card.querySelector('.variant-name');
    const prevBtn = card.querySelector('.prev');
    const nextBtn = card.querySelector('.next');
    const imgEl = card.querySelector('.skeleton-img');
    const tabBtns = card.querySelectorAll('.tab-btn');

    prevBtn.textContent = '‹';
    nextBtn.textContent = '›';

    const statsEls = mapStatEls(card);
    let idx = 0;

    skeletons.forEach((_, i) => {
      const dot = document.createElement('span');
      dot.className = 'dot' + (i === idx ? ' active' : '');
      dots.appendChild(dot);
    });

    const skeletonImages = {
      'Swordsman': '../images/enemies/skeletons/Skeleton_Swordman.png',
      'Archer': '../images/enemies/skeletons/Skeleton_Bowman.png',
      'Mage': '../images/enemies/skeletons/Skeleton_Mage.png'
    };

    const update = () => {
      const data = skeletons[idx];
      variantName.textContent = data.name;
      setStats(statsEls, data);
      syncDots(dots, idx);
      if (imgEl && skeletonImages[data.name]) {
        imgEl.src = skeletonImages[data.name];
        imgEl.alt = `Skeleton ${data.name}`;
      }
    };

    prevBtn.addEventListener('click', () => { idx = (idx - 1 + skeletons.length) % skeletons.length; update(); });
    nextBtn.addEventListener('click', () => { idx = (idx + 1) % skeletons.length; update(); });

    update();

    // tabs
    tabBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        tabBtns.forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        const tab = btn.getAttribute('data-tab');
        card.setAttribute('data-active-tab', tab);
        tabBtns.forEach(b => b.setAttribute('aria-selected', b === btn ? 'true' : 'false'));
      });
    });
  });

  function mapStatEls(scope) {
    return {
      exp: scope.querySelector('.exp'),
      hp: scope.querySelector('.hp'),
      dmg: scope.querySelector('.dmg'),
      def: scope.querySelector('.def'),
      atkSpd: scope.querySelector('.atkSpd'),
      atkRange: scope.querySelector('.atkRange'),
      moveSpd: scope.querySelector('.moveSpd'),
      vision: scope.querySelector('.vision'),
      assist: scope.querySelector('.assist'),
      wander: scope.querySelector('.wander'),
      spawn: scope.querySelector('.spawn'),
      hpPerLvl: scope.querySelector('.hpPerLvl'),
      dmgPerLvl: scope.querySelector('.dmgPerLvl'),
      defPerLvl: scope.querySelector('.defPerLvl'),
      distPerLvl: scope.querySelector('.distPerLvl'),
    };
  }

  function setStats(els, data) {
    els.exp.textContent = data.exp;
    els.hp.textContent = data.hp;
    els.dmg.textContent = data.dmg;
    els.def.textContent = data.def;
    els.atkSpd.textContent = data.atkSpd;
    els.atkRange.textContent = data.atkRange;
    els.moveSpd.textContent = data.moveSpd;
    els.vision.textContent = data.vision;
    els.assist.textContent = data.assist;
    els.wander.textContent = data.wander;
    els.spawn.textContent = data.spawn;
    if (els.hpPerLvl) els.hpPerLvl.textContent = data.hpPerLvl ?? '—';
    if (els.dmgPerLvl) els.dmgPerLvl.textContent = data.dmgPerLvl ?? '—';
    if (els.defPerLvl) els.defPerLvl.textContent = data.defPerLvl ?? '—';
    if (els.distPerLvl) els.distPerLvl.textContent = data.distPerLvl ?? '—';
  }

  function syncDots(container, active) {
    Array.from(container.children).forEach((dot, i) => dot.classList.toggle('active', i === active));
  }

  function capitalize(s){ return s ? s.charAt(0).toUpperCase() + s.slice(1) : s; }
});

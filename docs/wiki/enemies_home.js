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
      small: { exp: 1, hp: 5, dmg: 1, def: 0, atkSpd: 1, atkRange: 1, moveSpd: 3, vision: 7, assist: 5, wander: 2, spawn: spawn.small, img: '../images/enemies/slimes/Slime_Small_Green.png' },
      medium:{ exp: 4, hp: 66, dmg: 4.2, def: 0.3, atkSpd: 1.5, atkRange: 1, moveSpd: 5, vision: 7, assist: 5, wander: 3, spawn: spawn.medium, img: '../images/enemies/slimes/Slime_Medium_Green.png' },
      large: { exp: 15, hp: 200, dmg: 20, def: 1, atkSpd: 2, atkRange: 1, moveSpd: 6, vision: 7, assist: 5, wander: 4, spawn: spawn.large, img: '../images/enemies/slimes/Slime_Big_Green.png' },
    },
    yellow: {
      small: { exp: 2, hp: 5, dmg: 1, def: 0, atkSpd: 1, atkRange: 1, moveSpd: 3, vision: 7, assist: 5, wander: 2, spawn: spawn.small, img: '../images/enemies/slimes/Slime_Small_Yellow.png' },
      medium:{ exp: 5, hp: 96, dmg: 4.2, def: 0.3, atkSpd: 1.5, atkRange: 1, moveSpd: 5, vision: 7, assist: 5, wander: 3, spawn: spawn.medium, img: '../images/enemies/slimes/Slime_Medium_Yellow.png' },
      large: { exp: 20, hp: 295, dmg: 20, def: 1, atkSpd: 2, atkRange: 1, moveSpd: 6, vision: 7, assist: 5, wander: 4, spawn: spawn.large, img: '../images/enemies/slimes/Slime_Big_Yellow.png' },
    },
    blue: {
      small: { exp: 3, hp: 5, dmg: 1, def: 0, atkSpd: 1, atkRange: 1, moveSpd: 3, vision: 7, assist: 5, wander: 2, spawn: spawn.small, img: '../images/enemies/slimes/Slime_Small_Blue.png' },
      medium:{ exp: 7, hp: 126, dmg: 7.2, def: 0.3, atkSpd: 1.5, atkRange: 1, moveSpd: 5, vision: 7, assist: 5, wander: 3, spawn: spawn.medium, img: '../images/enemies/slimes/Slime_Medium_Blue.png' },
      large: { exp: 25, hp: 391, dmg: 38, def: 1, atkSpd: 2, atkRange: 1, moveSpd: 6, vision: 7, assist: 5, wander: 4, spawn: spawn.large, img: '../images/enemies/slimes/Slime_Big_Blue.png' },
    },
    red: {
      small: { exp: 5, hp: 5, dmg: 1, def: 0, atkSpd: 1, atkRange: 1, moveSpd: 3, vision: 7, assist: 5, wander: 2, spawn: spawn.small, img: '../images/enemies/slimes/Slime_Small_Red.png' },
      medium:{ exp:10, hp:156, dmg:10.2, def:0.6, atkSpd:1.5, atkRange:1, moveSpd:5, vision:7, assist:5, wander:3, spawn: spawn.medium, img: '../images/enemies/slimes/Slime_Medium_Red.png' },
      large: { exp:30, hp:487, dmg:57,   def:2,   atkSpd:2,   atkRange:1, moveSpd:6, vision:7, assist:5, wander:4, spawn: spawn.large, img: '../images/enemies/slimes/Slime_Big_Red.png' },
    },
    pink: {
      small: { exp:10, hp: 5, dmg: 1, def: 0, atkSpd: 1, atkRange: 1, moveSpd: 3, vision: 7, assist: 5, wander: 2, spawn: spawn.small, img: '../images/enemies/slimes/Slime_Small_Pink.png' },
      medium:{ exp:25, hp:216, dmg:16.2, def:0.9, atkSpd:1.5, atkRange:1, moveSpd:5, vision:7, assist:5, wander:3, spawn: spawn.medium, img: '../images/enemies/slimes/Slime_Medium_Pink.png' },
      large: { exp:50, hp:679, dmg:94,   def:3,   atkSpd:2,   atkRange:1, moveSpd:6, vision:7, assist:5, wander:4, spawn: spawn.large, img: '../images/enemies/slimes/Slime_Big_Pink.png' },
    }
  };

  const skeletons = [
    { name: 'Swordsman', exp: 4, hp: 15, dmg: 1, def: 0.5, atkSpd: 1.3, atkRange: 1, moveSpd: 5, vision: 7, assist: 5, wander: 3, spawn: '50 - ∞' },
    { name: 'Archer',    exp: 4, hp: 5,  dmg: 3, def: 0.5, atkSpd: 1.3, atkRange: 6, moveSpd: 5, vision: 7, assist: 5, wander: 3, spawn: '50 - ∞' },
    { name: 'Mage',      exp: 7, hp: 25, dmg: 4, def: 0.5, atkSpd: 1.5, atkRange: 8, moveSpd: 5, vision: 8, assist: 6, wander: 4, spawn: '150 - ∞' },
  ];

  // Initialize all slime cards
  document.querySelectorAll('.enemy-card.slime-card').forEach(card => {
    const color = card.dataset.color;
    const dots = card.querySelector('.dots');
    const variantName = card.querySelector('.variant-name');
    const prevBtn = card.querySelector('.prev');
    const nextBtn = card.querySelector('.next');
    const imgEl = card.querySelector('.slime-img');

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
  });

  // Initialize skeleton card
  document.querySelectorAll('.enemy-card.skeleton-card').forEach(card => {
    const dots = card.querySelector('.dots');
    const variantName = card.querySelector('.variant-name');
    const prevBtn = card.querySelector('.prev');
    const nextBtn = card.querySelector('.next');
    const imgEl = card.querySelector('.skeleton-img');

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
  }

  function syncDots(container, active) {
    Array.from(container.children).forEach((dot, i) => dot.classList.toggle('active', i === active));
  }

  function capitalize(s){ return s ? s.charAt(0).toUpperCase() + s.slice(1) : s; }
});

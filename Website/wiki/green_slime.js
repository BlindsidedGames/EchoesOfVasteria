document.addEventListener('DOMContentLoaded', () => {
    const slimeData = [
        {
            name: 'Small',
            image: '../images/Slime_Small_Green.png',
            stats: {
                exp: '1', hp: '10', dmg: '1', def: '0',
                atkSpd: '1', atkRange: '1', moveSpd: '3',
                vision: '7', assist: '5', wander: '2', spawn: '45 - 150'
            }
        },
        {
            name: 'Medium',
            image: '../images/Slime_Medium_Green.png',
            stats: {
                exp: '4', hp: '25', dmg: '2', def: '0.5',
                atkSpd: '1.3', atkRange: '1', moveSpd: '5',
                vision: '7', assist: '5', wander: '3', spawn: '150 - 500'
            }
        },
        {
            name: 'Large',
            image: '../images/Slime_Big_Green.png',
            stats: {
                exp: '15', hp: '250', dmg: '10', def: '10',
                atkSpd: '1.3', atkRange: '1', moveSpd: '5',
                vision: '7', assist: '5', wander: '3', spawn: '500+'
            }
        }
    ];

    let currentSlimeIndex = 0;

    const slimeImage = document.getElementById('slime-image');
    const slimeName = document.getElementById('slime-name');
    const dotsContainer = document.getElementById('slime-dots-container');
    const prevButton = document.getElementById('prev-slime');
    const nextButton = document.getElementById('next-slime');
    
    const statElements = {
        exp: document.getElementById('stat-exp'),
        hp: document.getElementById('stat-hp'),
        dmg: document.getElementById('stat-dmg'),
        def: document.getElementById('stat-def'),
        atkSpd: document.getElementById('stat-atk-spd'),
        atkRange: document.getElementById('stat-atk-range'),
        moveSpd: document.getElementById('stat-move-spd'),
        vision: document.getElementById('stat-vision'),
        assist: document.getElementById('stat-assist'),
        wander: document.getElementById('stat-wander'),
        spawn: document.getElementById('stat-spawn'),
    };

    function updateSlimeInfo(index) {
        const slime = slimeData[index];

        slimeImage.src = slime.image;
        slimeImage.alt = `${slime.name} Green Slime`;
        slimeName.textContent = slime.name;

        for (const key in slime.stats) {
            statElements[key].textContent = slime.stats[key];
        }
        
        updateDots(index);
    }
    
    function updateDots(activeIndex) {
        const dots = dotsContainer.children;
        for (let i = 0; i < dots.length; i++) {
            dots[i].classList.toggle('active', i === activeIndex);
        }
    }

    slimeData.forEach(() => {
        const dot = document.createElement('span');
        dot.classList.add('dot');
        dotsContainer.appendChild(dot);
    });

    prevButton.addEventListener('click', () => {
        currentSlimeIndex = (currentSlimeIndex - 1 + slimeData.length) % slimeData.length;
        updateSlimeInfo(currentSlimeIndex);
    });

    nextButton.addEventListener('click', () => {
        currentSlimeIndex = (currentSlimeIndex + 1) % slimeData.length;
        updateSlimeInfo(currentSlimeIndex);
    });

    // Initialize with the first slime
    updateSlimeInfo(currentSlimeIndex);
});

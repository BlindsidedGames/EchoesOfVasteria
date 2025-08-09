// Modern JavaScript for Echoes of Vasteria website
document.addEventListener('DOMContentLoaded', function() {
    // Initialize the website
    initializeWebsite();
});

function initializeWebsite() {
    // Add smooth scrolling for anchor links
    addSmoothScrolling();
    
    // Add theme toggle functionality
    addThemeToggle();
    
    // Add search functionality for wiki
    addSearchFunctionality();
    
    // Add loading animations
    addLoadingAnimations();
    
    // Add interactive features
    addInteractiveFeatures();

    // Compute dynamic offsets
    setDynamicOffsets();
    window.addEventListener('resize', setDynamicOffsets);
}

function setDynamicOffsets() {
    const header = document.querySelector('header');
    const topNav = document.querySelector('.top-nav');

    const headerH = header ? header.offsetHeight : 0;
    const topNavH = topNav ? topNav.offsetHeight : 0;
    const total = headerH + topNavH;

    const root = document.documentElement;
    root.style.setProperty('--header-height', headerH + 'px');
    root.style.setProperty('--topnav-height', topNavH + 'px');
    root.style.setProperty('--sticky-offset', total + 'px');
    root.style.setProperty('--nav-offset', total + 'px');
}

function addSmoothScrolling() {
    // Smooth scrolling for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
}

function addThemeToggle() {
    // Let wiki control its theme independently
    if (document.body.classList.contains('wiki-page')) {
        return;
    }

    // If page declares a locked theme, enforce it
    const lockedTheme = document.documentElement.getAttribute('data-lock-theme');
    if (lockedTheme === 'dark') {
        document.documentElement.classList.remove('light-mode');
        document.documentElement.classList.add('dark-mode');
        return;
    }

    // Check for saved theme preference; default to dark when not set
    const savedTheme = localStorage.getItem('theme');

    // Start from a clean state to avoid both classes present
    document.documentElement.classList.remove('light-mode', 'dark-mode');

    if (savedTheme === 'light' || savedTheme === 'dark') {
        document.documentElement.classList.add(savedTheme + '-mode');
    } else {
        document.documentElement.classList.add('dark-mode');
    }
    
    // Add theme toggle buttons if they exist
    const lightBtn = document.getElementById('light-btn');
    const darkBtn = document.getElementById('dark-btn');
    
    if (lightBtn && darkBtn) {
        lightBtn.addEventListener('click', () => {
            document.documentElement.classList.remove('dark-mode');
            document.documentElement.classList.add('light-mode');
            localStorage.setItem('theme', 'light');
            setDynamicOffsets();
        });
        
        darkBtn.addEventListener('click', () => {
            document.documentElement.classList.remove('light-mode');
            document.documentElement.classList.add('dark-mode');
            localStorage.setItem('theme', 'dark');
            setDynamicOffsets();
        });
    }
}

function addSearchFunctionality() {
    const searchInput = document.getElementById('search-input');
    if (searchInput) {
        const searchOptions = document.getElementById('search-options');

        // Map common terms to anchors or pages
        const anchorMap = {
            'wiki home': '#',
            'home': '#',
            'tasks': '#tasks',
            'task': '#tasks',
            'quests': '#quests',
            'quest': '#quests',
            'buffs': '#buffs',
            'buff': '#buffs',
            'enemies': '#enemies',
            'enemy': '#enemies',
            'slimes': '#enemies',
            'skeletons': '#enemies',
            'green slime': 'green.html'
        };

        const goTo = (rawValue) => {
            const value = (rawValue || '').trim().toLowerCase();
            if (!value) return;

            const mapped = anchorMap[value];
            if (mapped) {
                if (mapped.startsWith('#')) {
                    const target = document.querySelector(mapped);
                    if (target) {
                        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
                        // Update hash without adding history entries repeatedly
                        history.replaceState(null, '', mapped);
                    }
                } else {
                    window.location.href = mapped;
                }
                return;
            }

            // Fuzzy match against section headings
            const headings = Array.from(document.querySelectorAll('main section h2, main section h3'));
            const found = headings.find(h => h.textContent.trim().toLowerCase().includes(value));
            if (found) {
                const section = found.closest('section');
                if (section) {
                    section.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    if (section.id) history.replaceState(null, '', `#${section.id}`);
                }
            }
        };

        // Filter datalist options as you type
        searchInput.addEventListener('input', function() {
            const term = this.value.toLowerCase();
            if (searchOptions) {
                const options = searchOptions.querySelectorAll('option');
                options.forEach(option => {
                    option.style.display = option.value.toLowerCase().includes(term) ? 'block' : 'none';
                });
            }
        });

        // Navigate on Enter
        searchInput.addEventListener('keydown', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                goTo(searchInput.value);
            }
        });

        // Navigate when selecting a datalist option
        searchInput.addEventListener('change', function() {
            goTo(searchInput.value);
        });
    }
}

function addLoadingAnimations() {
    // Add fade-in animation for sections
    const sections = document.querySelectorAll('section');
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };
    
    const sectionObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, observerOptions);
    
    sections.forEach(section => {
        section.style.opacity = '0';
        section.style.transform = 'translateY(20px)';
        section.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
        sectionObserver.observe(section);
    });
}

function addInteractiveFeatures() {
    // Add hover effects for feature cards
    const featureCards = document.querySelectorAll('.feature-card');
    featureCards.forEach(card => {
        card.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-8px) scale(1.02)';
        });
        
        card.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0) scale(1)';
        });
    });
    
    // Add click effects for buttons
    const buttons = document.querySelectorAll('.btn');
    buttons.forEach(button => {
        button.addEventListener('click', function(e) {
            // Create ripple effect
            const ripple = document.createElement('span');
            const rect = this.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            const x = e.clientX - rect.left - size / 2;
            const y = e.clientY - rect.top - size / 2;
            
            ripple.style.width = ripple.style.height = size + 'px';
            ripple.style.left = x + 'px';
            ripple.style.top = y + 'px';
            ripple.classList.add('ripple');
            
            this.appendChild(ripple);
            
            setTimeout(() => {
                ripple.remove();
            }, 600);
        });
    });
}

// Add CSS for ripple effect
const rippleCSS = `
.ripple {
    position: absolute;
    border-radius: 50%;
    background: rgba(255, 255, 255, 0.3);
    transform: scale(0);
    animation: ripple-animation 0.6s linear;
    pointer-events: none;
}

@keyframes ripple-animation {
    to {
        transform: scale(4);
        opacity: 0;
    }
}

.btn {
    position: relative;
    overflow: hidden;
}
`;

// Inject ripple CSS
const style = document.createElement('style');
style.textContent = rippleCSS;
document.head.appendChild(style);

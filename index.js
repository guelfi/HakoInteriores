document.addEventListener('DOMContentLoaded', () => {
    const hamburger = document.querySelector('.hamburger');
    const navLinks = document.querySelector('.nav-links');
    const header = document.querySelector('header');
    const nav = document.querySelector('nav');
    const container = document.querySelector('header .container');
    const main = document.getElementById('main-content');

    const setExpanded = () => {
        hamburger.setAttribute('aria-expanded', navLinks.classList.contains('active') ? 'true' : 'false');
    };
    const toggleBlur = (active) => {
        document.body.classList.toggle('menu-open', !!active);
        if (main) {
            if (active) {
                try { main.setAttribute('inert', ''); } catch {}
                main.setAttribute('aria-hidden', 'true');
            } else {
                try { main.removeAttribute('inert'); } catch {}
                main.setAttribute('aria-hidden', 'false');
            }
        }
    };
    const closeMenu = () => {
        navLinks.classList.remove('active');
        setExpanded();
        toggleBlur(false);
    };

    hamburger.addEventListener('click', () => {
        navLinks.classList.toggle('active');
        setExpanded();
        toggleBlur(navLinks.classList.contains('active'));
        if (navLinks.classList.contains('active')) {
            const first = navLinks.querySelector('a');
            if (first) first.focus();
        }
    });
    hamburger.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); hamburger.click(); }
    });

    document.querySelectorAll('.nav-links li a').forEach(link => {
        link.addEventListener('click', closeMenu);
    });

    document.addEventListener('click', (e) => {
        if (!nav.contains(e.target) && navLinks.classList.contains('active')) closeMenu();
    });
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && navLinks.classList.contains('active')) closeMenu();
    });

    // Header com fundo ao rolar
    const onScroll = () => {
        header.classList.toggle('header-scrolled', window.scrollY > 50);
    };
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();

    // Colapsa o menu quando não há espaço
    const updateNavMode = () => {
        const available = (container ? container.clientWidth : window.innerWidth) - 160;
        const shouldCollapse = window.innerWidth <= 1024 || navLinks.scrollWidth > available;
        nav.classList.toggle('nav-collapsed', shouldCollapse);
        if (!shouldCollapse) closeMenu();
    };
    window.addEventListener('resize', updateNavMode);
    window.addEventListener('orientationchange', updateNavMode);
    updateNavMode();

    // Reveal on scroll
    const revealEls = document.querySelectorAll('.reveal');
    if (revealEls.length) {
        const io = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('in-view');
                    io.unobserve(entry.target);
                }
            });
        }, { threshold: 0.15, rootMargin: '0px 0px -8% 0px' });
        revealEls.forEach(el => io.observe(el));
    }

    // Link ativo conforme a seção visível
    const sectionEls = Array.from(document.querySelectorAll('section[id]'));
    if (sectionEls.length) {
        const ioSec = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    document.querySelectorAll('.nav-links a').forEach(a => {
                        a.classList.toggle('active', a.getAttribute('href') === '#' + entry.target.id);
                    });
                }
            });
        }, { threshold: 0.55 });
        sectionEls.forEach(el => ioSec.observe(el));
    }

    // Galeria: toque em mobile mostra legenda
    document.querySelectorAll('.gallery-item').forEach(el => {
        el.setAttribute('tabindex', '0');
        const toggle = () => el.classList.toggle('force-title');
        el.addEventListener('click', toggle);
        el.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); toggle(); }
        });
    });

    // Carrosséis mobile (<=820px): indicadores de posição
    const initCarousel = (container) => {
        if (!container) return;
        const items = Array.from(container.children);
        if (!items.length) return;
        const mq = window.matchMedia('(max-width: 820px)');
        let dotsWrap = null;
        let dots = [];
        let ticking = false;

        const setActive = () => {
            ticking = false;
            const cRect = container.getBoundingClientRect();
            const center = cRect.left + cRect.width / 2;
            let best = 0, bestDist = Infinity;
            items.forEach((item, i) => {
                const r = item.getBoundingClientRect();
                const dist = Math.abs(r.left + r.width / 2 - center);
                if (dist < bestDist) { bestDist = dist; best = i; }
            });
            dots.forEach((d, i) => d.classList.toggle('active', i === best));
        };
        const onScroll = () => {
            if (!ticking) {
                ticking = true;
                requestAnimationFrame(setActive);
            }
        };
        const build = () => {
            if (dotsWrap) return;
            dotsWrap = document.createElement('div');
            dotsWrap.className = 'carousel-dots';
            dots = items.map((item, i) => {
                const dot = document.createElement('button');
                dot.type = 'button';
                dot.className = 'carousel-dot';
                dot.setAttribute('aria-label', `Ir para o item ${i + 1}`);
                dot.addEventListener('click', () => {
                    item.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
                });
                dotsWrap.appendChild(dot);
                return dot;
            });
            container.insertAdjacentElement('afterend', dotsWrap);
            container.addEventListener('scroll', onScroll, { passive: true });
            setActive();
        };
        const teardown = () => {
            if (!dotsWrap) return;
            container.removeEventListener('scroll', onScroll);
            dotsWrap.remove();
            dotsWrap = null;
            dots = [];
        };
        const sync = () => { mq.matches ? build() : teardown(); };
        if (mq.addEventListener) mq.addEventListener('change', sync);
        else mq.addListener(sync);
        sync();
    };
    initCarousel(document.querySelector('.gallery-grid'));

    // Rodapé fixo: some na seção de contato (infos repetidas)
    const footerBar = document.getElementById('footer-bar');
    const contato = document.getElementById('contato');
    if (footerBar && contato) {
        const ioFoot = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                footerBar.classList.toggle('footer-hidden', entry.isIntersecting && entry.intersectionRatio > 0.2);
            });
        }, { threshold: [0, 0.2, 0.5] });
        ioFoot.observe(contato);
    }

    // Formulário -> WhatsApp
    const form = document.getElementById('contact-form');
    if (form) {
        form.addEventListener('submit', (e) => {
            e.preventDefault();
            const nome = form.nome.value.trim();
            const telefone = form.telefone.value.trim();
            const mensagem = form.mensagem.value.trim();
            const linhas = [
                `Olá, Hako! Meu nome é ${nome}.`,
                telefone ? `Meu telefone: ${telefone}.` : '',
                mensagem ? `Sobre o projeto: ${mensagem}` : 'Quero conversar sobre um projeto.',
            ].filter(Boolean);
            window.open(`https://wa.me/5511948854699?text=${encodeURIComponent(linhas.join('\n'))}`, '_blank', 'noopener');
        });
    }

    // Links de admin quando há sessão (cookie-marcador gravado pelo app; só dica visual)
    if (/(^|;\s*)hako_auth=1/.test(document.cookie)) {
        const prefix = location.pathname.startsWith('/hako/') ? '/hako' : '';
        const dash = document.getElementById('nav-dashboard');
        const sair = document.getElementById('nav-sair');
        if (dash) { dash.href = `${prefix}/dashboard`; dash.parentElement.hidden = false; }
        if (sair) { sair.href = `${prefix}/dashboard/conta/sair`; sair.parentElement.hidden = false; }
    }
});

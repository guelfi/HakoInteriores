document.addEventListener('DOMContentLoaded', () => {
    const hamburger = document.querySelector('.hamburger');
    const navLinks = document.querySelector('.nav-links');
    const header = document.querySelector('header');
    const nav = document.querySelector('nav');
    const container = document.querySelector('header .container');

    const setExpanded = () => {
        hamburger.setAttribute('aria-expanded', navLinks.classList.contains('active') ? 'true' : 'false');
    };
    const main = document.getElementById('main-content');
    const hero = document.getElementById('home');
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

    hamburger.addEventListener('click', () => {
        navLinks.classList.toggle('active');
        setExpanded();
        toggleBlur(navLinks.classList.contains('active'));
        if (navLinks.classList.contains('active')) {
            const first = navLinks.querySelector('a');
            if (first) first.focus();
        }
    });

    document.querySelectorAll('.nav-links li a').forEach(link => {
        link.addEventListener('click', () => {
            navLinks.classList.remove('active');
            setExpanded();
            toggleBlur(false);
        });
    });

    document.addEventListener('click', (e) => {
        if (!nav.contains(e.target) && navLinks.classList.contains('active')) {
            navLinks.classList.remove('active');
            setExpanded();
            toggleBlur(false);
        }
    });

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && navLinks.classList.contains('active')) {
            navLinks.classList.remove('active');
            setExpanded();
            toggleBlur(false);
        }
    });

    const onScroll = () => {
        if (window.scrollY > 50) {
            header.classList.add('header-scrolled');
        } else {
            header.classList.remove('header-scrolled');
        }
    };
    window.addEventListener('scroll', onScroll);
    onScroll();

    const updateNavMode = () => {
        const available = (container ? container.clientWidth : window.innerWidth) - 160;
        const needed = navLinks.scrollWidth;
        const shouldCollapse = window.innerWidth <= 1024 || needed > available;
        nav.classList.toggle('nav-collapsed', shouldCollapse);
    };
    window.addEventListener('resize', updateNavMode);
    window.addEventListener('orientationchange', updateNavMode);
    updateNavMode();

    const Automation = (() => {
        const log = [];
        const state = { paused: false };
        const storeLog = () => {
            try { localStorage.setItem('automationLog', JSON.stringify(log)); } catch {}
        };
        const addLog = (entry) => { log.push({ time: Date.now(), ...entry }); storeLog(); };

        const changes = [
            {
                id: 'lazy-images', impact: 3, complexity: 1, urgency: 3,
                apply() {
                    const imgs = Array.from(document.querySelectorAll('img'));
                    const prev = imgs.map(img => ({ img, loading: img.getAttribute('loading') }));
                    imgs.forEach(img => img.setAttribute('loading', 'lazy'));
                    return { prev };
                },
                validate() {
                    return Array.from(document.querySelectorAll('img')).every(img => img.getAttribute('loading') === 'lazy');
                },
                revert(ctx) {
                    ctx.prev.forEach(({ img, loading }) => {
                        if (loading === null) img.removeAttribute('loading'); else img.setAttribute('loading', loading);
                    });
                }
            },
            {
                id: 'meta-description', impact: 3, complexity: 1, urgency: 2,
                apply() {
                    const existing = document.querySelector('meta[name="description"]');
                    if (existing) return { removed: false };
                    const meta = document.createElement('meta');
                    meta.setAttribute('name', 'description');
                    meta.setAttribute('content', 'Móveis planejados de alto padrão, design exclusivo e acabamento premium. Hako Interiores transforma ambientes com marcenaria sob medida.');
                    document.head.appendChild(meta);
                    return { removed: true, meta };
                },
                validate() { return !!document.querySelector('meta[name="description"]'); },
                revert(ctx) { if (ctx.removed && ctx.meta && ctx.meta.parentNode) ctx.meta.parentNode.removeChild(ctx.meta); }
            },
            {
                id: 'preconnect-fonts', impact: 2, complexity: 1, urgency: 2,
                apply() {
                    const links = [];
                    const ensure = (href, crossorigin) => {
                        if (!Array.from(document.querySelectorAll('link[rel="preconnect"]')).some(l => l.href.indexOf(href) !== -1)) {
                            const link = document.createElement('link');
                            link.setAttribute('rel', 'preconnect');
                            link.setAttribute('href', href);
                            if (crossorigin) link.setAttribute('crossorigin', '');
                            document.head.appendChild(link);
                            links.push(link);
                        }
                    };
                    ensure('https://fonts.googleapis.com');
                    ensure('https://fonts.gstatic.com', true);
                    return { links };
                },
                validate() {
                    const hasA = Array.from(document.querySelectorAll('link[rel="preconnect"]')).some(l => l.href.indexOf('fonts.googleapis.com') !== -1);
                    const hasB = Array.from(document.querySelectorAll('link[rel="preconnect"]')).some(l => l.href.indexOf('fonts.gstatic.com') !== -1);
                    return hasA && hasB;
                },
                revert(ctx) { (ctx.links || []).forEach(l => l.parentNode && l.parentNode.removeChild(l)); }
            },
            {
                id: 'og-tags', impact: 2, complexity: 1, urgency: 1,
                apply() {
                    const created = [];
                    const ensure = (prop, content) => {
                        if (!document.querySelector('meta[property="' + prop + '"]')) {
                            const m = document.createElement('meta');
                            m.setAttribute('property', prop);
                            m.setAttribute('content', content);
                            document.head.appendChild(m);
                            created.push(m);
                        }
                    };
                    const title = document.title || 'Hako Interiores';
                    ensure('og:title', title);
                    ensure('og:description', 'Móveis planejados de alto padrão com design exclusivo e acabamento premium.');
                    return { created };
                },
                validate() {
                    return !!document.querySelector('meta[property="og:title"]') && !!document.querySelector('meta[property="og:description"]');
                },
                revert(ctx) { (ctx.created || []).forEach(m => m.parentNode && m.parentNode.removeChild(m)); }
            },
            {
                id: 'aria-labels', impact: 2, complexity: 1, urgency: 2,
                apply() {
                    const applied = [];
                    if (hamburger && !hamburger.getAttribute('aria-label')) { hamburger.setAttribute('aria-label', 'Abrir menu'); applied.push(hamburger); }
                    if (nav && !nav.getAttribute('role')) { nav.setAttribute('role', 'navigation'); applied.push(nav); }
                    Array.from(document.querySelectorAll('.footer-social a')).forEach(a => {
                        if (!a.getAttribute('aria-label')) {
                            const i = a.querySelector('i');
                            const label = i && i.className.includes('instagram') ? 'Instagram' : i && i.className.includes('facebook') ? 'Facebook' : i && i.className.includes('whatsapp') ? 'WhatsApp' : 'Rede social';
                            a.setAttribute('aria-label', label);
                            applied.push(a);
                        }
                    });
                    return { applied };
                },
                validate() {
                    const okHamburger = hamburger.getAttribute('aria-label') !== null;
                    const okNav = nav.getAttribute('role') === 'navigation';
                    const okSocial = Array.from(document.querySelectorAll('.footer-social a')).every(a => a.getAttribute('aria-label'));
                    return okHamburger && okNav && okSocial;
                },
                revert(ctx) { (ctx.applied || []).forEach(el => el.removeAttribute('aria-label')); if (nav) nav.removeAttribute('role'); }
            }
        ];

        const score = c => c.impact * 2 + c.urgency - c.complexity;
        const queue = changes.sort((a, b) => score(b) - score(a));

        const runChange = async c => {
            if (state.paused) return;
            const ctx = c.apply();
            let ok = false;
            try { ok = !!c.validate(); } catch { ok = false; }
            if (ok) addLog({ id: c.id, status: 'applied' }); else { try { c.revert(ctx); } catch {} addLog({ id: c.id, status: 'reverted' }); }
        };

        const runTests = () => {
            const a = document.querySelector('.hamburger');
            const l = document.querySelector('.nav-links');
            const before = l.classList.contains('active');
            a.click();
            const toggled = l.classList.contains('active') !== before;
            document.querySelector('.nav-links li a').click();
            const closed = !l.classList.contains('active');
            if (!toggled || !closed) throw new Error('menu');
            return true;
        };

        const monitor = () => {
            const banner = document.createElement('div');
            banner.className = 'monitor-banner';
            document.body.appendChild(banner);
            const start = performance.now();
            window.addEventListener('load', () => {
                const dur = Math.round(performance.now() - start);
                banner.textContent = 'Monitoramento ativo • carregamento ' + dur + 'ms';
                banner.classList.add('show');
            });
        };

        const api = {
            run() {
                monitor();
                let i = 0;
                const step = () => { if (i >= queue.length) { try { runTests(); addLog({ id: 'tests', status: 'passed' }); } catch { addLog({ id: 'tests', status: 'failed' }); } return; } runChange(queue[i++]).then(() => setTimeout(step, 0)); };
                step();
            },
            pause() { state.paused = true; addLog({ id: 'control', status: 'paused' }); },
            resume() { state.paused = false; addLog({ id: 'control', status: 'resumed' }); },
            report() { try { return JSON.parse(localStorage.getItem('automationLog') || '[]'); } catch { return []; } }
        };

        return api;
    })();

    window.AutomationControl = Automation;
    Automation.run();

    const items = Array.from(document.querySelectorAll('.gallery-item'));
    if (items.length) {
        const io = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                const el = entry.target;
                if (entry.isIntersecting) el.classList.add('in-view'); else el.classList.remove('in-view');
            });
        }, { root: null, threshold: 0.6, rootMargin: '0px 0px -5% 0px' });
        items.forEach(el => io.observe(el));
        items.forEach(el => {
            el.setAttribute('tabindex', '0');
            el.addEventListener('click', () => el.classList.toggle('force-title'));
            el.addEventListener('keydown', (e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); el.classList.toggle('force-title'); } });
        });
    }

    const hintStack = document.querySelector('.scroll-hint-stack');

    const anchors = Array.from(document.querySelectorAll('a[href^="#"]'));
    const ease = t => t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
    const computeBaseGap = () => {
        const hdr = header ? header.offsetHeight : 0;
        if (!hero) return 0;
        const topAbs = hero.getBoundingClientRect().top + window.pageYOffset;
        const gap = topAbs - hdr;
        return Math.max(0, Math.round(gap));
    };
    let baseGap = computeBaseGap();
    window.addEventListener('resize', () => { baseGap = computeBaseGap(); });
    window.addEventListener('orientationchange', () => { baseGap = computeBaseGap(); });
    const smoothTo = (target, duration, onDone) => {
        if (!target) return;
        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) { target.scrollIntoView(); return; }
        const start = window.pageYOffset;
        const rect = target.getBoundingClientRect();
        const headerOffset = header ? header.offsetHeight : 0;
        const end = Math.max(0, rect.top + start - headerOffset - (baseGap || 0));
        const dist = Math.abs(end - start);
        const dur = Math.max(500, Math.min(800, duration || (dist < 300 ? 650 : 800)));
        let cancelled = false;
        const stop = () => {
            if (cancelled) return;
            cancelled = true;
            ['wheel','touchstart','touchmove','keydown','mousedown'].forEach(evt => window.removeEventListener(evt, stop));
            if (onDone) onDone();
        };
        ['wheel','touchstart','touchmove','keydown','mousedown'].forEach(evt => window.addEventListener(evt, stop, { passive: true }));
        const t0 = performance.now();
        const step = now => {
            if (cancelled) return;
            const elapsed = Math.min(1, (now - t0) / dur);
            const pos = start + (end - start) * ease(elapsed);
            window.scrollTo(0, pos);
            if (elapsed < 1) requestAnimationFrame(step); else { try { target.focus({ preventScroll: true }); } catch {} if (onDone) onDone(); }
        };
        requestAnimationFrame(step);
    };

    anchors.forEach(a => {
        a.addEventListener('click', e => {
            const href = a.getAttribute('href');
            if (!href || href === '#') return;
            const id = href.slice(1);
            const target = document.getElementById(id);
            if (target) {
                e.preventDefault();
                smoothTo(target);
                const wasActive = navLinks.classList.contains('active');
                if (wasActive) { navLinks.classList.remove('active'); setExpanded(); toggleBlur(false); }
            }
        });
    });

    const sectionNodes = Array.from(document.querySelectorAll('section, .section'));
    const sectionEls = [];
    const seen = new Set();
    sectionNodes.forEach(el => { if (!seen.has(el)) { seen.add(el); sectionEls.push(el); } });
    const navActive = id => {
        if (!id) return;
        document.querySelectorAll('.nav-links a').forEach(a => {
            const href = a.getAttribute('href');
            const active = href === '#' + id;
            a.classList.toggle('active', !!active);
        });
    };
    const computePositions = () => sectionEls.map(el => ({ el, top: el.getBoundingClientRect().top + window.pageYOffset, height: el.offsetHeight, id: el.id || '' }));
    const getCurrentIndex = () => {
        if (!sectionEls.length) return 0;
        const headerOffset = header ? header.offsetHeight : 0;
        const y = window.pageYOffset + headerOffset + (baseGap || 0) + 1;
        let best = 0; let bestDist = Infinity;
        positions.forEach((p, i) => {
            const d = Math.abs(p.top - y);
            if (d < bestDist) { bestDist = d; best = i; }
        });
        return best;
    };
    let positions = computePositions();
    window.addEventListener('resize', () => { positions = computePositions(); });
    window.addEventListener('orientationchange', () => { positions = computePositions(); });
    let currentIndex = 0;
    if (sectionEls.length) {
        const ioSec = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting && entry.intersectionRatio >= 0.6) {
                    const idx = sectionEls.indexOf(entry.target);
                    if (idx !== -1) currentIndex = idx;
                    navActive(entry.target.id);
                }
            });
        }, { threshold: [0.6] });
        sectionEls.forEach(el => ioSec.observe(el));
    }

    let isScrollingIcon = false;
    let lastIconClick = 0;
    let continuousRunning = false;
    let continuousReq = null;
    let continuousInterrupt = null;
    let holdTimer = null;
    let holdTriggered = false;
    const HOLD_MS = 450;
    const startContinuous = dir => {
        if (continuousRunning) return;
        continuousRunning = true;
        let last = performance.now();
        const speed = Math.max(450, Math.min(950, window.innerHeight * 0.9));
        const interrupt = () => { stopContinuous(); };
        continuousInterrupt = interrupt;
        ['wheel','touchstart','touchmove','keydown','mousedown'].forEach(evt => window.addEventListener(evt, interrupt, { passive: true }));
        const step = now => {
            if (!continuousRunning) return;
            const dt = Math.min(0.05, (now - last) / 1000);
            last = now;
            const delta = speed * dt * (dir === 'up' ? -1 : 1);
            const maxY = Math.max(0, document.documentElement.scrollHeight - window.innerHeight);
            let y = window.pageYOffset + delta;
            if (y <= 0 || y >= maxY) {
                y = Math.min(maxY, Math.max(0, y));
                window.scrollTo(0, y);
                stopContinuous();
                return;
            }
            window.scrollTo(0, y);
            continuousReq = requestAnimationFrame(step);
        };
        continuousReq = requestAnimationFrame(step);
    };
    const stopContinuous = () => {
        if (!continuousRunning) return;
        continuousRunning = false;
        if (continuousReq) cancelAnimationFrame(continuousReq);
        if (continuousInterrupt) {
            ['wheel','touchstart','touchmove','keydown','mousedown'].forEach(evt => window.removeEventListener(evt, continuousInterrupt));
            continuousInterrupt = null;
        }
    };
    const singleScroll = (dir) => {
        const now = Date.now();
        if (isScrollingIcon) return;
        if (now - lastIconClick < 500) return;
        lastIconClick = now;
        currentIndex = getCurrentIndex();
        const idx = dir === 'up' ? Math.max(0, currentIndex - 1) : Math.min(sectionEls.length - 1, currentIndex + 1);
        const target = sectionEls[idx];
        if (!target) return;
        isScrollingIcon = true;
        smoothTo(target, 800, () => { isScrollingIcon = false; });
    };
    if (hintStack) {
        hintStack.querySelectorAll('.scroll-hint-btn').forEach(btn => {
            const getDir = () => btn.getAttribute('data-dir') === 'up' ? 'up' : 'down';
            const onPointerDown = () => {
                holdTriggered = false;
                clearTimeout(holdTimer);
                holdTimer = setTimeout(() => { holdTriggered = true; startContinuous(getDir()); }, HOLD_MS);
            };
            const onPointerUp = () => {
                clearTimeout(holdTimer);
                if (holdTriggered) { stopContinuous(); return; }
                singleScroll(getDir());
            };
            btn.addEventListener('pointerdown', onPointerDown);
            btn.addEventListener('pointerup', onPointerUp);
            btn.addEventListener('pointercancel', onPointerUp);
            btn.addEventListener('pointerleave', onPointerUp);
            // Fallbacks para navegadores sem Pointer Events
            btn.addEventListener('mousedown', onPointerDown);
            btn.addEventListener('mouseup', onPointerUp);
            btn.addEventListener('mouseleave', onPointerUp);
            btn.addEventListener('touchstart', onPointerDown, { passive: true });
            btn.addEventListener('touchend', onPointerUp);
            btn.addEventListener('touchcancel', onPointerUp);
            btn.addEventListener('click', e => { if (holdTriggered || continuousRunning) { e.preventDefault(); return; } /* evita dupla execução */ });
        });
    }

    const supportsFixed = typeof CSS !== 'undefined' && CSS.supports && CSS.supports('position', 'fixed');
    if (!supportsFixed) {
        const updateAbsPos = () => {
            const y = window.scrollY + window.innerHeight - 32;
            if (hintStack) { hintStack.style.position = 'absolute'; hintStack.style.top = (y - (hintStack.offsetHeight)) + 'px'; hintStack.style.right = '12px'; }
        };
        window.addEventListener('scroll', updateAbsPos, { passive: true });
        window.addEventListener('resize', updateAbsPos);
        updateAbsPos();
    }
});







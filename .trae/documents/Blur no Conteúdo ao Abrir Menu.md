## Objetivo e Requisitos
- Aplicar blur de 5px no conteúdo principal quando o menu hambúrguer estiver aberto, com transição suave de 300ms.
- Menu permanece nítido e totalmente funcional; conteúdo fica inacessível enquanto o menu estiver aberto.
- Remover o efeito automaticamente ao selecionar qualquer item do menu.
- Garantir acessibilidade e boa performance em telas e dispositivos variados.

## Estratégia CSS‑Pura
- Adicionar um wrapper para o conteúdo principal: `<main id="main-content">` englobando `hero`, `section(s)` e `footer`, mantendo o `header` fora do blur.
- Ativar o estado via classe global: adicionar/remover `menu-open` no `body`.
- Regras CSS:
  - `body.menu-open #main-content { filter: blur(5px); transition: filter 300ms ease-in-out; pointer-events: none; }`
  - `#main-content { contain: paint; }` para isolar repaints e reduzir custo do blur.
  - `@media (prefers-reduced-motion: reduce) { body.menu-open #main-content { transition: none; } }`
- Interatividade bloqueada sem overlay adicional (com `pointer-events: none` no conteúdo), mantendo o `header/nav` ativo.

## Fallback JavaScript
- Integrar ao código existente do menu:
  - Ao clicar no hambúrguer e ativar `.nav-links.active`, adicionar `document.body.classList.add('menu-open')`.
  - Ao clicar em qualquer item do menu, remover `menu-open` e fechar o painel (`.nav-links.active` → remover).
  - Atualizar `aria-expanded` do hambúrguer e mover foco para o primeiro link quando abrir.
- Inert/aria para acessibilidade:
  - Se suportado, aplicar `main.setAttribute('inert','')` quando `menu-open`; remover ao fechar.
  - Fallback: `aria-hidden="true"` no `#main-content` ao abrir; `false` ao fechar.

## Acessibilidade
- Foco inicial no primeiro item do menu ao abrir; `Escape` fecha menu e remove blur.
- Conteúdo principal inacessível (pointer-events/inert/aria-hidden), evitando navegação acidental.
- `aria-expanded` do hambúrguer sempre sincronizado com o estado.

## Performance e Compatibilidade
- Blur aplicado apenas ao wrapper do conteúdo, evitando processar o `header`.
- `contain: paint` no wrapper para reduzir invalidações e melhorar 60fps.
- Evitar `backdrop-filter`; usar `filter: blur`, mais previsível em compatibilidade.
- Manter o cancelamento de animação de scroll no `wheel` rápido já existente.

## Testes
- Mobile iOS/Android, tablets paisagem e notebooks com viewports intermediários.
- Diferentes densidades de tela e luminosidade; verificar nitidez e legibilidade do menu.
- Com `prefers-reduced-motion`, confirmar transição desativada.
- Verificar que clicar fora, selecionar item ou `Escape` remove blur e fecha menu.

## Mudanças em Arquivos
- `index.html`: envolver seções e `footer` com `<main id="main-content">` e manter `header` fora; sem alterar conteúdo interno.
- `index.css`: adicionar regras para `body.menu-open #main-content`, transição de 300ms, `pointer-events: none`, e `contain: paint`; incluir regra `prefers-reduced-motion`.
- `index.js`: no handler do hambúrguer/links/escape/click fora, adicionar/remover `menu-open`, gerenciar `inert`/`aria-hidden`, atualizar `aria-expanded` e foco.

## Resultado Esperado
- Ao abrir o menu hambúrguer, o conteúdo desfoca instantaneamente com transição suave; o menu fica nítido e acessível.
- Ao selecionar um item do menu, o blur é removido e o conteúdo volta a ficar interativo.
- Comportamento consistente e performático em navegadores modernos.
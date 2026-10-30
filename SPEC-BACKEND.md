# Hako — Spec Backend "Controle de Projeto"

Spec consolidada do brainstorm (out/2026). Backend interno da Hako Interiores: clientes, obras (Kanban), financeiro, contratos, assinatura e **portal do cliente** (diferencial central).

**Pipeline de documentos:** este SPEC (brainstorm consolidado) → **PRD** (Product Requirements Document) → **plano de desenvolvimento** → **roadmap geral do projeto**. O SPEC é a fonte de decisões; o PRD o formaliza em requisitos; plano e roadmap derivam do PRD.

## Stack

- **.NET 10** + **Blazor Server** (UI + API no mesmo app, deploy único)
- **EF Core + SQLite** (arquivo em volume Docker; migração futura p/ Postgres só trocando provider)
- **ASP.NET Identity** (usuários internos) + roles
- Container `hako-admin` na mesma rede Docker do site (`www_projetos-net`), atrás do Nginx proxy
- **Admin:** `hakointeriores.com.br/dashboard` (Nginx location `/dashboard` → `hako-admin`; Blazor `PathBase="/dashboard"`) — caminho não-publicado, sem link no menu da LP. Proteção real = Identity+roles+rate limit; a obscuridade é só camada extra
- **Portal do cliente:** `hakointeriores.com.br/obra/{token}`
- **Segurança (todas as camadas):** Cloudflare (WAF/rate-limit/proxy) → OCI → app .NET (Identity + roles + rate limiting + HTTPS + headers de segurança + validação de input em todo endpoint). Caminho `/dashboard` não-publicado é só camada extra — nunca substitui auth. Boas práticas são requisito de entrega, não opcionais
- **LGPD:** LP recebe link "Política de Privacidade" no rodapé (`/privacidade.html`); backend deve registrar consentimento e direitos do titular (acesso/correção/exclusão)

## Portal do cliente — peça central (SEM login, SEM dependência de WhatsApp)

- URL com token único por lead/obra: `app.hakointeriores.com.br/obra/{token}`
- Cliente recebe o link **logo após o primeiro contato** (por qualquer canal) e acompanha TUDO:
  - **Negociação:** status do orçamento → contrato para assinar na própria página
  - **Obra:** etapas em visão Kanban read-only com cores de saúde + fotos do andamento
  - **Entregue:** termo de entrega, canal de garantia/pós-venda
- Nada de Evolution API/WhatsApp API como canal obrigatório — portal é a fonte da verdade
- Gancho de marketing na LP: "acompanhe sua obra online, do contrato à entrega" (iteração futura)

### Portal — modelo de acesso

- Token na URL gerado ao criar lead/obra (ex.: `/obra/a8f3k2m9x7`), enviado por qualquer canal — sem login, sem senha, sem conta
- Token ≥192 bits de entropia, um por lead/obra, opaco (sem IDs sequenciais)
- Acesso **read-only**: cliente só vê a própria obra (etapas com cores, fotos, status do orçamento/contrato); únicas ações: assinar contrato (via provider, que re-autentica por e-mail/tel) e abrir chamado de garantia
- Revogável/rotacionável pelo Admin; expiração opcional (ex.: 90 dias pós-entrega) ou permanente
- Página evita dados sensíveis (sem CNPJ/CPF/valores expostos além do necessário); rate limit na rota + HTTPS
- Ressalva conhecida: posse do link = acesso; por isso dados minimizados e ações restritas

## RBAC e acesso

**Dois mundos:**
- **Interno** (`/dashboard`): Identity + roles + claims `perm="recurso.ação"` (Admin, Comercial, Financeiro, Obras)
- **Externo** (`/obra/{token}`): cliente/colaborador/parceiro via token por vínculo — sem login/conta

Claims por role: Admin→todas; Comercial→`clientes.*, funil.*, contratos.*, obras.view`; Financeiro→`financeiro.*, clientes.view, obras.view`; Obras→`obras.*, clientes.view`. Policies registradas por claim; verificadas no backend E nos widgets.

**Repo:** monorepo — backend .NET em `backend/` (`.sln`+Dockerfile próprios); frontend flat na raiz (deploy inalterado); rsync exclui `backend/`, spec, `*.key`, `*.crt`, `gitactions.err` ✓ aplicado.

## Dashboard (único, adaptativo por permissão)

- Um `Dashboard.razor`: widgets renderizam só se `perm` permite — Financeiro vê financeiro, Obras vê obras, mesma tela
- Widgets iniciais: total clientes, obras em andamento, faturamento mês, contas a receber/pagar, comissões pendentes, etapas atrasadas, leads novos
- Tema claro/escuro via CSS vars + `.dark` no root + toggle na topbar (localStorage)
- Layout: sidebar auto-oculta + topbar + área de conteúdo por seção

## UI/UX do admin — navegação e padrões de tela

**Mobile-first (design system):** toda a UI/UX é pensada **mobile first** — em vários cenários o acesso ao Dashboard será feito **direto da obra**. Três breakpoints de referência: **mobile · tablet · notebook/desktop**. Componentes, grids, cards-modais e boards (funil/kanban) devem funcionar plenamente nos três — ex.: board vira scroll horizontal por coluna no mobile, grid vira lista de cards, modais viram tela cheia. Decisão de design system do projeto, vale para todas as seções.

**Sidebar auto-oculta (sem JS):** colapsada por padrão mostrando só o rail de ícones; expande em overlay no `:hover`; mouse sai sem selecionar → recolhe mantendo a seção ativa; selecionou → navega e recolhe. Item ativo destacado no rail. Mobile: drawer via hambúrguer.

**Seções (topbar exibe o nome da seção ativa):** Dashboard · Clientes · Funil · Obras · Kanban · Financeiro · Contratos · Equipe · Config — cada item filtrado por `perm`; seção sem implementação exibe placeholder com o nome.

**Padrão grid (CRUD):** toolbar com `[+ Novo]` à esquerda e paginação ⏮ ◀ ▶ ⏭ à direita; clicar na linha abre o mesmo card-modal da inclusão (novo → [Salvar][Cancelar]; existente → [Salvar][Inativar|Cancelar…][Voltar]). **Nunca há exclusão de dados** — só inativação ou cancelamento; cancelamentos exigem justificativa (lançamentos, obras, contratos). Usuário logado fica na topbar (à direita) com dropdown: *Meu perfil* (modal: usuário, nome completo, senha ×2 com regras) e *Sair*. Tema claro/escuro no rodapé da sidebar. Boards (funil/kanban) com drag&drop entre colunas.

- **Clientes** — colunas: status (Lead|Cliente) + ativo/inativo, ícone de obra (em andamento 🛠 / entregue ✓ / cancelada ✗, +contador se >1), badge de atraso se houver etapa vermelha, nome, celular (contato direto sem abrir card)
  - **Pesquisa:** campo de busca na toolbar, entre `[+ Novo]` e a paginação — busca por nome, celular ou email (sem abrir card)
  - **Filtro:** dropdown/painel ao lado da busca com opções: tipo (todos | leads | clientes), status (ativo | inativo | cancelado | suspenso), origem (lp | instagram | indicacao | parceiro | outro), com obra em andamento, com etapa atrasada (badge 🔴), com/sem obras — combináveis entre si e com a busca
- **Equipe** (unificada) — Funcionários + Colaboradores + Parceiros num único grid, badge de perfil por linha; comissão% só para parceiros
- **Financeiro** — grid único pagar+receber: ícone direção (▲ pagar / ▼ receber), status (pago ✓ / pendente / vencido 🔴), descrição, beneficiário/cliente, valor, vencimento
- **Contratos** — abas: *Modelos* (ContratoTemplates) e *Emitidos* (contratos por cliente com status enviado/aguardando/assinado)
- **Obras** — grid de obras em andamento (não tipos — tipos ficam em Config/TiposServico): cliente, tipo, fase atual, %, previsão, cor de saúde
- **Funil** — colunas do pipeline com cards de lead: Novo → Contato → Visita/Medição → Orçamento → Negociação → Ganho | Perdido
- **Kanban** — board por obra: 5 colunas da spec com cards coloridos por saúde do prazo (drag-drop real na impl.; no mock, cards estáticos)

**Mock atual:** dados estáticos + seletor "visualizar como" (simula role sem Identity real). Serve para validar concepção e navegação antes da Fase 1 de auth.

**Sessão ↔ LP:** logado, o app grava cookie-marcador `hako_auth=1` (não-HttpOnly, sem valor de credencial — só dica visual; se forjado, o pior caso é exibir link que cai no login). A LP lê o cookie e exibe no menu `[Dashboard]` (`/dashboard`) + `Sair` (`/dashboard/conta/sair` → endpoint Identity que faz signout, limpa o marcador e redireciona à LP). No admin: "Voltar ao site" no rodapé da sidebar + "Sair" na topbar (rota/URL do site via config `SiteUrl`).

## Modelo de dados

```
Users (Identity) + Roles
TiposServico:    id, nome, ativo
TemplateEtapa:   id, tipoServicoId, nome, ordem, duracaoDias   → obra nasce com etapas+prazos prontos
Clientes:        id, nome, tel, email, endereco, status(prospecto|ativo|cancelado), origem, portalToken
Leads (funil):   Novo → Contato → Visita/Medição → Orçamento → Negociação → Ganho(vira cliente+obra) | Perdido(motivo)
Negociacoes:     id, pessoaId, estagio, status(em_andamento|cancelada|fechada), tipoServicoId,
                 valor?, origem, motivoPerda?, criadoEm, fechadaEm?, ganhoEm?
Visitas:         id, negociacaoId, dataAgendada, dataRealizada?, status(agendada|realizada|cancelada), observacoes?
Orcamentos:      id, negociacaoId, versao, valor, status(rascunho|enviado|aceito|recusado|substituido),
                 arquivo?, enviadoEm?, respondidoEm?
Colaboradores:   id, nome, funcao, tel, ativo
Parceiros:       id, nome, tipo(arquiteto|engenheiro|fornecedor|prestador), contato, comissaoPct, ativo
Obras:           id, clienteId, tipoServicoId, status(andamento|encerrada|cancelada),
                 enderecoObra, dtInicio, dtPrevisao, dtFim, valorContrato
ObraEquipe:      obraId, colaboradorId?|parceiroId?, papel, comissaoPct (snapshot do cadastro)
ObraEtapas:      id, obraId, nome, ordem, status, dtInicioPrevista, dtPrevista, dtRealizada, responsavelId
ParcelasReceber: id, obraId, descricao, valor, vencimento, pagoEm?, status(pendente|pago|atraso|cancelado)
ParcelasPagar:   id, obraId, beneficiarioTipo, beneficiarioId, descricao(comissão|material|mao-de-obra),
                 valor, vencimento, pagoEm?, status
Contratos:       id, clienteId, obraId?, templateId, status(rascunho|enviado|aguardando|assinado|cancelado),
                 pdfGerado, signerUrl, assinadoEm, hashDoc
ContratoTemplates: id, nome, clausulas(versionadas), ativo
Notas:           id, obraId, texto, autorId, criadoEm
Anexos:          id, obraId, arquivo(fotos de etapas, plantas, contrato assinado)
ConfigSistema:   chave, valor   (ex: kanban.alertaPct=70 — editável por Admin)
```

## Kanban de obra (equipe)

- **Colunas:** `A iniciar → Iniciada → Em andamento → Finalizada → Entregue` — cards = ObraEtapas, drag-drop; entrar em Finalizada/Entregue grava dtRealizada
- **Cor do card = saúde do prazo** (computada, não persiste):

```
%decorrido = (hoje − dtInicioPrevista) / (dtPrevista − dtInicioPrevista)
⚪ branca:   a_iniciar E hoje < dtInicioPrevista
🟢 verde:    iniciada/andamento E %decorrido < alertaPct (default 70)
🟡 amarela:  não concluída E %decorrido ≥ alertaPct
             OU a_iniciar E hoje ≥ dtInicioPrevista
🔴 vermelha: não concluída E hoje > dtPrevista
🔵 azul:     finalizada/entregue
```

## Assinatura de contratos — provider pluggable

Decisão postergada. `ISignatureProvider`: `ZapSignProvider` (SaaS BR ~R$1-2/doc, recomendada), `DocuSealProvider` (OSS self-hosted), `ManualProvider` (upload do PDF assinado — padrão até a decisão). O portal exibe o contrato/link de assinatura independente do provider.

## Expansões aprovadas (roadmap)

- Etapas-template por TipoServico ✓ (no modelo)
- QR code na obra → colaborador conclui etapa com foto obrigatória
- Fotos por etapa → portal + "antes-e-depois" automático
- Câmera IP na obra (fase posterior): snapshots agendados → timeline no portal; streaming RTSP fora de escopo inicial; requer aviso de filmagem à equipe
- Margem por obra (receber − pagar), motivo de perda no funil, dashboard gerencial
- Canal de garantia/pós-venda no portal

## Fases

| # | Escopo |
|---|---|
| 0 | Scaffold .NET 10 + Blazor + **mock navegável do admin** (sidebar auto-hide, grids, funil, kanban estático, seletor de role) ✓ em andamento |
| 1 | Identity + SQLite + roles/claims + políticas reais |
| 2 | Cadastros base (TiposServico+Templates, Colaboradores, Parceiros, Clientes) + funil de leads + portal read-only (acompanha negociação) |
| 3 | Obras + equipe + Kanban drag-drop com cores + QR/foto por etapa + portal mostra etapas |
| 4 | Financeiro completo (receber+pagar+comissões) + margem por obra |
| 5 | Contratos + templates + PDF + ISignatureProvider (Manual → provider real) |
| 6 | Câmeras/snapshots, garantia/pós-venda, dashboard, antes-e-depois |

## API pública — LP vira mini-CMS (brainstorm erp-roadmap)

**Leads da LP → funil (decisão aprovada):** o formulário da LP posta direto no ERP — lead nasce no funil estágio "Novo", não só no WhatsApp.

```
POST /api/public/leads   { nome, telefone, mensagem, origem:"lp" }
  → anti-spam: honeypot + rate limit por IP + validação server-side
  → cria Lead (funil "Novo") + opcional: dispara notificação p/ comercial
GET  /api/public/stats   → provas sociais da LP (obras entregues, tempo mercado, nota)
GET  /api/public/conteudo → seções editáveis da LP (serviços, portfólio, textos) — mini-CMS
```

**WhatsApp — Evolution API (não Meta oficial):** custo da API oficial adiado (decisão pendente com proprietário). Evolution API já está instalada na OCI (mesma infra do Batuara) e acessível via túnel SSH. Usos previstos: notificar comercial quando entra lead da LP; avisos ao cliente como CANAL AUXILIAR — o portal segue sendo a fonte da verdade.
- **Número dedicado**: novo eSIM exclusivo para a instância Evolution do Hako — nunca usar número pessoal (risco de banimento pela Meta)
- Não-usar Business Meta por ora (experiência Batuara); decisão de migração futura fica em aberto
- Segregação: conexão com Evolution via serviço interno (`IWhatsAppNotifier`) — nenhum endpoint do ERP expõe a Evolution diretamente

**Ordem sugerida de implementação:** POST /api/public/leads primeiro (destrava valor imediato: form da LP → funil), notificação WhatsApp depois.

## Dashboard — endpoints (fechado no brainstorm)

Cada widget vira dado de API; resposta filtrada pela permissão do usuário (401/403 se sem claim, nunca dado "escondido"):

```
GET /api/dashboard/resumo → {
  leadsNovos: int,        // funil.view — Leads novos (site) + orçamentos em aberto (Orcamento.status=enviado)
  clientesAtivos: int,    // clientes.view — Clientes ativos + novos no mês
  obrasAndamento: int,    // obras.view — Obras em andamento + entregues no mês
  etapasAtrasadas: int,   // obras.view — Etapas atrasadas + críticas (vermelhas)
  faturamentoMes: cents,  // financeiro.view — Faturamento do mês + % vs anterior
  comissoesPendentes: c., // financeiro.view — Comissões pendentes + nº parceiros
  receber30d: cents,      // financeiro.view — A receber 30d + nº parcelas
  pagar30d: cents         // financeiro.view — A pagar 30d + nº parcelas + vencidas
}
GET /api/dashboard/obras-por-fase → [ { fase, qtd, pctMedio } ]  // obras.view
```

Regras: valores monetários em centavos; contagens derivadas das mesmas queries das grids (fonte única); cache curto (30–60s) opcional.

## Clientes — modelo e endpoints (brainstorm erp-roadmap)

**Modelo:** tabela única `Pessoas/Clientes` com enums — `tipo: lead|cliente` (cliente = somente quem assinou contrato; promoção automática no Ganho do funil) e `status: ativo|inativo|cancelado`.

**Regras de status:**
- ativo→inativo: manual a qualquer momento OU automático (job diário) quando sem obras em andamento e última entregue há >90 dias (fim da garantia)
- cancelado: quando obra em execução é cancelada (justificativa obrigatória) — não retorna a ativo automaticamente
- lead usa o mesmo enum de status

**Endpoints:**
```
GET    /api/clientes?page=1&size=10&q=&tipo=&status= → { items:[{id,nome,celular,tipo,status,
            obrasAtivas, obraResumo, temAtraso}], total, page, pages }   // paginação offset — casa com o pager ⏮◀▶⏭ da UI; cursor só se volume justificar
GET    /api/clientes/{id}
POST   /api/clientes            → criar (lead ou cliente)
PUT    /api/clientes/{id}       → editar
POST   /api/clientes/{id}/status → { status, motivo? }  // nunca DELETE; motivo obrigatório p/ inativar/cancelar
GET    /api/clientes/{id}/obras → resumo p/ ícone+contador do grid
POST   /api/clientes/{id}/portal-token/rotate
```

Campos: id, nome, celular, email?, enderecoObra?, cpfCnpj? (só cliente — LGPD), tipo, status, origem(lp|instagram|indicacao|parceiro|outro), portalToken, criadoEm, alteradoEm, statusEm, motivoStatus?

## Funil — modelo por NEGOCIAÇÃO (preview aprovado no brainstorm)

Propósito: funil **básico por design** — visualização fácil e rápida da situação; objetivo é ajudar a converter lead em cliente, não ser um CRM pesado. **Controle puramente visual dos estágios.**

- **Sem edição pelo funil:** clicar no card não permite alterar nenhuma informação (nome do cliente, detalhes do serviço, valor etc.) — qualquer alteração de dados é feita na seção Clientes (ou Contratos); o funil serve só para ver/mover estágio
- **Só avança:** card nunca "volta" de fase — Orçamento não retorna a Visita/Medição, Negociação não retorna a Orçamento; drag&drop permitido apenas para colunas à frente (únicas saídas: Ganho ou Perdido/cancelada)

Card = `Negociacao`, nunca a pessoa diretamente. Uma pessoa pode ter N negociações **simultâneas** — um lead/cliente pode ter **vários cards ativos no funil ao mesmo tempo**, um por serviço em negociação.
- **Separação por serviço é obrigatória:** serviços distintos negociam-se em cards separados mesmo quando se relacionam na prática — ex.: marcenaria (armários) e reforma da cozinha são duas negociações independentes, ainda que os armários sejam para a cozinha em reforma
- Quando uma das negociações vira Ganho e a pessoa vira `cliente`, os **demais cards ativos** dela passam a exibir o badge ★ cliente (e o toggle/filtro os trata como recorrência)
- **Status da Negociacao:** `em_andamento` | `cancelada` | `fechada` (independente do estágio/coluna)
- Pessoa `tipo=lead` vira `cliente` **somente com negociação fechada E contrato assinado** (entrada na coluna Ganho) — e **nunca retorna a lead**
- **Ganho ≠ fechada:** o card só entra na coluna Ganho **depois do contrato assinado** — negociação fechada aguardando assinatura permanece na coluna Negociação com status `fechada` e a pessoa **continua lead** até assinar
- Cancelada: sai do fluxo ativo p/ Perdido/arquivo com motivoPerda obrigatório
- **motivoPerda (categoria + submotivo):**
  - `desacordo_comercial` → submotivo: `valor` | `prazo` | `ambos` | `outro`
  - `fechou_concorrencia` (fechou com outra empresa) → submotivo: `valor` | `prazo` | `ambos` | `outro`
  - `sem_resposta` (lead/cliente não responde aos contatos realizados) — sem submotivo
  - Modelo: Negociacao ganha `motivoPerda` + `submotivoPerda?`; "outro" exige observação livre
- **Cancelar → reabrir:** negociação que esfria (ex.: orçamento recusado) é cancelada e, **a partir do cancelamento**, abre-se uma nova negociação para a mesma pessoa — ex.: novo orçamento, ou orçamento reajustado após nova medição. O cancelamento oferece a opção "nova negociação" já vinculando a anterior (histórico na Aba3 do cliente); a nova nasce no estágio adequado (ex.: Orçamento ou Visita/Medição), nunca "retrocedendo" o card antigo
- **Histórico do cliente:** **todo** evento do funil é registrado na timeline da pessoa (Aba3) — criação do card, cada avanço de estágio, cancelamento + motivoPerda, reabertura vinculada, fechamento e contrato assinado/ganho — sempre com data e usuário responsável; nada no funil acontece sem rastro auditável
- **Período:** colunas ativas (Novo→Negociação) são **atemporais** — card em andamento nunca some por idade (alerta visual "⚠ parado há X dias" em vez disso); **Ganho e Perdido exibem só o período selecionado** na toolbar (padrão: mês vigente; opções: mês anterior | trimestre | ano | tudo) — métricas de conversão usam o mesmo seletor
- Cliente existente entrando em nova negociação aparece no board com badge "★ cliente" — mesmo funil, visual distinto
- **Jornada visível no card (obrigatória):** todo card identifica de imediato qual processo está correndo — **"Lead → 1º contrato"** (conversão lead→cliente) ou **"★ Cliente → novo contrato"** (recorrência: projeto/obra/reforma/serviço adicional). É a leitura nº 1 do board: distinguir aquisição de recorrência sem abrir nada
- **Recorrência:** o funil acompanha também negociação de projetos, obras, reformas e serviços p/ **clientes** (não só conversão de lead) — visualização simples: badge ★ + `tipoServico` no card e **filtro por tipo** (projeto|obra|reforma|serviço) + toggle "só leads / só clientes / todos", sem colunas ou boards separados
- Funil mede conversão em dois níveis: lead→cliente (1ª vez) e proposta→contrato (recorrente)
- Negociacao: id, pessoaId, estagio, status, tipoServico (obrigatório — o card é por serviço), valor?, origem, motivoPerda?, criadoEm, fechadaEm?, ganhoEm? → no Ganho cria Obra (etapas-template)

### Artefatos da negociação (Visita e Orçamento são registros, não só colunas)

- `Visita`: id, negociacaoId, dataAgendada, dataRealizada?, status(`agendada`|`realizada`|`cancelada`), observacoes?, criadoEm
- `Orcamento`: id, negociacaoId, versao(1,2,3…), valor, status(`rascunho`|`enviado`|`aceito`|`recusado`|`substituido`), arquivo?(PDF), enviadoEm?, respondidoEm?, criadoEm
- Ambos vinculados a **uma** negociação (nunca soltos na pessoa); cada evento (visita agendada/realizada, orçamento criado/enviado/aceito/recusado) entra na timeline da Aba3
- **Semântica das colunas** (o estágio reflete o estado dos artefatos; avanço continua manual via drag&drop, não automação):
  - `Visita/Medição` = visita agendada/realizada e nenhum orçamento enviado
  - `Orçamento` = orçamento `enviado` aguardando resposta
  - `Negociação` = orçamento em ajuste/aceito, aguardando fechamento + contrato
- **Cancelar → reabrir (cadeia de versões):** a nova negociação referencia a anterior; o novo orçamento segue a `versao` da cadeia e o antigo fica `substituido` — histórico completo na Aba3
- Funil segue **read-only para dados** (não edita pessoa/serviço/valor pelo board); a **única** interação além de visualizar é o avanço de estágio, que dispara o modal da ação correspondente (grava na Aba3 — ver "Gatilhos de avanço")

### Gatilhos de avanço (regra de negócio de cada transição — movimento manual, só p/ frente)

| Transição | Gatilho (o que aconteceu no mundo real) |
|---|---|
| Novo → Contato | Primeiro contato realizado — ao arrastar, **modal de confirmação obrigatório**: "Confirma que o contato com este lead foi realizado?" |
| Contato → Visita/Medição | Ao arrastar, **modal de agendamento** — registra a `Visita` (data/obs) na Aba3 do cliente; sem visita agendada, o card não avança |
| Visita/Medição → Orçamento | Ao arrastar, **modal de orçamento** — registra/anexa o orçamento enviado (valor + PDF) na Aba3; sem orçamento `enviado`, o card não avança |
| Orçamento → Negociação | Ao arrastar, **modal de contrato** — exige confirmar/vincular o **contrato enviado** p/ assinatura; sem contrato enviado, o card não avança |
| Negociação → Ganho | Negociação `fechada` + **contrato assinado** |
| Qualquer → Perdido | Cancelamento com `motivoPerda` |

**Avanço amarrado à ação:** mover o card no funil **sempre abre o modal da ação correspondente**, que grava o artefato na Aba3 (agenda de visitas, orçamentos, contrato) e na timeline — o board não edita dados da pessoa, mas o avanço de estágio **exige** o registro do artefato. Sem preencher o modal, o card volta à coluna de origem (não avança).

**Regras da coluna Novo e origem dos cards:**
- Coluna **Novo** recebe **apenas** leads que enviaram mensagem pela **LP** (form do site) — é a única origem que inicia em Novo
- **Novo → Contato é irreversível:** não se retrocede de Contato p/ Novo (coerente com "só avança")
- **Cliente recorrente** (já é cliente, nova proposta) **inicia direto em Contato** — nunca passa por Novo
- **Leads captados fora da LP** (email, WhatsApp, ligação, cartão de visita, contato pessoal): cadastro manual na tela **Clientes** (tipo=lead) e o card **já nasce em Contato** — afinal o contato já aconteceu na captação

### Clientes — card CRUD (UI)
- tipo = badge readonly derivado (lead→cliente só via negociação fechada + contrato assinado; carga legada = fase posterior/Admin)
- obrigatórios: nome*, celular* (máscara); opcionais: email, enderecoObra, origem, observações
- cpfCnpj só visível/obrigatório quando tipo=cliente (minimização LGPD em leads)
- alteração de status no card: select + motivo obrigatório p/ inativo|cancelado
- bloco "Portal": link /obra/{token} + [Regenerar] + expiração — só cliente existente

### Clientes — card ampliado por ABAS (em discussão, seção ABERTA)
- Aba1 Dados pessoais: nome*, celular*, tel2?, email?, endereço completo (rua/nº/compl/bairro/cidade/UF/CEP — contrato exige), origem, obs
- Aba2 Documentos/Contrato: cpf, rg+órgão, estadoCivil?, profissão?, nacionalidade? — alimenta templates; obrigatórios só ao emitir contrato
- Aba3 Negociações: timeline auditável da pessoa — todos os cards do funil + cada evento (criação, avanço de estágio, visita agendada/realizada, orçamento criado/enviado/aceito/recusado, cancelamento+motivo, reabertura vinculada, fechamento, ganho) com data e usuário
  - **Ações operacionais na Aba3** (aqui, nunca no funil): `[Agendar visita]`, `[Registrar visita realizada]` (data+observações), `[Novo orçamento]` / `[Enviar orçamento]` (valor + upload PDF + marcar enviado/aceito/recusado) — sempre no contexto de uma negociação da pessoa (1 ativa → pré-selecionada; N ativas → escolhe o card)
- Aba4 Obras: obras c/ saúde+fase+atalho p/ kanban
- Aba5 Portal: link token, regenerar, expiração, chamados garantia
- Status: select ativo|inativo|cancelado + motivo obrigatório (exceto ativo)
- SEÇÃO CLIENTES NÃO FECHADA — aguarda revisão/abas adicionais; seções revisitáveis durante desenvolvimento
- Aba6 Contratos: contratos emitidos p/ a pessoa + status + obra associada a cada contrato
- Aba7 Financeiro: budget total (soma valorContrato das obras) + extrato de recebimentos/pagamentos ligados ao cliente (parcelas por obra, status pago|pendente|vencido, saldo)
- Aba Portal do cliente: gerencia acesso /obra/{token} (link, regerar, expiração, chamados garantia) — refinar em dev
- Aba8 Mensagens (CRM): histórico unificado de conversas com o cliente — envio/recebimento WhatsApp via Evolution API hoje, atrás de interface IMessagingProvider p/ migrar p/ Meta Business oficial sem trocar a UI; guardar também notas internas da equipe
- Status SUSPENSO: aplica-se a Obra (motivo legal/ex.: embargada, inadimplência, pedido do cliente) — obra suspensa pausa etapas/kanban e prazos; cliente pode refletir badge derivado da obra suspensa, mas suspensão é da obra, não da pessoa; retomada exige motivo+data
- Suspensão em DOIS níveis: Obra (motivo próprio) OU Cliente (ex.: desacordo comercial) — suspender o cliente suspende TODAS as suas obras em cascata (badge + pausa de prazos/kanban); retomada também em cascata; sempre com motivo+data auditável

## Cronograma (novo item do sidebar — Gantt estilo MS Project)
- Nova seção "Cronograma": gráfico de Gantt das ObraEtapas — mesmas datas do Kanban (dtInicioPrevista/dtPrevista/dtRealizada), uma linha por etapa, barras coloridas pela mesma saúde de prazo, marco de hoje, zoom semana/mês
- Opcional fase posterior: dependências entre etapas (predecessora), baseline previsto vs real, visão multi-obra (linha por obra)
- Endpoint: GET /api/obras/{id}/cronograma → etapas ordenadas com datas e saúde; drag de barra = ajuste de dtPrevista (perm obras.edit)

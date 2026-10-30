# Hako — Spec Backend "Controle de Projeto"

Spec consolidada do brainstorm (out/2026). Backend interno da Hako Interiores: clientes, serviços, kanban, cronograma (Gantt), financeiro, contratos, assinatura e **portal do cliente** (diferencial central).

**Visão de produto (MVP + potencial SaaS):** este projeto nasce como um **MVP funcional para apresentação comercial à Hako Interiores**, e já é concebido com arquitetura e domínio aderentes para **escalar para outras empresas do mesmo segmento** (interiores, marcenaria, reformas e pequenas obras). Detalhes finos de operação serão calibrados junto ao cliente após validação comercial da proposta, mantendo o MVP enxuto, visualmente forte e flexível.

**Termo de domínio:** o que as sessões anteriores chamavam de **Obra** passa a **Serviço**. "Obra" não cobre o que a Hako executa — reforma de alvenaria (cozinha, lavabo, banheiro, construção, ampliação) e móveis/mobiliário (cozinha, quarto, dormitório). Seções, claims, modelo e textos do admin usam Serviço. A rota pública `/obra/{token}` não foi revista nesta sessão.

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

- URL com token único por lead/serviço: `app.hakointeriores.com.br/obra/{token}`
- Cliente recebe o link **logo após o primeiro contato** (por qualquer canal) e acompanha TUDO:
  - **Negociação:** status do orçamento → contrato para assinar na própria página
  - **Serviço:** etapas em visão Kanban read-only com cores de saúde + fotos do andamento
  - **Entregue:** termo de entrega, canal de garantia/pós-venda
- Nada de Evolution API/WhatsApp API como canal obrigatório — portal é a fonte da verdade
- Gancho de marketing na LP: "acompanhe seu serviço online, do contrato à entrega" (iteração futura)

### Portal — modelo de acesso

- Token na URL gerado ao criar lead/serviço (ex.: `/obra/a8f3k2m9x7`), enviado por qualquer canal — sem login, sem senha, sem conta
- Token ≥192 bits de entropia, um por lead/serviço, opaco (sem IDs sequenciais)
- Acesso **read-only**: cliente só vê o próprio serviço (etapas com cores, fotos, status do orçamento/contrato); únicas ações: assinar contrato (via provider, que re-autentica por e-mail/tel) e abrir chamado de garantia
- Revogável/rotacionável pelo Admin; expiração opcional (ex.: 90 dias pós-entrega) ou permanente
- Página evita dados sensíveis (sem CNPJ/CPF/valores expostos além do necessário); rate limit na rota + HTTPS
- Ressalva conhecida: posse do link = acesso; por isso dados minimizados e ações restritas

## RBAC e acesso

**Dois mundos:**
- **Interno** (`/dashboard`): Identity + roles + claims `perm="recurso.ação"` (Admin, Comercial, Financeiro, Serviços)
- **Externo** (`/obra/{token}`): cliente/colaborador/parceiro via token por vínculo — sem login/conta

Claims por role: Admin→todas; Comercial→`clientes.*, funil.*, contratos.*, servicos.view`; Financeiro→`financeiro.*, clientes.view, servicos.view`; Serviços→`servicos.*, clientes.view`. Policies registradas por claim; verificadas no backend E nos widgets. Nenhuma dessas duas roles operacionais recebe `financeiro.*` — a aba financeira fica invisível para elas (ver "Abas e autorização").

**Repo:** monorepo — backend .NET em `backend/` (`.sln`+Dockerfile próprios); frontend flat na raiz (deploy inalterado); rsync exclui `backend/`, spec, `*.key`, `*.crt`, `gitactions.err` ✓ aplicado.

## Dashboard (único, adaptativo por permissão)

- Um `Dashboard.razor`: widgets renderizam só se `perm` permite — Financeiro vê financeiro, Serviços vê serviços, mesma tela
- Widgets iniciais: total clientes, serviços em andamento, faturamento mês, contas a receber/pagar, comissões pendentes, etapas atrasadas, leads novos
- Tema claro/escuro via CSS vars + `.dark` no root + toggle na topbar (localStorage)
- Layout: sidebar auto-oculta + topbar + área de conteúdo por seção

## UI/UX do admin — navegação e padrões de tela

**Mobile-first (design system):** toda a UI/UX é pensada **mobile first** — em vários cenários o acesso ao Dashboard será feito **direto do local do serviço**. Três breakpoints de referência: **mobile · tablet · notebook/desktop**. Componentes, grids, cards-modais e boards (funil/kanban) devem funcionar plenamente nos três — ex.: board vira scroll horizontal por coluna no mobile, grid vira lista de cards, modais viram tela cheia. Decisão de design system do projeto, vale para todas as seções.

**Sidebar auto-oculta (sem JS):** colapsada por padrão mostrando só o rail de ícones; expande em overlay no `:hover`; mouse sai sem selecionar → recolhe mantendo a seção ativa; selecionou → navega e recolhe. Item ativo destacado no rail. Mobile: drawer via hambúrguer.

**Seções (topbar exibe o nome da seção ativa):** Dashboard · Clientes · Funil · Serviços · Kanban · Cronograma · Financeiro · Contratos · Equipe · Config — cada item filtrado por `perm`; seção sem implementação exibe placeholder com o nome.

**Padrão grid (CRUD):** toolbar com `[+ Novo]` à esquerda e paginação ⏮ ◀ ▶ ⏭ à direita; clicar na linha abre o mesmo card-modal da inclusão (novo → [Salvar][Cancelar]; existente → [Salvar][Inativar|Cancelar…][Voltar]). **Nunca há exclusão de dados** — só inativação ou cancelamento; cancelamentos exigem justificativa (lançamentos, serviços, contratos). Usuário logado fica na topbar (à direita) com dropdown: *Meu perfil* (modal: usuário, nome completo, senha ×2 com regras) e *Sair*. Tema claro/escuro no rodapé da sidebar. Boards (funil/kanban) com drag&drop entre colunas.

- **Clientes** — colunas: status (Lead|Cliente) + ativo/inativo, ícone de serviço (em andamento 🛠 / entregue ✓ / cancelada ✗, +contador se >1), badge de atraso se houver etapa vermelha, nome, celular (contato direto sem abrir card)
  - **Pesquisa:** campo de busca na toolbar, entre `[+ Novo]` e a paginação — busca por nome, celular ou email (sem abrir card)
  - **Filtro:** dropdown/painel ao lado da busca com opções: tipo (todos | leads | clientes), status (ativo | inativo | cancelado | suspenso), origem (lp | instagram | indicacao | parceiro | outro), com serviço em andamento, com etapa atrasada (badge 🔴), com/sem serviços — combináveis entre si e com a busca
- **Equipe** (unificada) — Funcionários + Colaboradores + Parceiros num único grid, badge de perfil por linha; comissão% só para parceiros
- **Financeiro** — grid único pagar+receber: ícone direção (▲ pagar / ▼ receber), status (pago ✓ / pendente / vencido 🔴), descrição, beneficiário/cliente, valor, vencimento
- **Contratos** — abas: *Modelos* (ContratoTemplates) e *Emitidos* (contratos por cliente com status enviado/aguardando/assinado)
- **Serviços** — grid confirmado (não é catálogo de tipos — tipos ficam em Config/TiposServico): saúde, tipo, cliente, fase atual, % (percentual do serviço, calculado ou ajustado), previsão (= data de entrega). Busca e filtros na toolbar — ver seção Serviços
- **Funil** — colunas do pipeline com cards de lead: Novo → Contato → Visita/Medição → Orçamento → Negociação → Ganho | Perdido
- **Kanban** — seletor no topo lista os **serviços em andamento**; o board exibe os cards das etapas/tarefas (e sub-tarefas) do serviço selecionado nas 5 colunas, com cores por saúde do prazo (drag-drop real na impl.; no mock, cards estáticos)

**Mock atual:** dados estáticos + seletor "visualizar como" (simula role sem Identity real). Serve para validar concepção e navegação antes da Fase 1 de auth.

**Sessão ↔ LP:** logado, o app grava cookie-marcador `hako_auth=1` (não-HttpOnly, sem valor de credencial — só dica visual; se forjado, o pior caso é exibir link que cai no login). A LP lê o cookie e exibe no menu `[Dashboard]` (`/dashboard`) + `Sair` (`/dashboard/conta/sair` → endpoint Identity que faz signout, limpa o marcador e redireciona à LP). No admin: "Voltar ao site" no rodapé da sidebar + "Sair" na topbar (rota/URL do site via config `SiteUrl`).

## Modelo de dados

```
Users (Identity) + Roles
TiposServico:    id, nome, categoria(alvenaria|mobiliario), ativo
TemplateEtapa:   id, tipoServicoId, nome, ordem, duracaoDias   → serviço nasce com etapas+prazos prontos
Clientes:        id, nome, tel, email, endereco, status(prospecto|ativo|cancelado), origem, portalToken
Leads (funil):   Novo → Contato → Visita/Medição → Orçamento → Negociação → Ganho(vira cliente+serviço) | Perdido(motivo)
Negociacoes:     id, pessoaId, estagio, status(em_andamento|cancelada|fechada), tipoServicoId,
                 valor?, origem, motivoPerda?, criadoEm, fechadaEm?, ganhoEm?
Visitas:         id, negociacaoId, dataAgendada, dataRealizada?, status(agendada|realizada|cancelada),
                 medicoes?(ambientes/metragens levantadas), observacoes?
Orcamentos:      id, negociacaoId, visitaId?, versao, valor, condicoesPagamento?, prazoExecucaoDias?,
                 escopoResumo?, status(rascunho|enviado|aceito|recusado|substituido),
                 arquivo?, enviadoEm?, respondidoEm?
Colaboradores:   id, nome, funcao, tel, ativo
Parceiros:       id, nome, tipo(arquiteto|engenheiro|fornecedor|prestador), contato, comissaoPct, ativo
Servicos:        id, clienteId (imutável), tipoServicoId (imutável), negociacaoId?,
                 status(andamento|suspenso|encerrada|cancelada),
                 modoExecucao(no_local|oficina),
                 enderecoExecucaoOrigem?(cliente|informado), enderecoExecucao?,
                 instalacaoOrigem?(cliente|informado), enderecoInstalacao?,
                 dtInicio, dtInicioEfetivo?, dtEntrega, valorContrato,
                 ajustePctDelta?(0 se nunca ajustou)
AjustePercentual: id, servicoId, exibidoDe, exibidoPara, calculadoNoMomento,
                 deltaResultante, motivo?, usuarioId, quando
ServicoEquipe:   servicoId, colaboradorId?|parceiroId?, papel, comissaoPct (snapshot do cadastro)
ServicoEtapas:   id, servicoId, parentId? (sub-tarefa), predecessoraId? (depende de conclusão anterior),
                 nome, ordem, status, dtInicioPrevista, dtPrevista, dtRealizada, responsavelId
                 duracao = dtPrevista − dtInicioPrevista (dias)
ParcelasReceber: id, servicoId, descricao, valor, vencimento, pagoEm?, status(pendente|pago|atraso|cancelado)
ParcelasPagar:   id, servicoId, beneficiarioTipo, beneficiarioId, descricao(comissão|material|mao-de-obra),
                 valor, vencimento, pagoEm?, status
Contratos:       id, clienteId, negociacaoId, orcamentoId (aprovado), servicoId? (vinculado no Ganho),
                 templateId, status(rascunho|enviado|aguardando|assinado|cancelado),
                 textoRenderizado, pdfGerado, signerUrl, assinadoEm, hashDoc
ContratoTemplates: id, tipoServicoId, nome, corpoTexto (texto padrão + {{campos}}), versao, ativo
Notas:           id, servicoId, texto, autorId, criadoEm
Anexos:          id, servicoId, etapaId?, tipo(foto|documento), momento?(antes|andamento|depois),
                 visivelPortal(bool), arquivo, ativo
ConfigSistema:   chave, valor   (ex: kanban.alertaPct=70 — editável por Admin)
```

## Kanban de serviço (equipe)

- **Seletor de serviço:** a caixa de seleção no topo do Kanban lista efetivamente os **serviços em andamento** (`status = andamento`), cadastrados na seção **Serviços**.
- **Quadro (board):** exibe em formato de cards as etapas/tarefas (`ServicoEtapas`) do serviço selecionado para avançarem pelas colunas. Sem etapa cadastrada, exibe o card único do serviço (janela `dtInicio` → `dtEntrega`).
- **Colunas e percentual por coluna:**
  - `A iniciar` = **0%**
  - `Iniciada` = **10%** (grava `dtInicioReal` na etapa e, se for a 1ª do serviço, preenche `dtInicioEfetivo` no serviço caso esteja vazio)
  - `Em andamento` = **50%**
  - `Finalizada` = **90%** (grava `dtRealizada` na etapa/sub-tarefa)
  - `Entregue` = **100%** (grava/confirma `dtRealizada`)
- **Dependência entre tarefas (precedência):** um serviço possui tarefas e algumas tarefas só podem iniciar depois que a tarefa anterior (`predecessoraId`) for concluída. A tarefa seguinte é liberada para sair de `A iniciar` quando a tarefa anterior chega em **Finalizada** (ou **Entregue**, caso a anterior seja uma tarefa pai com sub-tarefas).
- **Tarefas com sub-tarefas (hierarquia Pai → Filhas):**
  - Quando uma tarefa possui sub-tarefas (`parentId`), a **tarefa pai** fica estacionada na coluna **Iniciada** e os cards das **tarefas filhas** são exibidos **abaixo dela**.
  - As **tarefas filhas** são movimentadas pelas colunas **Em andamento** (50%) e **Finalizada** (90%). Enquanto está em `Iniciada`, o `%` da tarefa pai reflete a média ponderada pela duração das suas sub-tarefas (piso de 10%, chegando a 90% quando todas as filhas estão em `Finalizada`).
  - Enquanto houver sub-tarefa não finalizada, a tarefa pai permanece bloqueada em **Iniciada**.
  - Quando **todas as tarefas filhas forem finalizadas**, a **tarefa pai** é liberada para ser movida para a coluna **Entregue** (**100%**).
  - Na coluna **Entregue**, os cards das tarefas filhas **não aparecem mais abaixo** da tarefa pai (recolhidos do board), mas **devem ser exibidos ao passar o mouse por cima (`:hover` / tooltip — ou toque no mobile)** da tarefa pai, mostrando a relação das sub-tarefas concluídas.
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
- QR code no serviço → colaborador conclui etapa com foto obrigatória
- Fotos por etapa → portal + "antes-e-depois" automático
- Câmera IP no local do serviço (fase posterior): snapshots agendados → timeline no portal; streaming RTSP fora de escopo inicial; requer aviso de filmagem à equipe
- Margem por serviço (receber − pagar), motivo de perda no funil, dashboard gerencial
- Canal de garantia/pós-venda no portal

## Fases

| # | Escopo |
|---|---|
| 0 | Scaffold .NET 10 + Blazor + **mock navegável do admin** (sidebar auto-hide, grids, funil, kanban estático, seletor de role) ✓ em andamento |
| 1 | Identity + SQLite + roles/claims + políticas reais |
| 2 | Cadastros base (TiposServico+Templates, Colaboradores, Parceiros, Clientes) + funil de leads + portal read-only (acompanha negociação) |
| 3 | Serviços + equipe + Kanban drag-drop com cores + QR/foto por etapa + portal mostra etapas |
| 4 | Financeiro completo (receber+pagar+comissões) + margem por serviço |
| 5 | Contratos + templates + PDF + ISignatureProvider (Manual → provider real) |
| 6 | Câmeras/snapshots, garantia/pós-venda, dashboard, antes-e-depois |

## API pública — LP vira mini-CMS (brainstorm erp-roadmap)

**Leads da LP → funil (decisão aprovada):** o formulário da LP posta direto no ERP — lead nasce no funil estágio "Novo", não só no WhatsApp.

```
POST /api/public/leads   { nome, telefone, mensagem, origem:"lp" }
  → anti-spam: honeypot + rate limit por IP + validação server-side
  → cria Lead (funil "Novo") + opcional: dispara notificação p/ comercial
GET  /api/public/stats   → provas sociais da LP (serviços entregues, tempo mercado, nota)
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
  servicosAndamento: int, // servicos.view — Serviços em andamento + entregues no mês
  etapasAtrasadas: int,   // servicos.view — Etapas atrasadas + críticas (vermelhas)
  faturamentoMes: cents,  // financeiro.view — Faturamento do mês + % vs anterior
  comissoesPendentes: c., // financeiro.view — Comissões pendentes + nº parceiros
  receber30d: cents,      // financeiro.view — A receber 30d + nº parcelas
  pagar30d: cents         // financeiro.view — A pagar 30d + nº parcelas + vencidas
}
GET /api/dashboard/servicos-por-fase → [ { fase, qtd, pctMedio } ]  // servicos.view
```

Regras: valores monetários em centavos; contagens derivadas das mesmas queries das grids (fonte única); cache curto (30–60s) opcional.

## Clientes — modelo e endpoints (brainstorm erp-roadmap)

**Modelo:** tabela única `Pessoas/Clientes` com enums — `tipo: lead|cliente` (cliente = somente quem assinou contrato; promoção automática no Ganho do funil) e `status: ativo|inativo|cancelado`.

**Regras de status:**
- ativo→inativo: manual a qualquer momento OU automático (job diário) quando sem serviços em andamento e último entregue há >90 dias (fim da garantia)
- cancelado: quando serviço em execução é cancelado (justificativa obrigatória) — não retorna a ativo automaticamente
- lead usa o mesmo enum de status

**Endpoints:**
```
GET    /api/clientes?page=1&size=10&q=&tipo=&status= → { items:[{id,nome,celular,tipo,status,
            servicosAtivos, servicoResumo, temAtraso}], total, page, pages }   // paginação offset — casa com o pager ⏮◀▶⏭ da UI; cursor só se volume justificar
GET    /api/clientes/{id}
POST   /api/clientes            → criar (lead ou cliente)
PUT    /api/clientes/{id}       → editar
POST   /api/clientes/{id}/status → { status, motivo? }  // nunca DELETE; motivo obrigatório p/ inativar/cancelar
GET    /api/clientes/{id}/servicos → resumo p/ ícone+contador do grid
POST   /api/clientes/{id}/portal-token/rotate
```

Campos: id, nome, celular, email?, endereco? (cadastral da pessoa — contrato; o endereço de execução fica no serviço), cpfCnpj? (só cliente — LGPD), tipo, status, origem(lp|instagram|indicacao|parceiro|outro), portalToken, criadoEm, alteradoEm, statusEm, motivoStatus?

## Funil — modelo por NEGOCIAÇÃO (preview aprovado no brainstorm)

Propósito: funil **básico por design** — visualização fácil e rápida da situação; objetivo é ajudar a converter lead em cliente, não ser um CRM pesado. **Controle puramente visual dos estágios.**

- **Sem edição pelo funil:** clicar no card não permite alterar nenhuma informação (nome do cliente, detalhes do serviço, valor etc.) — qualquer alteração de dados é feita na seção Clientes (ou Contratos); o funil serve só para ver/mover estágio
- **Só avança, sem saltos:** card nunca "volta" de fase e **não pode pular colunas** — avanço é sempre para a **próxima coluna adjacente** (ex.: Visita/Medição → Orçamento, nunca Visita/Medição → Negociação ou direto p/ Ganho); drag&drop permitido apenas para a coluna seguinte (únicas saídas: Ganho ou Perdido/cancelada). Objetivo: manter o CRM vivo e ativo — cada estágio registrado, nenhum atalho que esvazie o pipeline
- **Affordance de destino (UI):** ao iniciar o arraste (ou clique-mover no mobile), **apenas a coluna seguinte** e Perdido acendem em verde/destaque; todas as demais ficam esmaecidas/bloqueadas (sem aceitar drop) — o usuário vê de antemão o único avanço possível, sem ser induzido ao erro; soltar fora de destino válido cancela o movimento

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
- **Jornada visível no card (obrigatória):** todo card identifica de imediato qual processo está correndo — **"Lead → 1º contrato"** (conversão lead→cliente) ou **"★ Cliente → novo contrato"** (recorrência: novo serviço — alvenaria ou mobiliário). É a leitura nº 1 do board: distinguir aquisição de recorrência sem abrir nada
- **Recorrência:** o funil acompanha também negociação de novos serviços p/ **clientes** (não só conversão de lead) — visualização simples: badge ★ + `tipoServico` no card e **filtro por tipo** (TiposServico / categoria alvenaria|mobiliario) + toggle "só leads / só clientes / todos", sem colunas ou boards separados
- Funil mede conversão em dois níveis: lead→cliente (1ª vez) e proposta→contrato (recorrente)
- Negociacao: id, pessoaId, estagio, status, tipoServico (obrigatório — o card é por serviço), valor?, origem, motivoPerda?, criadoEm, fechadaEm?, ganhoEm? → no Ganho cria Serviço (etapas-template); o cliente do serviço nasce da pessoa da negociação e não se troca depois

### Artefatos da negociação (Visita/Medição e Orçamento alimentam o Contrato)

- `Visita`: id, negociacaoId, dataAgendada, dataRealizada?, status(`agendada`|`realizada`|`cancelada`), **medicoes?** (ambientes, medidas em metros/m² e especificações técnicas levantadas no local), observacoes?, criadoEm
- `Orcamento`: id, negociacaoId, visitaId?, versao(1,2,3…), **valor**, **condicoesPagamento?**, **prazoExecucaoDias?**, **escopoResumo?**, status(`rascunho`|`enviado`|`aceito`|`recusado`|`substituido`), arquivo?(PDF), enviadoEm?, respondidoEm?, criadoEm
- **Vínculo direto com o Contrato:** as informações de **valores, condições de pagamento e prazo** do contrato vêm obrigatoriamente do **Orçamento aprovado pelo cliente (`status = aceito`)**, e os detalhes de escopo/ambientes vêm das **Medições registradas na `Visita`** (ambos na Aba3 do cliente). Não se digita valor solto no contrato fora do orçamento aprovado.
- Ambos vinculados a **uma** negociação (nunca soltos na pessoa); cada evento (visita agendada/realizada com medições, orçamento criado/enviado/aceito/recusado) entra na timeline da Aba3
- **Semântica das colunas** (o estágio reflete o estado dos artefatos; avanço continua manual via drag&drop, não automação):
  - `Visita/Medição` = visita agendada/realizada (com registro das medições) e nenhum orçamento enviado
  - `Orçamento` = orçamento `enviado` aguardando aprovação do cliente
  - `Negociação` = orçamento `aceito` (aprovado pelo cliente) e contrato emitido/enviado aguardando assinatura
- **Cancelar → reabrir (cadeia de versões):** a nova negociação referencia a anterior; o novo orçamento segue a `versao` da cadeia e o antigo fica `substituido` — histórico completo na Aba3
- Funil segue **read-only para dados** (não edita pessoa/serviço/valor pelo board); a **única** interação além de visualizar é o avanço de estágio, que dispara o modal da ação correspondente (grava na Aba3 — ver "Gatilhos de avanço")

### Gatilhos de avanço (regra de negócio de cada transição — movimento manual, só p/ frente)

| Transição | Gatilho (o que aconteceu no mundo real) |
|---|---|
| Novo → Contato | Primeiro contato realizado — ao arrastar, **modal de confirmação obrigatório**: "Confirma que o contato com este lead foi realizado?" |
| Contato → Visita/Medição | Ao arrastar, **modal de agendamento** — registra a `Visita` (data/obs) na Aba3 do cliente; sem visita agendada, o card não avança |
| Visita/Medição → Orçamento | Ao arrastar, **modal de orçamento** — exige visita `realizada` (com medições registradas) e registra/anexa o orçamento `enviado` (valor, condições, prazo + PDF) na Aba3; sem orçamento `enviado`, o card não avança |
| Orçamento → Negociação | Ao arrastar, **modal de aprovação de orçamento + emissão de contrato** — exige marcar o orçamento como `aceito` (aprovado pelo cliente) e emitir/vincular o **contrato enviado** p/ assinatura (puxando valores do orçamento aprovado e medidas da visita); sem orçamento aprovado e contrato enviado, o card não avança |
| Negociação → Ganho | Ao arrastar, **modal de fechamento** — exige confirmar que o contrato foi **enviado E assinado** pelo lead/cliente (vincular contrato `assinado`); cria o Serviço já com `valorContrato` e prazo vindos do orçamento aprovado |
| Qualquer → Perdido | Cancelamento com `motivoPerda` |

**Avanço amarrado à ação:** mover o card no funil **sempre abre o modal da ação correspondente**, que grava o artefato na Aba3 (agenda de visitas, orçamentos, contrato) e na timeline — o board não edita dados da pessoa, mas o avanço de estágio **exige** o registro do artefato. Sem preencher o modal, o card volta à coluna de origem (não avança).

**Regras da coluna Novo e origem dos cards:**
- Coluna **Novo** recebe **apenas** leads que enviaram mensagem pela **LP** (form do site) — é a única origem que inicia em Novo
- **Novo → Contato é irreversível:** não se retrocede de Contato p/ Novo (coerente com "só avança")
- **Cliente recorrente** (já é cliente, nova proposta) **inicia direto em Contato** — nunca passa por Novo
- **Leads captados fora da LP** (email, WhatsApp, ligação, cartão de visita, contato pessoal): cadastro manual na tela **Clientes** (tipo=lead) e o card **já nasce em Contato** — afinal o contato já aconteceu na captação

### Clientes — card CRUD (UI)
- tipo = badge readonly derivado (lead→cliente só via negociação fechada + contrato assinado; carga legada = fase posterior/Admin)
- obrigatórios: nome*, celular* (máscara); opcionais: email, endereco (cadastral), origem, observações
- cpfCnpj só visível/obrigatório quando tipo=cliente (minimização LGPD em leads)
- alteração de status no card: select + motivo obrigatório p/ inativo|cancelado
- bloco "Portal": link /obra/{token} + [Regenerar] + expiração — só cliente existente

### Clientes — card ampliado por ABAS (em discussão, seção ABERTA)
- Aba1 Dados pessoais: nome*, celular*, tel2?, email?, endereço completo (rua/nº/compl/bairro/cidade/UF/CEP — contrato exige), origem, obs
- Aba2 Documentos/Contrato: cpf, rg+órgão, estadoCivil?, profissão?, nacionalidade? — alimenta templates; obrigatórios só ao emitir contrato
- Aba3 Negociações: timeline auditável da pessoa — todos os cards do funil + cada evento (criação, avanço de estágio, visita agendada/realizada com **medições**, orçamento criado/enviado/aceito/recusado, cancelamento+motivo, reabertura vinculada, fechamento, ganho) com data e usuário
  - **Controle de Medições e Orçamentos na Aba3** (aqui, nunca no funil):
    - `[Agendar visita]` e `[Registrar visita realizada + Medições]` (data, ambientes/medidas levantadas em campo e observações técnicas — essas medições alimentam o orçamento e o contrato).
    - `[Novo orçamento]` / `[Enviar orçamento]` (versão, valor, condições de pagamento, prazo estimado em dias, resumo do escopo, upload PDF) e controle de status dos orçamentos enviados (`enviado` → `aceito` | `recusado` | `substituido`).
    - Somente o **orçamento aprovado pelo cliente (`status = aceito`)** + as **medições da visita** servem de fonte para emitir o Contrato daquela negociação.
- Aba4 Serviços: serviços c/ saúde+fase+atalho p/ kanban (`servicos.view`)
- Aba5 Portal: link token, regenerar, expiração, chamados garantia
- Status: select ativo|inativo|cancelado + motivo obrigatório (exceto ativo)
- SEÇÃO CLIENTES NÃO FECHADA — aguarda revisão/abas adicionais; seções revisitáveis durante desenvolvimento
- Aba6 Contratos: contratos emitidos p/ a pessoa + status + serviço associado a cada contrato (`contratos.view`)
- Aba7 Financeiro: **recorte do cliente** — budget total (soma `valorContrato` de todos os serviços contratados) + extrato de recebimentos/pagamentos de todos esses serviços. Cada linha identifica o serviço. Status pago|pendente|vencido, saldo. Mesma fonte da aba financeira do serviço; aqui o escopo é a pessoa inteira. Claim `financeiro.view` — sem ela a aba não renderiza e a API responde 403
- Aba Portal do cliente: gerencia acesso /obra/{token} (link, regerar, expiração, chamados garantia) — refinar em dev
- Aba8 Mensagens (CRM): histórico unificado de conversas com o cliente — envio/recebimento WhatsApp via Evolution API hoje, atrás de interface IMessagingProvider p/ migrar p/ Meta Business oficial sem trocar a UI; guardar também notas internas da equipe
- Status SUSPENSO: aplica-se ao Serviço (motivo legal/ex.: embargada, inadimplência, pedido do cliente) — serviço suspenso pausa etapas/kanban e prazos; cliente pode refletir badge derivado do serviço suspenso, mas suspensão é do serviço, não da pessoa; retomada exige motivo+data
- Suspensão em DOIS níveis: Serviço (motivo próprio) OU Cliente (ex.: desacordo comercial) — suspender o cliente suspende TODOS os seus serviços em cascata (badge + pausa de prazos/kanban); retomada também em cascata; sempre com motivo+data auditável

### Abas e autorização de visualização (Cliente e Serviço)

Perfis enxergam abas, não a ficha inteira. Cada aba declara a claim de ver e, quando edita, a claim de editar. Sem a claim de ver, a aba não entra no card e o endpoint daquela aba responde 403 — o mesmo critério dos widgets do dashboard. Vale para o card de Cliente e para o card de Serviço.

Financeiro é o caso restrito: Aba7 do cliente e a Aba Financeiro do serviço exigem `financeiro.view` (edição de lançamento: `financeiro.edit`). Comercial e Serviços não têm `financeiro.*`, então não veem valor, budget nem extrato em nenhuma das duas fichas.

| Aba do cliente | Ver | Editar |
|---|---|---|
| Dados pessoais | `clientes.view` | `clientes.edit` |
| Documentos/Contrato | `clientes.view` | `clientes.edit` |
| Negociações | `funil.view` | `funil.edit` |
| Serviços | `servicos.view` | — (abre o card do serviço) |
| Portal | `clientes.view` | `clientes.edit` (regerar token) |
| Contratos | `contratos.view` | `contratos.edit` |
| Financeiro | `financeiro.view` | `financeiro.edit` |
| Mensagens | `clientes.view` | `clientes.edit` |

| Aba do serviço | Ver | Editar |
|---|---|---|
| Dados | `servicos.view` | `servicos.edit` |
| Etapas | `servicos.view` | `servicos.edit` |
| Equipe | `servicos.view` | `servicos.edit` |
| Anexos | `servicos.view` | `servicos.edit` |
| Contrato | `contratos.view` | `contratos.edit` |
| Financeiro | `financeiro.view` | `financeiro.edit` |

Quem tem `clientes.*` ou `servicos.*` cobre view+edit do respectivo recurso. A tabela é o recurso; o wildcard da role continua valendo.

## Serviços — grid, card e local (brainstorm erp-roadmap, seção ABERTA)

O grid já esboçado fica. Colunas confirmadas: saúde, tipo, cliente, fase atual, %, previsão. A lista mostra serviços, não o catálogo — tipos continuam em Config/TiposServico, agora com `categoria: alvenaria | mobiliario`.

Exemplos do que a categoria precisa cobrir (o catálogo em si é configurável):
- **alvenaria:** reforma de cozinha, lavabo, banheiro, construção, ampliação
- **mobiliario:** móveis e mobiliário de cozinha, quarto, dormitório

**Nasce no Ganho.** Contrato assinado cria o serviço com as etapas do template do tipo. O cliente é a pessoa da negociação.

### Toolbar

Ordem fixa, a mesma do padrão grid: `[+ Novo serviço]` · busca · filtros · paginação ⏮ ◀ ▶ ⏭.

- **Busca:** por nome do cliente, entre o botão Novo e o pager. Não abre o card.
- **Filtros**, combináveis entre si e com a busca:
  - status — padrão da lista: em andamento; opções: todos | em andamento | suspenso | encerrado | cancelado
  - tipo de serviço (TiposServico)
  - categoria: alvenaria | mobiliario
  - saúde: no prazo | atenção | em atraso
  - local: no endereço do cliente | outro endereço | oficina Hako

### Card por abas

O card é em abas porque cliente e tipo **não se alteram depois de cadastrados**. As abas aprofundam o serviço sem reabrir a identidade.

- Na inclusão, cliente* e tipo* são obrigatórios. No Ganho os dois já vêm da negociação.
- Depois de gravar, cliente e tipo são somente leitura em todas as abas. Não existe "trocar cliente" nem "trocar tipo". O caminho é cancelar o serviço com justificativa (nunca exclusão).

Abas do card de Serviço:

- **Dados** (`servicos.view` / `servicos.edit`): tipo (leitura após gravar), cliente (leitura após gravar), status, datas, percentual, valor do contrato, local de execução.
- **Etapas** (`servicos.view` / `servicos.edit`): inclui, reordena e ajusta as etapas/tarefas (e sub-tarefas + predecessoras) deste serviço. O template do tipo copia as etapas na criação; a aba é onde se mexe nelas depois. Ver regra de prazo abaixo.
- **Equipe** (`servicos.view` / `servicos.edit`): vincula quem atua neste serviço (`ServicoEquipe`) a partir do cadastro unificado de Equipe (Funcionários, Colaboradores e Parceiros ativos), definindo o papel no serviço. Para Parceiros, grava um **snapshot** do `comissaoPct` vigente no cadastro (mudar o `%` no cadastro depois não altera serviços já contratados). A aba exibe `%` mas não expõe valores em R$ (que ficam restritos à aba Financeiro). Apenas pessoas vinculadas aqui aparecem no seletor de responsável das etapas.
- **Anexos** (`servicos.view` / `servicos.edit`): fotos das etapas (`momento: antes | andamento | depois` + toggle `visivelPortal: true|false` para publicar em `/obra/{token}` e montar o comparativo Antes e Depois) e documentos técnicos de uso interno em campo (plantas, marcenaria, detalhamentos). Sem exclusão física — apenas inativação ou remoção de visibilidade do portal.
- **Contrato** (`contratos.view` / `contratos.edit`): **uma fonte, dois recortes** — exibe o contrato assinado que originou este serviço (`servicoId`), eventuais aditivos e o Termo de Entrega, com visualização do PDF e status de assinatura. Invisível para a role operacional `Serviços` (que não possui `contratos.view`).
- **Financeiro** (`financeiro.view` / `financeiro.edit`): lançamentos **deste** serviço. Por dentro, receber e pagar no mesmo extrato (ícone de direção, como a seção Financeiro). Budget = `valorContrato` deste serviço.

### Datas e percentual de execução (amarrado ao Kanban e ao Cronograma)

Três datas no card, na aba Dados:

- **Data de início** (`dtInicio`, obrigatória) — começo planejado da janela.
- **Data efetiva** (`dtInicioEfetivo`, opcional) — dia em que o serviço de fato começou (preenchida manualmente no card ou automaticamente quando a 1ª tarefa avança para `Iniciada` no Kanban).
- **Data de entrega** (`dtEntrega`, obrigatória, posterior à data de início) — fim da janela. É a coluna Previsão do grid (`dtEntrega = dtInicio + prazoDias + diasSuspensos`).

O percentual de execução do serviço (`%` do grid, do card e da barra-resumo no Gantt) **nasce da movimentação dos cards no Kanban**, ponderado pela duração (`duracao` em dias) de cada tarefa/etapa:

```
pctColuna     = A iniciar: 0% | Iniciada: 10% | Em andamento: 50% | Finalizada: 90% | Entregue: 100%
pctTarefa     = sem sub-tarefas → pctColuna da própria tarefa
                com sub-tarefas → se em A iniciar: 0%; se em Entregue: 100%;
                                  se em Iniciada: max(10, Σ(pctFilha × duracaoFilha) / Σ(duracaoFilha))
pctCalculado  = inteiro, clamp(0, 100, Σ(pctTarefaTopo × duracaoTarefaTopo) / Σ(duracaoTarefaTopo))
                (sem etapas cadastradas → pctColuna do card único do serviço)
pctExibido    = inteiro, clamp(0, 100, pctCalculado + delta)
delta         = 0 até alguém ajustar manualmente
```

Serviço suspenso bloqueia a movimentação de cards no Kanban e congela o percentual; a data de entrega (`dtEntrega`) anda um dia por dia parado (`diasSuspensos`).

**Ajuste manual.** Dá para informar outro percentual no card do serviço. O valor novo grava o desvio (`delta`) em relação ao `pctCalculado` daquele momento, e continua evoluindo conforme os cards avançam no Kanban:

- Novo valor **menor** que o exibido na tela: justificativa obrigatória.
- Novo valor **igual ou maior**: sem justificativa.
- Cada ajuste grava `AjustePercentual` (de, para, calculado na hora, delta resultante, motivo se houve, usuário, data).
- Remover o ajuste devolve o exibido ao calculado pelo Kanban (`delta = 0`).
- No card, o calculado permanece visível ao lado do campo ajustável, para o desvio ficar explícito.

### Etapas, Kanban e Cronograma (sincronia em tempo real)

Todo serviço em andamento entra no seletor do Kanban e no Cronograma no momento em que é criado. Não espera ter etapa. **Kanban e Cronograma (Gantt) andam em paralelo sobre a mesma base (`ServicoEtapas`)**:

- **Movimentou no Kanban → atualiza Etapa, Serviço e Gantt:**
  - Muda o `status` e o `%` de execução da tarefa/sub-tarefa (`0% → 10% → 50% → 90% → 100%`), preenchendo internamente a barra de progresso correspondente no Gantt.
  - Grava `dtInicioReal` (ao entrar em `Iniciada`) e `dtRealizada` (ao entrar em `Finalizada` / `Entregue`).
  - Recalcula imediatamente o `%` da tarefa pai (se for sub-tarefa) e o `pctCalculado` / `pctExibido` do Serviço (no grid, no card do serviço e na linha-resumo do Gantt).
  - Desbloqueia tarefas dependentes (`predecessoraId`) assim que a tarefa anterior atinge `Finalizada` (ou `Entregue`, quando a anterior é tarefa pai com sub-tarefas).
- **Cronograma (Gantt):** uma linha-resumo por serviço (`dtInicio` → `dtEntrega`, com `%` do serviço) e, abaixo dela, a árvore de etapas/tarefas (tarefas pai expansíveis e sub-tarefas identadas) com setas de dependência (`predecessoraId`) e preenchimento interno do `%` de execução vindo do Kanban. Serviço sem etapa fica só com a linha do serviço.
- **Kanban:** caixa de seleção no topo filtra pelos serviços em andamento; no quadro, as etapas/tarefas (e sub-tarefas) do serviço selecionado são os cards nas cinco colunas (`A iniciar → Iniciada → Em andamento → Finalizada → Entregue`). Sem etapa, um card só, com a janela do serviço.
- A aba Etapas (e a linha do grid) abre o recorte daquele serviço nas duas seções.

**Prazo das etapas.** A soma das durações das etapas de topo não pode passar do prazo combinado do serviço (`prazoDias`, sem os dias suspensos), e a soma das sub-tarefas deve caber na janela da sua tarefa pai. Cada etapa também tem de caber na janela vigente: não começa antes de `dtInicio` nem termina depois da `dtEntrega` atual. Gravar etapa, arrastar barra no Cronograma ou encurtar a entrega do serviço recusa a operação quando a soma estoura ou alguma etapa sai da janela. O template copiado na criação obedece a mesma trava: se as durações do template não couberem nas datas informadas, o serviço não grava até as datas ou as etapas serem ajustadas.

### Financeiro em paralelo com o cliente

Uma fonte, dois recortes. `ParcelasReceber` e `ParcelasPagar` pertencem a um `servicoId`.

- No **serviço**, a aba mostra só os lançamentos daquele `servicoId`.
- No **cliente**, a Aba7 mostra os lançamentos de todos os serviços contratados da pessoa, cada linha com o serviço. Budget = soma dos `valorContrato`.
- O mesmo lançamento aparece nos dois lugares. O registro não se duplica.
- Um cliente com vários serviços não mistura os extratos dentro do card do serviço.

### Onde o serviço é executado

`modoExecucao: no_local | oficina`

- **no_local** — a execução é num endereço, que pode ser o do cliente ou outro.
  - `enderecoExecucaoOrigem: cliente | informado`
  - `cliente`: copia o endereço cadastral da pessoa naquele momento (snapshot). Mudança posterior no cliente não reescreve o serviço. Há ação explícita "atualizar a partir do cliente".
  - `informado`: endereço completo (rua, nº, compl, bairro, cidade, UF, CEP), o mesmo formato do endereço cadastral.
- **oficina** — a produção é na oficina da Hako. O endereço da oficina é único, em Config (`oficina.endereco`), não repetido em cada serviço. Não há execução no cliente. Depois o serviço é enviado para instalação:
  - `instalacaoOrigem: cliente | informado`
  - `cliente`: instala no endereço cadastral (snapshot, mesma regra)
  - `informado`: instala na localização que o cliente passou
  - `enderecoInstalacao` no mesmo formato

O endereço de execução (ou de instalação, no modo oficina) é editável com `servicos.edit`. O cliente não é.

### Fase atual no grid e fluxo de entrega / finalização (MVP)

- **Coluna "Fase atual" (grid de Serviços):** exibe a etapa/tarefa que está atualmente em andamento no Kanban (`Em andamento` ou `Iniciada`). Se houver mais de uma etapa ativa em paralelo, exibe pela `ordem` de cadastro.
- **100% no Kanban → destaque no grid (sem encerramento automático):** quando **todas** as tarefas do serviço no Kanban chegam na coluna `Entregue` (100%), o serviço continua com `status = andamento`, mas a linha no **grid de Serviços** recebe **cor de fundo diferenciada** sinalizando que está pronto para vistoria/entrega final.
- **Ação manual de entrega / finalização (enxuta para o MVP, refinável pós-proposta):**
  - **Se surgirem pendências de entrega/vistoria:** o usuário inclui novas tarefas/sub-tarefas de entrega no serviço (ex.: retoques, montagem final, limpeza); o serviço volta a ter itens pendentes no Kanban/Gantt e a linha do grid retorna à cor normal até que essas tarefas também cheguem em `Entregue`.
  - **Confirmar finalização definitiva:** ação manual no card do serviço informando a data efetiva de entrega (`dtEntregaEfetiva`, sugerindo hoje) + anexos opcionais (fotos finais / termo de entrega). Muda o serviço para `status = encerrada`, atualiza o ícone do cliente (🛠 → ✓) e vira o **Portal do Cliente (`/obra/{token}`)** para o estado **Entregue** (liberando fotos finais, termo e abertura de chamado de garantia por 90 dias).

## Cronograma (opção no sidebar — Gráfico de Gantt, em paralelo com o Kanban)

- **Acesso no sidebar:** item **Cronograma** logo abaixo de Kanban (`servicos.view` para visualizar; `servicos.edit` para ajustar barras).
- **Toolbar (mesma ergonomia do Kanban):**
  - **Caixa de seleção de Serviço:** lista os **serviços em andamento** (`status = andamento`), com opção `"Todos em andamento (visão consolidada)"` + seleção individual de um serviço específico.
  - **Zoom / Escala:** `Dia · Semana · Mês` + botão `[Hoje]` (centraliza na linha vertical da data atual).
- **Layout Split (Árvore à esquerda + Gráfico de Gantt à direita):**
  - Todo serviço em andamento aparece no Cronograma. Linha-resumo do serviço (`dtInicio` → `dtEntrega`, com `%` de execução do serviço) e, abaixo, árvore de etapas/tarefas (`parentId`, expansíveis `▾`) e sub-tarefas identadas (`dtInicioPrevista` / `dtPrevista` / `dtRealizada`), conectadas por setas de dependência (`predecessoraId`). Sem etapa, só a linha do serviço.
- **Sincronia total com o Kanban:** as barras das etapas exibem tanto a cor de saúde do prazo (`⚪🟢🟡🔴🔵`) quanto o preenchimento interno do `%` de execução derivado da coluna atual do card no Kanban (`0% / 10% / 50% / 90% / 100%`). Mover um card no Kanban atualiza imediatamente o preenchimento e as datas realizadas no Gantt; ajustar datas/duração no Gantt atualiza os prazos e a saúde do card no Kanban.
- **Trava de prazo e precedência:** a soma das durações das etapas de topo não passa do prazo do serviço (e sub-tarefas cabem na janela da tarefa pai), nenhuma barra de etapa sai da janela do serviço e nenhuma tarefa dependente começa antes da predecessora. Drag que estoure isso é recusado.
- Opcional fase posterior: baseline previsto vs real (linha comparativa congelada).
- Endpoints: `GET /api/cronograma` → todos os serviços em andamento com suas etapas/sub-tarefas; `GET /api/servicos/{id}/cronograma` → recorte de um serviço. Drag de barra de etapa ajusta `dtPrevista`/`dtInicioPrevista` (`servicos.edit`), respeitando a trava de prazo e precedência.

## Contratos — Emitidos, Modelos (Editor de Variáveis) e Vínculo com Orçamento/Medições

Seção **Contratos** no sidebar (`contratos.view` / `contratos.edit`), dividida em **duas opções (abas)**:

### 1. Aba "Contratos Emitidos" (`Contratos`)
- Lista todos os contratos emitidos e enviados contra os clientes para assinatura, com busca por cliente e filtros por status (`rascunho | enviado | aguardando | assinado | cancelado`) e por tipo de serviço.
- **Uma fonte, três recortes:** o mesmo registro de `Contratos` aparece aqui na visão geral, na **Aba6 Contratos** do Cliente e (após o Ganho criar o serviço) na **Aba Contrato** do Serviço.
- **Fluxo de emissão (amarrado ao Orçamento Aprovado e às Medições):**
  1. Para emitir um contrato, seleciona-se o **Cliente** e a **Negociação / Tipo de Serviço** (ou direto pela transição `Orçamento → Negociação` / Aba3 do cliente).
  2. **Pré-requisito obrigatório:** a negociação precisa ter uma `Visita` realizada (com as **medições** registradas) e um `Orcamento` com `status = aceito` (**aprovado pelo cliente**).
  3. **Validação cadastral do cliente:** se os campos de documento/endereço da **Aba1/Aba2 do Cliente** (CPF/CNPJ, RG, endereço completo) ainda estiverem vazios, o modal solicita o preenchimento antes de gerar o contrato.
  4. O sistema seleciona o **Modelo (`ContratoTemplate`)** correspondente ao `TipoServico`, substitui todas as variáveis `{{...}}` pelos dados do banco (Cliente + Medições da Visita + Valores/Condições do Orçamento Aprovado), permite revisão final no preview, gera o PDF (`pdfGerado` + `hashDoc`) e envia para assinatura.
  5. Quando o contrato recebe status `assinado`, destrava o avanço da negociação para **Ganho**, que promove o lead a `cliente` (se for o 1º contrato) e **cria o Serviço** já com `valorContrato`, prazo e vínculo `servicoId` preenchidos a partir daquele orçamento aprovado e contrato.
- **Assinatura eletrônica (`ISignatureProvider` — decisão entre 2 opções em aberto):**
  - Arquitetura pluggable pronta para receber a solução escolhida entre as duas avaliadas (**ZapSign** SaaS BR vs. **DocuSeal** self-hosted), operando com **ManualProvider** (upload do PDF assinado / aceite no portal) enquanto a decisão comercial não for batida. O Portal do Cliente (`/obra/{token}`) exibe o contrato para assinatura ou download de forma transparente.

### 2. Aba "Modelos" (`ContratoTemplates`)
- Catálogo de modelos de contrato vinculados aos **Tipos de Serviço** (`tipoServicoId` — ex.: Reforma de Cozinha, Marcenaria Dormitório, Construção/Ampliação) ou modelo-base por categoria (`alvenaria | mobiliario`).
- **Editor simples com substituição automática de variáveis (`{{campo}}`):**
  - Editor de texto limpo (formatação básica: títulos, negrito, listas, cláusulas numeradas) onde o usuário cola/edita o texto padrão do contrato e insere **tags/campos dinâmicos** clicáveis na barra do editor.
  - **Versionamento sem perda de histórico:** editar um modelo gera nova `versao`; contratos já emitidos guardam o snapshot (`textoRenderizado` + `pdfGerado`) e nunca são alterados se o modelo mudar depois.
  - **Dicionário de campos puxados automaticamente do banco de dados na emissão:**
    - **Dados do Cliente (Aba1 + Aba2 de Clientes):** `{{cliente.nome}}`, `{{cliente.cpf_cnpj}}`, `{{cliente.rg}}`, `{{cliente.estado_civil}}`, `{{cliente.profissao}}`, `{{cliente.nacionalidade}}`, `{{cliente.endereco_completo}}`, `{{cliente.celular}}`, `{{cliente.email}}`
    - **Dados do Tipo de Serviço e Local:** `{{servico.tipo}}`, `{{servico.categoria}}`, `{{servico.endereco_execucao}}`
    - **Dados das Medições (vêm da `Visita` realizada na Aba3 do Cliente):** `{{medicao.data}}`, `{{medicao.ambientes_medidas}}` (medidas e especificações levantadas no local), `{{medicao.observacoes}}`
    - **Dados de Valores e Prazos (vêm do `Orcamento` aprovado `status=aceito` na Aba3 do Cliente):** `{{orcamento.versao}}`, `{{orcamento.valor_total}}`, `{{orcamento.valor_extenso}}`, `{{orcamento.condicoes_pagamento}}`, `{{orcamento.prazo_execucao_dias}}`, `{{orcamento.escopo_resumo}}`
    - **Datas do Contrato:** `{{contrato.data_emissao}}`, `{{contrato.cidade_foro}}`

## Financeiro — Receber, Pagar, Comissões e Margem por Serviço

Seção **Financeiro** no sidebar (`financeiro.view` / `financeiro.edit`). Visível apenas para `Admin` e `Financeiro` (roles `Comercial` e `Serviços` não possuem `financeiro.*`).

### Uma fonte, três recortes
Todos os lançamentos (`ParcelasReceber` e `ParcelasPagar`) são registrados em centavos e vinculados ao `servicoId` correspondente, aparecendo sem duplicação em três lugares:
1. **Seção Financeiro (visão geral):** todos os lançamentos da empresa (pagar e receber consolidados).
2. **Aba Financeiro do Serviço:** apenas os lançamentos daquele `servicoId` + budget (`valorContrato`) e **margem real daquele serviço**.
3. **Aba7 Financeiro do Cliente:** lançamentos de todos os serviços contratados daquela pessoa (cada linha identificando o serviço) + budget total do cliente.

### Grid único (Pagar + Receber) e Toolbar
- **Indicadores no topo da seção:** Total a Receber (período) · Total a Pagar (período) · Saldo Previsto · Vencidos (🔴) · Comissões Pendentes.
- **Toolbar (padrão grid):** `[+ Novo lançamento]` · busca (por cliente, beneficiário ou descrição) · filtros combináveis · paginação `⏮ ◀ ▶ ⏭`.
  - **Filtros:** direção (`todos | a receber ▼ | a pagar ▲`), status (`todos | pendente | vencido 🔴 | pago ✓ | cancelado ✗`), natureza a pagar (`comissão | material | mão-de-obra`), serviço/cliente, e período de vencimento (`mês vigente | próximos 30d | atrasados | personalizado`).
- **Colunas do grid:** ícone de direção (`▼ Receber` verde / `▲ Pagar` vermelho), status (`pago ✓` / `pendente` / `vencido 🔴` / `cancelado ✗`), descrição (parcela / material / mão-de-obra / comissão), serviço (`tipo + cliente`), cliente/beneficiário, valor, vencimento, data de pagamento (`pagoEm`).

### Origem dos lançamentos e regras de negócio
- **Contas a Receber (`ParcelasReceber`) — nascem do Ganho/Contrato:**
  - Ao confirmar o **Ganho** no funil (contrato assinado criando o Serviço), o sistema gera ou sugere as parcelas a receber com base no `valorContrato` e nas `condicoesPagamento` do **Orçamento aprovado (`aceito`)** (ex.: sinal/entrada + parcelas).
  - Permite adicionar parcelas extras (ex.: aditivos) ou renegociar vencimentos com `financeiro.edit`.
- **Contas a Pagar (`ParcelasPagar`) — Comissões, Material e Mão-de-obra:**
  - **Comissão automática de Parceiro (`descricao = comissao`):** ao vincular um Parceiro na **Aba Equipe** do serviço (ou quando a negociação veio de indicação de parceiro), o sistema calcula `valorContrato × comissaoPct (snapshot do serviço)` e gera o lançamento a pagar vinculado ao `parceiroId` e ao `servicoId`.
  - **Custos diretos (`material | mao-de-obra`):** lançados manualmente no serviço ou pela tela Financeiro, vinculando o fornecedor/colaborador (`beneficiarioId`) e o `servicoId`.
- **Margem por Serviço (cálculo automático):**
  - `Margem Prevista = valorContrato − Σ(ParcelasPagar previstas do serviço)`
  - `Margem Realizada = Σ(ParcelasReceber pagas) − Σ(ParcelasPagar pagas)`
- **Baixa e Cancelamento (sem exclusão):**
  - **Dar baixa (`status = pago`):** informa `pagoEm` (sugere hoje) e observação/comprovante opcional.
  - **Cancelar lançamento (`status = cancelado`):** nunca há `DELETE`; exige **justificativa obrigatória** gravada com data e usuário, retirando a parcela da soma ativa mas mantendo-a no histórico auditável.

## Equipe — Funcionários, Colaboradores e Parceiros (Cadastro Unificado)

Seção **Equipe** no sidebar (`equipe.view` / `equipe.edit`, ou `Admin` + leitura operacional).

### Grid único com distinção por perfil
- Reúne em uma única lista os três perfis que atuam nos serviços da empresa:
  - **Funcionário:** equipe interna/fixa da empresa.
  - **Colaborador:** prestadores de execução em campo ou oficina (marceneiros, montadores, pedreiros, eletricistas, pintores).
  - **Parceiro:** arquitetos, engenheiros, designers de interiores ou indicadores comerciais que recebem comissão (`comissaoPct`) sobre serviços indicados/acompanhados.
- **Toolbar (padrão grid):** `[+ Novo membro]` · busca (por nome, função/especialidade ou celular) · filtros combináveis (`perfil: todos | funcionário | colaborador | parceiro`; `status: ativo | inativo`; `com serviço em andamento`) · paginação `⏮ ◀ ▶ ⏭`.
- **Colunas do grid:** status (`ativo` | `inativo`), badge de perfil (`Funcionário` | `Colaborador` | `Parceiro`), nome, função/tipo (ex.: *Marceneiro, Pedreiro, Arquiteto*), celular (contato direto), `% Comissão` (exibe o percentual apenas para `Parceiro`; exibe `—` para Funcionário e Colaborador), nº de serviços ativos.

### Card de cadastro e vínculo com Serviços
- **Campos do card:**
  - Perfil* (`funcionario | colaborador | parceiro`), status (`ativo | inativo` — inativar pede motivo).
  - Nome*, celular*, email?, função/especialidade* (para parceiro: `arquiteto | engenheiro | fornecedor | prestador | indicador`), CPF/CNPJ?, chave PIX/dados bancários? (para pagamento de mão-de-obra ou comissão), observações.
  - **Comissão padrão (`comissaoPct`):** campo visível e obrigatório **somente quando `perfil = parceiro`**. Ao vincular esse parceiro na **Aba Equipe** de um Serviço, esse percentual é copiado como **snapshot** (`ServicoEquipe.comissaoPct`) — alterações futuras no cadastro do parceiro não mudam serviços já fechados.
- **Histórico no card do membro:** aba/bloco listando os serviços em que está alocado (com atalho para o serviço) e, caso o usuário logado possua `financeiro.view`, o extrato de pagamentos/comissões daquele colaborador/parceiro.
- **Sem exclusão:** membro desligado ou parceiro inativo passa para `status = inativo` (deixa de aparecer nos seletores de novos serviços/etapas, mantendo íntegro o histórico de serviços e financeiro passados).

## Config — Catálogos, Templates de Etapas, Parâmetros e Acessos

Seção **Config** no sidebar (restrita à role `Admin`). Desenhada para que regras operacionais, tipos de serviço e dados da empresa sejam 100% configuráveis via interface — permitindo calibrar o sistema com a Hako na implantação e replicar o produto (SaaS) para outras empresas do setor sem alterar código.

Dividida em **4 abas**:

### 1. Aba "Tipos de Serviço e Etapas-Padrão" (`TiposServico` + `TemplateEtapa`)
- Gerencia o catálogo de **Tipos de Serviço** (ex.: *Reforma de Cozinha, Reforma de Banheiro/Lavabo, Ampliação, Móveis de Cozinha, Móveis de Dormitório*):
  - Campos: `nome*`, `categoria*` (`alvenaria | mobiliario`), `ativo`.
- **Árvore de Etapas/Tarefas-Padrão por Tipo (`TemplateEtapa`):**
  - Dentro de cada Tipo de Serviço, o Admin define a lista ordenada de tarefas pai, sub-tarefas (`parentId`), duração padrão em dias (`duracaoDias`) e dependências (`predecessoraId`).
  - Quando um Serviço desse tipo é criado (no Ganho do funil ou manualmente), o sistema copia automaticamente essa estrutura para `ServicoEtapas`, já deixando o **Kanban** e o **Cronograma (Gantt)** prontos para uso.

### 2. Aba "Empresa, Oficina e Parâmetros" (`ConfigSistema`)
- **Identificação da Empresa (usada no cabeçalho de contratos, PDFs e Portal do Cliente):** Nome Fantasia, Razão Social, CNPJ, endereço da sede, telefone/WhatsApp comercial e `{{contrato.cidade_foro}}`.
- **Endereço da Oficina (`oficina.endereco`):** endereço único utilizado automaticamente pelos serviços com `modoExecucao = oficina`.
- **Parâmetros de Prazo, Kanban, Funil e Garantia:**
  - `kanban.alertaPct` (padrão `70`): percentual do prazo decorrido a partir do qual o card da tarefa fica 🟡 amarelo no Kanban e no Gantt.
  - `garantia.prazoDias` (padrão `90`): dias corridos após a entrega final do serviço em que o Portal do Cliente mantém aberto o botão de chamado de garantia (e prazo após o qual o job diário inativa clientes sem serviços ativos).
  - `funil.diasAlertaParado` (padrão `7`): dias sem movimentação para exibir o alerta visual `⚠ parado há X dias` nos cards ativos do funil.

### 3. Aba "Usuários e Permissões" (`Identity` + `Roles/Claims`)
- Gestão dos usuários internos que acessam `/dashboard`: `[+ Novo usuário]`, nome completo, login/email, definição/reset de senha, status (`ativo | inativo`).
- Seleção da **Role** do usuário (`Admin`, `Comercial`, `Financeiro`, `Serviços`) e visualização da matriz de permissões (`recurso.view` / `recurso.edit`) que governa a visibilidade do sidebar, widgets do dashboard e abas dos cards de Cliente e Serviço.

### 4. Aba "Integrações e Portal/LP"
- **Assinatura de Contratos (`ISignatureProvider`):** seletor do provider ativo (`Manual` | `ZapSign` | `DocuSeal`) e campos de API Key / Webhook URL para quando a escolha comercial entre as duas opções for definida.
- **Notificações WhatsApp (`IWhatsAppNotifier`):** endereço interno/credenciais da instância Evolution API e status de conexão do número dedicado (eSIM).
- **Mini-CMS da Landing Page:** edição rápida dos números de prova social (`GET /api/public/stats` — serviços entregues, anos de mercado, avaliação) e textos básicos do site.

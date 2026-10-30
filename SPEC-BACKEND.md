# Hako — Spec Backend "Controle de Projeto"

Spec consolidada do brainstorm (out/2026). Backend interno da Hako Interiores: clientes, obras (Kanban), financeiro, contratos, assinatura e **portal do cliente** (diferencial central).

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

**Sidebar auto-oculta (sem JS):** colapsada por padrão mostrando só o rail de ícones; expande em overlay no `:hover`; mouse sai sem selecionar → recolhe mantendo a seção ativa; selecionou → navega e recolhe. Item ativo destacado no rail. Mobile: drawer via hambúrguer.

**Seções (topbar exibe o nome da seção ativa):** Dashboard · Clientes · Funil · Obras · Kanban · Financeiro · Contratos · Equipe · Config — cada item filtrado por `perm`; seção sem implementação exibe placeholder com o nome.

**Padrão grid (CRUD):** toolbar com `[+ Novo]` à esquerda e paginação ⏮ ◀ ▶ ⏭ à direita; clicar na linha abre o mesmo card-modal da inclusão (novo → [Salvar][Cancelar]; existente → [Salvar][Inativar|Cancelar…][Voltar]). **Nunca há exclusão de dados** — só inativação ou cancelamento; cancelamentos exigem justificativa (lançamentos, obras, contratos). Usuário logado fica na topbar (à direita) com dropdown: *Meu perfil* (modal: usuário, nome completo, senha ×2 com regras) e *Sair*. Tema claro/escuro no rodapé da sidebar. Boards (funil/kanban) com drag&drop entre colunas.

- **Clientes** — colunas: status (Lead|Cliente) + ativo/inativo, ícone de obra (em andamento 🛠 / entregue ✓ / cancelada ✗, +contador se >1), badge de atraso se houver etapa vermelha, nome, celular (contato direto sem abrir card)
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
Leads (funil):   Novo → Contato → Orçamento → Contrato enviado → Ganho(vira cliente+obra) | Perdido(motivo)
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
  leadsNovos: int,        // funil.view — Leads novos (site) + orçamentos em aberto
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

Card = `Negociacao`, nunca a pessoa diretamente. Uma pessoa pode ter N negociações (1ª obra, obra nova, reforma futura).
- Pessoa `tipo=lead` só na 1ª negociação; ao Ganho+contrato assinado → vira `cliente` e **nunca retorna a lead**
- Cliente existente entrando em nova negociação aparece no board com badge "★ cliente" — mesmo funil, visual distinto
- Funil mede conversão em dois níveis: lead→cliente (1ª vez) e proposta→contrato (recorrente)
- Negociacao: id, pessoaId, estagio, tipoServico?, valor?, origem, motivoPerda?, criadoEm, ganhoEm? → no Ganho cria Obra (etapas-template)

### Clientes — card CRUD (UI)
- tipo = badge readonly derivado (lead→cliente só via contrato; carga legada = fase posterior/Admin)
- obrigatórios: nome*, celular* (máscara); opcionais: email, enderecoObra, origem, observações
- cpfCnpj só visível/obrigatório quando tipo=cliente (minimização LGPD em leads)
- alteração de status no card: select + motivo obrigatório p/ inativo|cancelado
- bloco "Portal": link /obra/{token} + [Regenerar] + expiração — só cliente existente

### Clientes — card ampliado por ABAS (em discussão, seção ABERTA)
- Aba1 Dados pessoais: nome*, celular*, tel2?, email?, endereço completo (rua/nº/compl/bairro/cidade/UF/CEP — contrato exige), origem, obs
- Aba2 Documentos/Contrato: cpf, rg+órgão, estadoCivil?, profissão?, nacionalidade? — alimenta templates; obrigatórios só ao emitir contrato
- Aba3 Negociações: histórico dos cards do funil da pessoa
- Aba4 Obras: obras c/ saúde+fase+atalho p/ kanban
- Aba5 Portal: link token, regenerar, expiração, chamados garantia
- Status: select ativo|inativo|cancelado + motivo obrigatório (exceto ativo)
- SEÇÃO CLIENTES NÃO FECHADA — aguarda revisão/abas adicionais; seções revisitáveis durante desenvolvimento
- Aba6 Contratos: contratos emitidos p/ a pessoa + status + obra associada a cada contrato
- Aba7 Financeiro: budget total (soma valorContrato das obras) + extrato de recebimentos/pagamentos ligados ao cliente (parcelas por obra, status pago|pendente|vencido, saldo)
- Aba Portal do cliente: gerencia acesso /obra/{token} (link, regerar, expiração, chamados garantia) — refinar em dev
- Aba8 Mensagens (CRM): histórico unificado de conversas com o cliente — envio/recebimento WhatsApp via Evolution API hoje, atrás de interface IMessagingProvider p/ migrar p/ Meta Business oficial sem trocar a UI; guardar também notas internas da equipe

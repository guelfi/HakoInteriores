# Hako — Spec Backend "Controle de Projeto"

Spec consolidada do brainstorm (out/2026). Backend interno da Hako Interiores: clientes, obras (Kanban), financeiro, contratos, assinatura e **portal do cliente** (diferencial central).

## Stack

- **.NET 10** + **Blazor Server** (UI + API no mesmo app, deploy único)
- **EF Core + SQLite** (arquivo em volume Docker; migração futura p/ Postgres só trocando provider)
- **ASP.NET Identity** (usuários internos) + roles
- Container `hako-admin` na mesma rede Docker do site (`www_projetos-net`), atrás do Nginx proxy
- **Admin:** `hakointeriores.com.br/dash` (Nginx location `/dash` → `hako-admin`; Blazor `PathBase="/dash"`) — caminho não-publicado, sem link no menu da LP. Proteção real = Identity+roles+rate limit; a obscuridade é só camada extra
- **Portal do cliente:** `hakointeriores.com.br/obra/{token}`
- **Segurança (todas as camadas):** Cloudflare (WAF/rate-limit/proxy) → OCI → app .NET (Identity + roles + rate limiting + HTTPS + headers de segurança + validação de input em todo endpoint). Caminho `/dash` não-publicado é só camada extra — nunca substitui auth. Boas práticas são requisito de entrega, não opcionais
- **LGPD:** LP recebe link "Política de Privacidade" no rodapé (`/privacidade.html`); backend deve registrar consentimento e direitos do titular (acesso/correção/exclusão)

## Portal do cliente — peça central (SEM login, SEM dependência de WhatsApp)

- URL com token único por lead/obra: `app.hakointeriores.com.br/obra/{token}`
- Cliente recebe o link **logo após o primeiro contato** (por qualquer canal) e acompanha TUDO:
  - **Negociação:** status do orçamento → contrato para assinar na própria página
  - **Obra:** etapas em visão Kanban read-only com cores de saúde + fotos do andamento
  - **Entregue:** termo de entrega, canal de garantia/pós-venda
- Nada de Evolution API/WhatsApp API como canal obrigatório — portal é a fonte da verdade
- Gancho de marketing na LP: "acompanhe sua obra online, do contrato à entrega" (iteração futura)

## RBAC (equipe interna)

| Role | Acesso |
|---|---|
| Admin | tudo + usuários + ConfigSistema |
| Comercial | clientes, funil, contratos |
| Financeiro | clientes(read), contas a receber/pagar, relatórios |
| Obras | clientes(read), status/etapas de obra, anexos/fotos |

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
| 1 | Scaffold .NET 10 + Blazor + Identity + SQLite + roles |
| 2 | Cadastros base (TiposServico+Templates, Colaboradores, Parceiros, Clientes) + funil de leads + portal read-only (acompanha negociação) |
| 3 | Obras + equipe + Kanban drag-drop com cores + QR/foto por etapa + portal mostra etapas |
| 4 | Financeiro completo (receber+pagar+comissões) + margem por obra |
| 5 | Contratos + templates + PDF + ISignatureProvider (Manual → provider real) |
| 6 | Câmeras/snapshots, garantia/pós-venda, dashboard, antes-e-depois |

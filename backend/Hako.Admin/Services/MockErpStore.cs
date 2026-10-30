namespace Hako.Admin.Services;

public class MockErpStore
{
    public string CurrentRole { get; set; } = "Admin";
    public string ActiveSection { get; set; } = "dash";
    public int SelectedServicoKanbanId { get; set; } = 1;
    public int SelectedServicoCronogramaId { get; set; } = 0; // 0 = Todos em andamento

    public event Action? OnChange;
    public void NotifyStateChanged() => OnChange?.Invoke();

    // ConfigSistema
    public string EmpresaNomeFantasia { get; set; } = "Hako Interiores";
    public string EmpresaRazaoSocial { get; set; } = "Hako Design, Reformas e Marcenaria Ltda.";
    public string EmpresaCnpj { get; set; } = "48.192.301/0001-88";
    public string EmpresaEndereco { get; set; } = "Av. Ibirapuera, 2900 - Conj. 84, Moema, São Paulo/SP";
    public string CidadeForo { get; set; } = "São Paulo/SP";
    public string OficinaEndereco { get; set; } = "Rua das Indústrias, 410 - Galpão 3, Vila Leopoldina, São Paulo/SP - CEP 05311-000";
    public int KanbanAlertaPct { get; set; } = 70;
    public int GarantiaPrazoDias { get; set; } = 90;
    public int FunilDiasAlertaParado { get; set; } = 7;
    public string SignatureProviderAtivo { get; set; } = "Manual"; // Manual | ZapSign | DocuSeal
    public string EvolutionInstanceUrl { get; set; } = "https://evo.hakointeriores.com.br/instance/hako-esim";
    public string EvolutionNumeroDedicado { get; set; } = "+55 (11) 99102-8844";
    public int LpServicosEntregues { get; set; } = 148;
    public int LpAnosMercado { get; set; } = 9;
    public string LpNotaMedia { get; set; } = "4.9";

    public static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        ["Admin"] = ["clientes.*", "funil.*", "contratos.*", "servicos.*", "kanban.*", "financeiro.*", "equipe.*", "config.*"],
        ["Comercial"] = ["clientes.*", "funil.*", "contratos.*", "servicos.view"],
        ["Financeiro"] = ["financeiro.*", "clientes.view", "servicos.view"],
        ["Serviços"] = ["servicos.*", "kanban.*", "equipe.view"],
        ["Equipe"] = ["servicos.view", "kanban.move"],
    };

    public bool HasPerm(string need)
    {
        var perms = RolePermissions.GetValueOrDefault(CurrentRole) ?? [];
        var res = need.Split('.')[0];
        return perms.Any(p => p == need || p == $"{res}.*");
    }

    public List<UsuarioSistema> Usuarios { get; } =
    [
        new() { Id = 1, Login = "admin", Nome = "Henrique (Admin Hako)", Email = "henrique@hakointeriores.com.br", Role = "Admin", Ativo = true },
        new() { Id = 2, Login = "comercial", Nome = "Camila Vendas", Email = "comercial@hakointeriores.com.br", Role = "Comercial", Ativo = true },
        new() { Id = 3, Login = "financeiro", Nome = "Rodrigo Financeiro", Email = "financeiro@hakointeriores.com.br", Role = "Financeiro", Ativo = true },
        new() { Id = 4, Login = "operacao", Nome = "João Marceneiro (Campo)", Email = "obras@hakointeriores.com.br", Role = "Serviços", Ativo = true },
    ];

    public List<TipoServicoItem> TiposServico { get; } =
    [
        new()
        {
            Id = 1, Nome = "Móveis de Cozinha Planejada", Categoria = "mobiliario", Ativo = true,
            EtapasPadrao =
            [
                new() { Id = 1, Nome = "Conferência de medidas e projeto executivo", Ordem = 1, DuracaoDias = 5 },
                new() { Id = 2, Nome = "Produção na oficina (Marcenaria)", Ordem = 2, DuracaoDias = 15, PredecessoraId = 1 },
                new() { Id = 3, ParentId = 2, Nome = "Plano de corte e usinagem MDF", Ordem = 1, DuracaoDias = 5 },
                new() { Id = 4, ParentId = 2, Nome = "Fita de borda e pré-montagem", Ordem = 2, DuracaoDias = 6 },
                new() { Id = 5, ParentId = 2, Nome = "Ferragens, dobradiças e embalagem", Ordem = 3, DuracaoDias = 4 },
                new() { Id = 6, Nome = "Montagem e instalação no local", Ordem = 3, DuracaoDias = 7, PredecessoraId = 2 },
                new() { Id = 7, Nome = "Acabamento fino e limpeza", Ordem = 4, DuracaoDias = 3, PredecessoraId = 6 }
            ]
        },
        new()
        {
            Id = 2, Nome = "Móveis de Dormitório / Closet", Categoria = "mobiliario", Ativo = true,
            EtapasPadrao =
            [
                new() { Id = 10, Nome = "Detalhamento executivo e aprovação", Ordem = 1, DuracaoDias = 4 },
                new() { Id = 11, Nome = "Corte e produção em oficina", Ordem = 2, DuracaoDias = 12, PredecessoraId = 10 },
                new() { Id = 12, Nome = "Montagem de armários e portas", Ordem = 3, DuracaoDias = 5, PredecessoraId = 11 }
            ]
        },
        new()
        {
            Id = 3, Nome = "Reforma Completa de Cozinha", Categoria = "alvenaria", Ativo = true,
            EtapasPadrao =
            [
                new() { Id = 20, Nome = "Demolição e retirada de entulho", Ordem = 1, DuracaoDias = 4 },
                new() { Id = 21, Nome = "Infraestrutura (Hidráulica e Elétrica)", Ordem = 2, DuracaoDias = 8, PredecessoraId = 20 },
                new() { Id = 22, ParentId = 21, Nome = "Tubulação água fria/quente e esgoto", Ordem = 1, DuracaoDias = 4 },
                new() { Id = 23, ParentId = 21, Nome = "Pontos elétricos e iluminação LED", Ordem = 2, DuracaoDias = 4 },
                new() { Id = 24, Nome = "Revestimentos, porcelanato e bancadas", Ordem = 3, DuracaoDias = 10, PredecessoraId = 21 },
                new() { Id = 25, Nome = "Pintura, louças e entrega técnica", Ordem = 4, DuracaoDias = 5, PredecessoraId = 24 }
            ]
        },
        new()
        {
            Id = 4, Nome = "Reforma de Banheiro / Lavabo", Categoria = "alvenaria", Ativo = true,
            EtapasPadrao =
            [
                new() { Id = 30, Nome = "Demolição e impermeabilização Box", Ordem = 1, DuracaoDias = 5 },
                new() { Id = 31, Nome = "Assentamento de porcelanato e nicho", Ordem = 2, DuracaoDias = 7, PredecessoraId = 30 },
                new() { Id = 32, Nome = "Instalação de bancada, metais e box", Ordem = 3, DuracaoDias = 4, PredecessoraId = 31 }
            ]
        },
        new()
        {
            Id = 5, Nome = "Construção e Ampliação Residencial", Categoria = "alvenaria", Ativo = true,
            EtapasPadrao =
            [
                new() { Id = 40, Nome = "Fundação e estrutura", Ordem = 1, DuracaoDias = 15 },
                new() { Id = 41, Nome = "Alvenaria e cobertura", Ordem = 2, DuracaoDias = 15, PredecessoraId = 40 },
                new() { Id = 42, Nome = "Acabamentos e pintura", Ordem = 3, DuracaoDias = 15, PredecessoraId = 41 }
            ]
        }
    ];

    public List<MembroEquipe> Equipe { get; } =
    [
        new() { Id = 1, Perfil = "Funcionário", Nome = "João Marceneiro", Funcao = "Marceneiro Sênior", Celular = "(11) 91111-0001", Email = "joao@hakointeriores.com.br", CpfCnpj = "281.440.198-01", Pix = "joao@hakointeriores.com.br", Ativo = true },
        new() { Id = 2, Perfil = "Funcionário", Nome = "Pedro Ajudante", Funcao = "Auxiliar de Montagem e Obra", Celular = "(11) 92222-0002", Email = "pedro@hakointeriores.com.br", CpfCnpj = "390.112.448-20", Pix = "11922220002", Ativo = true },
        new() { Id = 3, Perfil = "Colaborador", Nome = "Marcos Eletricista", Funcao = "Elétrica e Iluminação", Celular = "(11) 93333-0003", CpfCnpj = "19.442.881/0001-10", Pix = "19442881000110", Ativo = true },
        new() { Id = 4, Perfil = "Colaborador", Nome = "Carlos Mestre de Obras", Funcao = "Alvenaria e Revestimentos", Celular = "(11) 94444-0004", CpfCnpj = "31.902.114/0001-55", Pix = "11944440004", Ativo = true },
        new() { Id = 5, Perfil = "Colaborador", Nome = "Luiz Gesseiro", Funcao = "Gesso e Drywall", Celular = "(11) 94444-0099", Ativo = false, MotivoInativacao = "Mudou de cidade em set/2026" },
        new() { Id = 6, Perfil = "Parceiro", Nome = "Arq. Sofia Ribeiro", Funcao = "Arquiteta — Interiores", TipoParceiro = "arquiteto", Celular = "(11) 95555-0005", Email = "sofia@ribeiroarq.com.br", CpfCnpj = "22.891.004/0001-90", Pix = "sofia@ribeiroarq.com.br", ComissaoPct = 10, Ativo = true },
        new() { Id = 7, Perfil = "Parceiro", Nome = "Eng. Ricardo Paz", Funcao = "Engenheiro Civil — Estrutural", TipoParceiro = "engenheiro", Celular = "(11) 96666-0006", Email = "ricardo@pazeng.com.br", ComissaoPct = 8, Ativo = true },
        new() { Id = 8, Perfil = "Parceiro", Nome = "Marmoraria Itália", Funcao = "Fornecedor — Bancadas e Pedras", TipoParceiro = "fornecedor", Celular = "(11) 97777-0007", ComissaoPct = 5, Ativo = true },
    ];

    public List<ClienteItem> Clientes { get; } = [];
    public List<NegociacaoItem> Negociacoes { get; } = [];
    public List<ContratoTemplateItem> ModelosContrato { get; } = [];
    public List<ContratoEmitidoItem> ContratosEmitidos { get; } = [];
    public List<ServicoItem> Servicos { get; } = [];
    public List<LancamentoFinanceiro> Lancamentos { get; } = [];

    public MockErpStore()
    {
        SeedData();
    }

    private void SeedData()
    {
        Clientes.AddRange(
        [
            new()
            {
                Id = 1, Nome = "Mariana Costa", Celular = "(11) 98765-1100", Tel2 = "(11) 3044-1100",
                Email = "mariana.costa@email.com", IsLead = false, Status = "ativo", Origem = "parceiro",
                Rua = "Rua Normandia", Numero = "412", Complemento = "Apto 402", Bairro = "Moema",
                Cidade = "São Paulo", Uf = "SP", Cep = "04517-040",
                CpfCnpj = "348.921.448-12", Rg = "42.198.331-X", OrgaoEmissor = "SSP/SP",
                EstadoCivil = "Casada", Profissao = "Médica", Nacionalidade = "Brasileira",
                PortalToken = "mcosta-402-a8f3k2m9x7",
                Observacoes = "Indicada pela Arq. Sofia Ribeiro. Exige horário comercial estrito do condomínio (9h às 17h).",
                Timeline =
                [
                    new("01/10/2026 10:15", "Camila Vendas", "Card criado em Contato (indicação Arq. Sofia Ribeiro — Móveis de Cozinha Planejada)"),
                    new("02/10/2026 14:30", "João Marceneiro", "Visita/Medição realizada no Apto 402 — medidas da cozinha e ilha gourmet registradas"),
                    new("03/10/2026 11:00", "Camila Vendas", "Orçamento v1 enviado (R$ 55.500,00 — 3x R$ 18.500,00)"),
                    new("03/10/2026 17:40", "Camila Vendas", "Orçamento v1 APROVADO pela cliente + Contrato #101 emitido e enviado"),
                    new("04/10/2026 09:20", "Henrique (Admin)", "Contrato #101 ASSINADO — Negociação movida para Ganho → Cliente promovida e Serviço #1 criado")
                ],
                Mensagens =
                [
                    new("02/10/2026 15:10", "Cliente (WhatsApp)", "Olá Camila! O João acabou de medir aqui no apto 402. Fico no aguardo do orçamento."),
                    new("03/10/2026 11:05", "Camila Vendas (WhatsApp)", "Oi Mariana! Acabei de enviar o orçamento v1 e atualizar no seu Portal de Acompanhamento."),
                    new("04/10/2026 09:25", "Nota Interna", "Cliente assinou digitalmente. Produção dos módulos de MDF iniciada na oficina.")
                ]
            },
            new()
            {
                Id = 2, Nome = "Roberto Almeida", Celular = "(11) 97654-2200", Email = "roberto.almeida@empresa.com.br",
                IsLead = false, Status = "ativo", Origem = "instagram",
                Rua = "Alameda Lorena", Numero = "1180", Complemento = "Apto 121", Bairro = "Jardins",
                Cidade = "São Paulo", Uf = "SP", Cep = "01424-001",
                CpfCnpj = "219.840.118-90", Rg = "28.441.902-1", OrgaoEmissor = "SSP/SP",
                EstadoCivil = "Solteiro", Profissao = "Empresário", Nacionalidade = "Brasileira",
                PortalToken = "ralmeida-121-k9p4x2m1",
                Timeline =
                [
                    new("10/10/2026 09:00", "Camila Vendas", "Lead captado via Instagram → Contato realizado"),
                    new("12/10/2026 16:00", "João Marceneiro", "Visita técnica e medição da suíte master realizada"),
                    new("14/10/2026 10:00", "Camila Vendas", "Orçamento v1 aprovado (R$ 29.400,00) e Contrato #102 assinado → Serviço #2 criado")
                ],
                Mensagens =
                [
                    new("14/10/2026 10:30", "Camila Vendas (WhatsApp)", "Roberto, seu link de acompanhamento da obra já está ativo!")
                ]
            },
            new()
            {
                Id = 3, Nome = "Beatriz Nunes", Celular = "(11) 92109-7700", Email = "bia.nunes@gmail.com",
                IsLead = false, Status = "ativo", Origem = "indicacao",
                Rua = "Rua Ministro Jesuíno Cardoso", Numero = "550", Complemento = "Apto 92", Bairro = "Vila Nova Conceição",
                Cidade = "São Paulo", Uf = "SP", Cep = "04544-051",
                CpfCnpj = "401.229.884-55", Rg = "35.991.204-8", OrgaoEmissor = "SSP/SP",
                EstadoCivil = "Casada", Profissao = "Advogada", Nacionalidade = "Brasileira",
                PortalToken = "bnunes-92-z7v3q8n5",
                Timeline =
                [
                    new("25/09/2026 11:00", "Camila Vendas", "Contrato assinado para Reforma de Banheiro / Lavabo → Serviço #3 criado"),
                    new("29/10/2026 18:00", "João Marceneiro", "Todas as etapas do Serviço #3 concluídas no Kanban (100%) — aguardando vistoria/finalização")
                ]
            },
            new()
            {
                Id = 4, Nome = "Fernanda Lira", Celular = "(11) 96543-3300", Email = "fernanda.lira@uol.com.br",
                IsLead = true, Status = "ativo", Origem = "lp",
                Cidade = "São Paulo", Uf = "SP",
                PortalToken = "flira-lead-n8m2v4x1",
                Observacoes = "Enviou formulário pela Landing Page pedindo orçamento para reforma completa de cozinha.",
                Timeline =
                [
                    new("29/10/2026 21:15", "Sistema (LP)", "Lead recebido via formulário da Landing Page — Estágio: Novo")
                ]
            },
            new()
            {
                Id = 5, Nome = "Tiago Braga", Celular = "(11) 97788-9900", Email = "tiago.braga@gmail.com",
                IsLead = true, Status = "ativo", Origem = "lp",
                Rua = "Rua Tucuna", Numero = "820", Complemento = "Casa 2", Bairro = "Perdizes",
                Cidade = "São Paulo", Uf = "SP", Cep = "05021-010",
                CpfCnpj = "388.102.994-10", Rg = "40.112.889-3", OrgaoEmissor = "SSP/SP",
                EstadoCivil = "Casado", Profissao = "Arquiteto de Software", Nacionalidade = "Brasileira",
                PortalToken = "tbraga-lead-p3k9w7c2",
                Timeline =
                [
                    new("20/10/2026 10:00", "Sistema (LP)", "Lead recebido via LP"),
                    new("21/10/2026 11:20", "Camila Vendas", "Contato telefônico realizado → Avançou para Contato"),
                    new("24/10/2026 15:00", "Carlos Mestre de Obras", "Visita e medição realizada na Casa 2 (Perdizes) — Cozinha 18m²"),
                    new("26/10/2026 09:30", "Camila Vendas", "Orçamento v1 enviado (R$ 42.000,00) — aguardando aprovação do cliente")
                ]
            },
            new()
            {
                Id = 6, Nome = "Paulo Henrique", Celular = "(11) 95432-4400", Email = "paulo.h@outlook.com",
                IsLead = false, Status = "ativo", Origem = "indicacao",
                Rua = "Rua Apinajés", Numero = "310", Complemento = "Apto 61", Bairro = "Sumaré",
                Cidade = "São Paulo", Uf = "SP", Cep = "05017-000",
                CpfCnpj = "192.448.771-04", Rg = "21.884.102-9", OrgaoEmissor = "SSP/SP",
                EstadoCivil = "Divorciado", Profissao = "Contador", Nacionalidade = "Brasileira",
                PortalToken = "phenrique-61-w5x9m2r8"
            }
        ]);

        // Negociações (Funil + Aba3)
        Negociacoes.AddRange(
        [
            new()
            {
                Id = 1, ClienteId = 4, TipoServicoId = 3, Estagio = "Novo", Status = "em_andamento",
                ValorEstimado = 48000_00, Origem = "lp", CriadoEm = "29/10/2026", ParadoDias = 1
            },
            new()
            {
                Id = 2, ClienteId = 1, TipoServicoId = 2, Estagio = "Contato", Status = "em_andamento",
                ValorEstimado = 26500_00, Origem = "parceiro", CriadoEm = "20/10/2026", ParadoDias = 9
            },
            new()
            {
                Id = 3, ClienteId = 3, TipoServicoId = 1, Estagio = "Visita/Medição", Status = "em_andamento",
                ValorEstimado = 38000_00, Origem = "indicacao", CriadoEm = "25/10/2026", ParadoDias = 4,
                Visitas =
                [
                    new()
                    {
                        Id = 1, NegociacaoId = 3, DataAgendada = "27/10/2026 14:00", DataRealizada = "27/10/2026 15:10",
                        Status = "realizada",
                        Medicoes = "Parede pia: 3,40m × 2,65m pé-direito | Ilha central: 1,80m × 0,90m | Torre quente: 0,70m larg. MDF Freijó + Cinza Urbano.",
                        Observacoes = "Ponto de gás e coifa já posicionados no forro."
                    }
                ]
            },
            new()
            {
                Id = 4, ClienteId = 5, TipoServicoId = 3, Estagio = "Orçamento", Status = "em_andamento",
                ValorEstimado = 42000_00, Origem = "lp", CriadoEm = "20/10/2026", ParadoDias = 3,
                Visitas =
                [
                    new()
                    {
                        Id = 2, NegociacaoId = 4, DataAgendada = "24/10/2026 15:00", DataRealizada = "24/10/2026 16:00",
                        Status = "realizada",
                        Medicoes = "Cozinha 18,4m² piso + 32m² parede revestimento metrô white | Bancada granito São Gabriel 4,10m linear | Troca de 8 pontos hidráulicos.",
                        Observacoes = "Necessário caçamba estacionária na rua."
                    }
                ],
                Orcamentos =
                [
                    new()
                    {
                        Id = 1, NegociacaoId = 4, Versao = 1, Valor = 42000_00,
                        CondicoesPagamento = "30% de sinal na assinatura (R$ 12.600,00) + 3 parcelas mensais de R$ 9.800,00 no boleto/PIX",
                        PrazoExecucaoDias = 27,
                        EscopoResumo = "Demolição de revestimento antigo, nova rede hidráulica/elétrica, assentamento de porcelanato 18,4m² e revestimento 32m².",
                        Status = "enviado", ArquivoPdf = "Orcamento_v1_TiagoBraga_Cozinha.pdf", EnviadoEm = "26/10/2026"
                    }
                ]
            },
            new()
            {
                Id = 5, ClienteId = 2, TipoServicoId = 4, Estagio = "Negociação", Status = "fechada",
                ValorEstimado = 21000_00, Origem = "instagram", CriadoEm = "18/10/2026", ParadoDias = 2,
                Visitas =
                [
                    new()
                    {
                        Id = 3, NegociacaoId = 5, DataAgendada = "20/10/2026 10:00", DataRealizada = "20/10/2026 11:00",
                        Status = "realizada",
                        Medicoes = "Lavabo social 3,2m² + Banheiro suíte 6,5m² | Nicho esculpido 0,80m × 0,35m | Impermeabilização polimérica.",
                        Observacoes = "Revestimento fornecido pelo cliente; argamassa e mão de obra Hako."
                    }
                ],
                Orcamentos =
                [
                    new()
                    {
                        Id = 2, NegociacaoId = 5, Versao = 1, Valor = 21000_00,
                        CondicoesPagamento = "50% na assinatura (R$ 10.500,00) + 50% na entrega técnica (R$ 10.500,00)",
                        PrazoExecucaoDias = 16,
                        EscopoResumo = "Reforma completa de lavabo e banheiro suíte com impermeabilização, porcelanato e instalação de bancada e metais.",
                        Status = "aceito", ArquivoPdf = "Orcamento_v1_RobertoAlmeida_Banheiro.pdf", EnviadoEm = "22/10/2026", RespondidoEm = "25/10/2026"
                    }
                ]
            },
            new()
            {
                Id = 6, ClienteId = 1, TipoServicoId = 1, Estagio = "Ganho", Status = "fechada",
                ValorEstimado = 55500_00, Origem = "parceiro", CriadoEm = "01/10/2026", ParadoDias = 0,
                Visitas =
                [
                    new()
                    {
                        Id = 4, NegociacaoId = 6, DataAgendada = "02/10/2026 14:00", DataRealizada = "02/10/2026 14:30",
                        Status = "realizada",
                        Medicoes = "Cozinha planejada Apto 402: Bancada 3,80m + Armários superiores 3,80m × 0,90m + Ilha gourmet 2,20m × 1,00m + Cristaleira iluminada.",
                        Observacoes = "MDF Louro Freijó com frentes em Laca Cinza Urbano e corrediças ocultas com amortecimento."
                    }
                ],
                Orcamentos =
                [
                    new()
                    {
                        Id = 3, NegociacaoId = 6, Versao = 1, Valor = 55500_00,
                        CondicoesPagamento = "3 parcelas iguais de R$ 18.500,00 (05/10, 05/11 e 05/12/2026)",
                        PrazoExecucaoDias = 30,
                        EscopoResumo = "Fabricação em oficina e instalação de mobiliário sob medida completo para cozinha e ilha gourmet (Apto 402).",
                        Status = "aceito", ArquivoPdf = "Orcamento_v1_MarianaCosta_Cozinha.pdf", EnviadoEm = "03/10/2026", RespondidoEm = "03/10/2026"
                    }
                ]
            }
        ]);

        // Modelos de Contrato (ContratoTemplates com {{placeholders}})
        ModelosContrato.AddRange(
        [
            new()
            {
                Id = 1, TipoServicoId = 1, Nome = "Contrato de Marcenaria e Mobiliário Sob Medida", Versao = "v2.2", Ativo = true,
                CorpoTexto = """
                CONTRATO DE PRESTAÇÃO DE SERVIÇOS DE MARCENARIA SOB MEDIDA

                CONTRATANTE: {{cliente.nome}}, inscrito(a) no CPF/CNPJ sob nº {{cliente.cpf_cnpj}}, RG {{cliente.rg}}, {{cliente.nacionalidade}}, {{cliente.estado_civil}}, {{cliente.profissao}}, residente e domiciliado(a) em {{cliente.endereco_completo}}, telefone {{cliente.celular}}, e-mail {{cliente.email}}.

                CONTRATADA: Hako Design, Reformas e Marcenaria Ltda., CNPJ 48.192.301/0001-88.

                CLÁUSULA 1ª — DO OBJETO E DAS MEDIÇÕES APROVADAS
                1.1. O presente instrumento tem como objeto a fabricação e instalação de {{servico.tipo}} ({{servico.categoria}}), a ser instalado em {{servico.endereco_execucao}}.
                1.2. Especificações técnicas e medidas aferidas na visita de {{medicao.data}}:
                {{medicao.ambientes_medidas}}
                Observações técnicas: {{medicao.observacoes}}

                CLÁUSULA 2ª — DO ESCOPO E ORÇAMENTO APROVADO (Versão {{orcamento.versao}})
                2.1. Escopo detalhado: {{orcamento.escopo_resumo}}
                2.2. Valor total ajustado: {{orcamento.valor_total}} ({{orcamento.valor_extenso}}).
                2.3. Forma e condições de pagamento: {{orcamento.condicoes_pagamento}}.

                CLÁUSULA 3ª — DO PRAZO DE EXECUÇÃO E GARANTIA
                3.1. O prazo estimado para execução e entrega é de {{orcamento.prazo_execucao_dias}} dias corridos.
                3.2. A CONTRATADA oferece garantia legal e contratual de 90 (noventa) dias após o Termo de Entrega, acompanhável online pelo Portal do Cliente.

                Foro eleito: {{contrato.cidade_foro}}, emitido em {{contrato.data_emissao}}.
                """
            },
            new()
            {
                Id = 2, TipoServicoId = 3, Nome = "Contrato de Reforma Civil e Alvenaria", Versao = "v1.5", Ativo = true,
                CorpoTexto = """
                CONTRATO DE EMPREITADA E REFORMA RESIDENCIAL

                CONTRATANTE: {{cliente.nome}}, CPF/CNPJ {{cliente.cpf_cnpj}}, RG {{cliente.rg}}, residente em {{cliente.endereco_completo}}.

                CLÁUSULA 1ª — DO OBJETO E LEVANTAMENTO TÉCNICO
                1.1. Execução de {{servico.tipo}} no imóvel situado em {{servico.endereco_execucao}}.
                1.2. Medições apuradas em {{medicao.data}}:
                {{medicao.ambientes_medidas}}

                CLÁUSULA 2ª — DO PREÇO E CONDIÇÕES (Orçamento v{{orcamento.versao}})
                2.1. Escopo aprovado: {{orcamento.escopo_resumo}}
                2.2. Valor contratado: {{orcamento.valor_total}} ({{orcamento.valor_extenso}}).
                2.3. Condições de pagamento: {{orcamento.condicoes_pagamento}}.
                2.4. Prazo de execução: {{orcamento.prazo_execucao_dias}} dias corridos.

                {{contrato.cidade_foro}}, {{contrato.data_emissao}}.
                """
            },
            new()
            {
                Id = 3, TipoServicoId = 4, Nome = "Contrato de Reforma de Banheiro / Lavabo", Versao = "v1.2", Ativo = true,
                CorpoTexto = """
                CONTRATO DE REFORMA DE BANHEIRO E IMPERMEABILIZAÇÃO

                CONTRATANTE: {{cliente.nome}} (CPF {{cliente.cpf_cnpj}}), endereço {{cliente.endereco_completo}}.
                OBJETO: {{servico.tipo}} no local {{servico.endereco_execucao}}.
                MEDIÇÕES ({{medicao.data}}): {{medicao.ambientes_medidas}}
                ORÇAMENTO APROVADO (v{{orcamento.versao}}): {{orcamento.valor_total}} — {{orcamento.condicoes_pagamento}}.
                PRAZO PREVISTO: {{orcamento.prazo_execucao_dias}} dias.
                Local e data: {{contrato.cidade_foro}}, {{contrato.data_emissao}}.
                """
            }
        ]);

        // Contratos Emitidos
        ContratosEmitidos.AddRange(
        [
            new()
            {
                Id = 101, ClienteId = 1, NegociacaoId = 6, OrcamentoId = 3, ServicoId = 1, TemplateId = 1,
                Status = "assinado", EnviadoEm = "03/10/2026", AssinadoEm = "04/10/2026",
                HashDoc = "sha256:9f84b2c1e0d3a7f5", PdfNome = "Contrato_101_MarianaCosta_Assinado.pdf",
                TextoRenderizado = RenderContratoTexto(1, 6, 1)
            },
            new()
            {
                Id = 102, ClienteId = 2, NegociacaoId = 5, OrcamentoId = 2, ServicoId = null, TemplateId = 3,
                Status = "aguardando", EnviadoEm = "26/10/2026", AssinadoEm = "—",
                HashDoc = "sha256:3c11a8f0b442d901", PdfNome = "Contrato_102_RobertoAlmeida_Banheiro.pdf",
                TextoRenderizado = RenderContratoTexto(2, 5, 3)
            }
        ]);

        // Serviços
        Servicos.AddRange(
        [
            // Serviço 1: Mariana Costa — Cozinha Planejada (Em andamento, com tarefa pai em Iniciada e sub-tarefas!)
            new()
            {
                Id = 1, ClienteId = 1, TipoServicoId = 1, NegociacaoId = 6, Status = "andamento",
                ModoExecucao = "oficina",
                InstalacaoOrigem = "cliente",
                EnderecoInstalacao = "Rua Normandia, 412 - Apto 402, Moema, São Paulo/SP - CEP 04517-040",
                DtInicio = new DateOnly(2026, 10, 5),
                DtInicioEfetivo = new DateOnly(2026, 10, 5),
                DtEntrega = new DateOnly(2026, 11, 15),
                PrazoDias = 41,
                ValorContrato = 55500_00,
                AjustePctDelta = 0,
                Equipe =
                [
                    new() { MembroId = 1, Papel = "Marceneiro Responsável", ComissaoPct = 0 },
                    new() { MembroId = 2, Papel = "Auxiliar de Montagem", ComissaoPct = 0 },
                    new() { MembroId = 6, Papel = "Arquiteta Indicadora", ComissaoPct = 8 } // snapshot 8% na época!
                ],
                Anexos =
                [
                    new() { Id = 1, NomeArquivo = "Cozinha_Apto402_Antes_Demolicao.jpg", Tipo = "foto", Momento = "antes", EtapaNome = "Conferência de medidas e projeto executivo", VisivelPortal = true, DataUpload = "05/10/2026" },
                    new() { Id = 2, NomeArquivo = "Corte_Usinagem_MDF_Freijo_Oficina.jpg", Tipo = "foto", Momento = "andamento", EtapaNome = "Produção na oficina (Marcenaria)", VisivelPortal = true, DataUpload = "18/10/2026" },
                    new() { Id = 3, NomeArquivo = "Detalhamento_Executivo_Marcenaria_vFinal.pdf", Tipo = "documento", Momento = "andamento", EtapaNome = "Conferência de medidas e projeto executivo", VisivelPortal = false, DataUpload = "06/10/2026" }
                ],
                Etapas =
                [
                    // Tarefa 1 (Pai com sub-tarefas JA ENTREGUE -> demonstra o hover das filhas concluídas na coluna Entregue!)
                    new()
                    {
                        Id = 101, ServicoId = 1, Nome = "Conferência de medidas e projeto executivo", Ordem = 1,
                        Coluna = "Entregue", DtInicioPrevista = new DateOnly(2026, 10, 5), DtPrevista = new DateOnly(2026, 10, 12),
                        DtInicioReal = new DateOnly(2026, 10, 5), DtRealizada = new DateOnly(2026, 10, 11),
                        DuracaoDias = 7, ResponsavelNome = "João Marceneiro"
                    },
                    new()
                    {
                        Id = 102, ServicoId = 1, ParentId = 101, Nome = "Levantamento laser e pontos hidráulicos", Ordem = 1,
                        Coluna = "Finalizada", DtInicioPrevista = new DateOnly(2026, 10, 5), DtPrevista = new DateOnly(2026, 10, 8),
                        DtRealizada = new DateOnly(2026, 10, 8), DuracaoDias = 3, ResponsavelNome = "João Marceneiro"
                    },
                    new()
                    {
                        Id = 103, ServicoId = 1, ParentId = 101, Nome = "Caderno executivo 3D e aprovação final", Ordem = 2,
                        Coluna = "Finalizada", DtInicioPrevista = new DateOnly(2026, 10, 8), DtPrevista = new DateOnly(2026, 10, 12),
                        DtRealizada = new DateOnly(2026, 10, 11), DuracaoDias = 4, ResponsavelNome = "Arq. Sofia Ribeiro"
                    },

                    // Tarefa 2 (Pai estacionada na coluna Iniciada com 3 sub-tarefas abaixo dela!)
                    new()
                    {
                        Id = 104, ServicoId = 1, PredecessoraId = 101, Nome = "Produção na oficina (Marcenaria)", Ordem = 2,
                        Coluna = "Iniciada", DtInicioPrevista = new DateOnly(2026, 10, 13), DtPrevista = new DateOnly(2026, 11, 2),
                        DtInicioReal = new DateOnly(2026, 10, 13), DuracaoDias = 20, ResponsavelNome = "João Marceneiro"
                    },
                    new()
                    {
                        Id = 105, ServicoId = 1, ParentId = 104, Nome = "Plano de corte e usinagem MDF Freijó", Ordem = 1,
                        Coluna = "Finalizada", DtInicioPrevista = new DateOnly(2026, 10, 13), DtPrevista = new DateOnly(2026, 10, 20),
                        DtInicioReal = new DateOnly(2026, 10, 13), DtRealizada = new DateOnly(2026, 10, 19),
                        DuracaoDias = 7, ResponsavelNome = "João Marceneiro"
                    },
                    new()
                    {
                        Id = 106, ServicoId = 1, ParentId = 104, Nome = "Colagem de fita de borda e pintura Laca", Ordem = 2,
                        Coluna = "Em andamento", DtInicioPrevista = new DateOnly(2026, 10, 20), DtPrevista = new DateOnly(2026, 10, 28),
                        DtInicioReal = new DateOnly(2026, 10, 20), DuracaoDias = 8, ResponsavelNome = "Pedro Ajudante"
                    },
                    new()
                    {
                        Id = 107, ServicoId = 1, ParentId = 104, Nome = "Pré-montagem de gaveteiros e ferragens Blum", Ordem = 3,
                        Coluna = "Em andamento", DtInicioPrevista = new DateOnly(2026, 10, 28), DtPrevista = new DateOnly(2026, 11, 2),
                        DtInicioReal = new DateOnly(2026, 10, 28), DuracaoDias = 5, ResponsavelNome = "João Marceneiro"
                    },

                    // Tarefa 3 (Bloqueada em A iniciar porque depende da conclusão da Tarefa 2!)
                    new()
                    {
                        Id = 108, ServicoId = 1, PredecessoraId = 104, Nome = "Montagem e instalação no Apto 402", Ordem = 3,
                        Coluna = "A iniciar", DtInicioPrevista = new DateOnly(2026, 11, 3), DtPrevista = new DateOnly(2026, 11, 11),
                        DuracaoDias = 8, ResponsavelNome = "João Marceneiro"
                    },
                    // Tarefa 4 (Bloqueada em A iniciar porque depende da Tarefa 3!)
                    new()
                    {
                        Id = 109, ServicoId = 1, PredecessoraId = 108, Nome = "Regulagem fina, limpeza e vistoria", Ordem = 4,
                        Coluna = "A iniciar", DtInicioPrevista = new DateOnly(2026, 11, 11), DtPrevista = new DateOnly(2026, 11, 15),
                        DuracaoDias = 4, ResponsavelNome = "Pedro Ajudante"
                    }
                ]
            },

            // Serviço 2: Roberto Almeida — Dormitório / Closet (Em andamento)
            new()
            {
                Id = 2, ClienteId = 2, TipoServicoId = 2, Status = "andamento",
                ModoExecucao = "oficina", InstalacaoOrigem = "cliente",
                EnderecoInstalacao = "Alameda Lorena, 1180 - Apto 121, Jardins, São Paulo/SP",
                DtInicio = new DateOnly(2026, 10, 15), DtInicioEfetivo = new DateOnly(2026, 10, 16),
                DtEntrega = new DateOnly(2026, 11, 20), PrazoDias = 36, ValorContrato = 29400_00,
                Equipe =
                [
                    new() { MembroId = 1, Papel = "Marceneiro Responsável", ComissaoPct = 0 }
                ],
                Etapas =
                [
                    new()
                    {
                        Id = 201, ServicoId = 2, Nome = "Detalhamento executivo e aprovação", Ordem = 1,
                        Coluna = "Entregue", DtInicioPrevista = new DateOnly(2026, 10, 15), DtPrevista = new DateOnly(2026, 10, 21),
                        DtRealizada = new DateOnly(2026, 10, 20), DuracaoDias = 6, ResponsavelNome = "João Marceneiro"
                    },
                    new()
                    {
                        Id = 202, ServicoId = 2, PredecessoraId = 201, Nome = "Corte e produção do closet na oficina", Ordem = 2,
                        Coluna = "Em andamento", DtInicioPrevista = new DateOnly(2026, 10, 22), DtPrevista = new DateOnly(2026, 11, 10),
                        DtInicioReal = new DateOnly(2026, 10, 22), DuracaoDias = 19, ResponsavelNome = "João Marceneiro"
                    },
                    new()
                    {
                        Id = 203, ServicoId = 2, PredecessoraId = 202, Nome = "Montagem de armários e portas reflecta", Ordem = 3,
                        Coluna = "A iniciar", DtInicioPrevista = new DateOnly(2026, 11, 11), DtPrevista = new DateOnly(2026, 11, 20),
                        DuracaoDias = 9, ResponsavelNome = "Pedro Ajudante"
                    }
                ]
            },

            // Serviço 3: Beatriz Nunes — Reforma de Banheiro (100% no Kanban -> Destaque de linha no Grid aguardando confirmação manual de entrega!)
            new()
            {
                Id = 3, ClienteId = 3, TipoServicoId = 4, Status = "andamento",
                ModoExecucao = "no_local", EnderecoOrigem = "cliente",
                EnderecoExecucao = "Rua Ministro Jesuíno Cardoso, 550 - Apto 92, Vila Nova Conceição, São Paulo/SP",
                DtInicio = new DateOnly(2026, 10, 1), DtInicioEfetivo = new DateOnly(2026, 10, 1),
                DtEntrega = new DateOnly(2026, 10, 30), PrazoDias = 29, ValorContrato = 24800_00,
                Equipe =
                [
                    new() { MembroId = 4, Papel = "Mestre de Obras", ComissaoPct = 0 },
                    new() { MembroId = 3, Papel = "Eletricista", ComissaoPct = 0 }
                ],
                Anexos =
                [
                    new() { Id = 10, NomeArquivo = "Banheiro_Suite_Antes.jpg", Tipo = "foto", Momento = "antes", EtapaNome = "Demolição e impermeabilização Box", VisivelPortal = true, DataUpload = "01/10/2026" },
                    new() { Id = 11, NomeArquivo = "Banheiro_Suite_Porcelanato_Finalizado.jpg", Tipo = "foto", Momento = "depois", EtapaNome = "Instalação de bancada, metais e box", VisivelPortal = true, DataUpload = "29/10/2026" }
                ],
                Etapas =
                [
                    new()
                    {
                        Id = 301, ServicoId = 3, Nome = "Demolição e impermeabilização Box", Ordem = 1,
                        Coluna = "Entregue", DtInicioPrevista = new DateOnly(2026, 10, 1), DtPrevista = new DateOnly(2026, 10, 8),
                        DtRealizada = new DateOnly(2026, 10, 8), DuracaoDias = 7, ResponsavelNome = "Carlos Mestre de Obras"
                    },
                    new()
                    {
                        Id = 302, ServicoId = 3, PredecessoraId = 301, Nome = "Assentamento de porcelanato e nicho", Ordem = 2,
                        Coluna = "Entregue", DtInicioPrevista = new DateOnly(2026, 10, 9), DtPrevista = new DateOnly(2026, 10, 21),
                        DtRealizada = new DateOnly(2026, 10, 20), DuracaoDias = 12, ResponsavelNome = "Carlos Mestre de Obras"
                    },
                    new()
                    {
                        Id = 303, ServicoId = 3, PredecessoraId = 302, Nome = "Instalação de bancada, metais e box", Ordem = 3,
                        Coluna = "Entregue", DtInicioPrevista = new DateOnly(2026, 10, 22), DtPrevista = new DateOnly(2026, 10, 30),
                        DtRealizada = new DateOnly(2026, 10, 29), DuracaoDias = 8, ResponsavelNome = "Marcos Eletricista"
                    }
                ]
            },

            // Serviço 4: Paulo Henrique — Serviço já encerrado (para demonstrar filtro de encerrados e portal pós-obra)
            new()
            {
                Id = 4, ClienteId = 6, TipoServicoId = 4, Status = "encerrada",
                ModoExecucao = "no_local", EnderecoOrigem = "cliente",
                EnderecoExecucao = "Rua Apinajés, 310 - Apto 61, Sumaré, São Paulo/SP",
                DtInicio = new DateOnly(2026, 9, 1), DtInicioEfetivo = new DateOnly(2026, 9, 2),
                DtEntrega = new DateOnly(2026, 9, 28), DtEntregaEfetiva = new DateOnly(2026, 9, 27),
                PrazoDias = 27, ValorContrato = 19500_00,
                Etapas =
                [
                    new()
                    {
                        Id = 401, ServicoId = 4, Nome = "Reforma completa do banheiro social", Ordem = 1,
                        Coluna = "Entregue", DtInicioPrevista = new DateOnly(2026, 9, 1), DtPrevista = new DateOnly(2026, 9, 28),
                        DtRealizada = new DateOnly(2026, 9, 27), DuracaoDias = 27, ResponsavelNome = "Carlos Mestre de Obras"
                    }
                ]
            }
        ]);

        // Lançamentos Financeiros (Uma fonte, três recortes: Geral, Serviço e Cliente)
        Lancamentos.AddRange(
        [
            new() { Id = 1, ServicoId = 1, ClienteId = 1, Pagar = false, Natureza = "parcela", Status = "pago", Descricao = "Parcela 1/3 (Sinal) — Cozinha Planejada Apto 402", BeneficiarioOuCliente = "Mariana Costa", ValorCentavos = 18500_00, Vencimento = new DateOnly(2026, 10, 5), PagoEm = "05/10/2026" },
            new() { Id = 2, ServicoId = 1, ClienteId = 1, Pagar = false, Natureza = "parcela", Status = "pendente", Descricao = "Parcela 2/3 — Cozinha Planejada Apto 402", BeneficiarioOuCliente = "Mariana Costa", ValorCentavos = 18500_00, Vencimento = new DateOnly(2026, 11, 5) },
            new() { Id = 3, ServicoId = 1, ClienteId = 1, Pagar = false, Natureza = "parcela", Status = "pendente", Descricao = "Parcela 3/3 (Entrega) — Cozinha Planejada Apto 402", BeneficiarioOuCliente = "Mariana Costa", ValorCentavos = 18500_00, Vencimento = new DateOnly(2026, 12, 5) },
            new() { Id = 4, ServicoId = 1, ClienteId = 1, Pagar = true, Natureza = "comissao", Status = "pendente", Descricao = "Comissão Indicação (8% snapshot) — Cozinha Apto 402", BeneficiarioOuCliente = "Arq. Sofia Ribeiro", MembroEquipeId = 6, ValorCentavos = 4440_00, Vencimento = new DateOnly(2026, 11, 10) },
            new() { Id = 5, ServicoId = 1, ClienteId = 1, Pagar = true, Natureza = "material", Status = "pago", Descricao = "Chapas MDF Louro Freijó + Ferragens Blum", BeneficiarioOuCliente = "Fornecedor Duratex/Blum", ValorCentavos = 16200_00, Vencimento = new DateOnly(2026, 10, 12), PagoEm = "12/10/2026" },

            new() { Id = 6, ServicoId = 2, ClienteId = 2, Pagar = false, Natureza = "parcela", Status = "pago", Descricao = "Entrada 50% — Closet Suíte Master", BeneficiarioOuCliente = "Roberto Almeida", ValorCentavos = 14700_00, Vencimento = new DateOnly(2026, 10, 15), PagoEm = "15/10/2026" },
            new() { Id = 7, ServicoId = 2, ClienteId = 2, Pagar = false, Natureza = "parcela", Status = "pendente", Descricao = "Saldo 50% na Montagem — Closet Suíte Master", BeneficiarioOuCliente = "Roberto Almeida", ValorCentavos = 14700_00, Vencimento = new DateOnly(2026, 11, 20) },
            new() { Id = 8, ServicoId = 2, ClienteId = 2, Pagar = true, Natureza = "material", Status = "pendente", Descricao = "Perfis alumínio e portas espelho reflecta", BeneficiarioOuCliente = "Vidraçaria & Perfis SP", ValorCentavos = 8900_00, Vencimento = new DateOnly(2026, 11, 2) },

            new() { Id = 9, ServicoId = 3, ClienteId = 3, Pagar = false, Natureza = "parcela", Status = "pago", Descricao = "Parcela 1/2 — Reforma Banheiro Apto 92", BeneficiarioOuCliente = "Beatriz Nunes", ValorCentavos = 12400_00, Vencimento = new DateOnly(2026, 10, 2), PagoEm = "02/10/2026" },
            new() { Id = 10, ServicoId = 3, ClienteId = 3, Pagar = false, Natureza = "parcela", Status = "vencido", Descricao = "Parcela 2/2 (Final) — Reforma Banheiro Apto 92", BeneficiarioOuCliente = "Beatriz Nunes", ValorCentavos = 12400_00, Vencimento = new DateOnly(2026, 10, 28) },
            new() { Id = 11, ServicoId = 3, ClienteId = 3, Pagar = true, Natureza = "mao-de-obra", Status = "vencido", Descricao = "Diárias finais revestimento e bancada", BeneficiarioOuCliente = "Carlos Mestre de Obras", MembroEquipeId = 4, ValorCentavos = 4800_00, Vencimento = new DateOnly(2026, 10, 28) },
            new() { Id = 12, ServicoId = 3, ClienteId = 3, Pagar = true, Natureza = "material", Status = "pago", Descricao = "Bancada quartzo branco e nicho", BeneficiarioOuCliente = "Marmoraria Itália", MembroEquipeId = 8, ValorCentavos = 5200_00, Vencimento = new DateOnly(2026, 10, 18), PagoEm = "18/10/2026" }
        ]);
    }

    // Helpers de Domínio
    public ClienteItem? GetCliente(int id) => Clientes.FirstOrDefault(c => c.Id == id);
    public TipoServicoItem? GetTipoServico(int id) => TiposServico.FirstOrDefault(t => t.Id == id);
    public MembroEquipe? GetMembroEquipe(int id) => Equipe.FirstOrDefault(m => m.Id == id);

    public static int GetPctColuna(string coluna) => coluna switch
    {
        "A iniciar" => 0,
        "Iniciada" => 10,
        "Em andamento" => 50,
        "Finalizada" => 90,
        "Entregue" => 100,
        _ => 0
    };

    public int GetPctEtapa(ServicoItem s, ServicoEtapaItem e)
    {
        var filhas = s.Etapas.Where(x => x.ParentId == e.Id).ToList();
        if (filhas.Count == 0)
            return GetPctColuna(e.Coluna);

        if (e.Coluna == "A iniciar") return 0;
        if (e.Coluna == "Entregue") return 100;

        // Tarefa pai em Iniciada: média ponderada pela duração das filhas (piso 10%, teto 90%)
        var somaPesos = filhas.Sum(f => Math.Max(1, f.DuracaoDias));
        var somaPond = filhas.Sum(f => GetPctColuna(f.Coluna) * Math.Max(1, f.DuracaoDias));
        var calc = (int)Math.Round(somaPond / (double)somaPesos);
        return Math.Clamp(calc, 10, 90);
    }

    public int GetPctCalculadoServico(ServicoItem s)
    {
        if (s.Status == "encerrada") return 100;
        var topos = s.Etapas.Where(x => x.ParentId == null).ToList();
        if (topos.Count == 0) return 0;
        var somaPesos = topos.Sum(t => Math.Max(1, t.DuracaoDias));
        var somaPond = topos.Sum(t => GetPctEtapa(s, t) * Math.Max(1, t.DuracaoDias));
        return Math.Clamp((int)Math.Round(somaPond / (double)somaPesos), 0, 100);
    }

    public int GetPctExibidoServico(ServicoItem s)
    {
        if (s.Status == "encerrada") return 100;
        return Math.Clamp(GetPctCalculadoServico(s) + s.AjustePctDelta, 0, 100);
    }

    public bool IsProntoParaFinalizar(ServicoItem s)
    {
        if (s.Status != "andamento") return false;
        var topos = s.Etapas.Where(x => x.ParentId == null).ToList();
        return topos.Count > 0 && topos.All(t => t.Coluna == "Entregue");
    }

    public string GetFaseAtualServico(ServicoItem s)
    {
        if (s.Status == "encerrada") return "Entregue ✓";
        if (s.Status == "suspenso") return "Suspenso ⏸";
        if (s.Status == "cancelada") return "Cancelado ✗";
        if (IsProntoParaFinalizar(s)) return "Aguardando entrega final ★";

        var ativas = s.Etapas
            .Where(x => x.ParentId == null && (x.Coluna == "Em andamento" || x.Coluna == "Iniciada"))
            .OrderBy(x => x.Ordem)
            .ToList();

        if (ativas.Count == 0) return "A iniciar";
        if (ativas.Count == 1) return ativas[0].Nome;
        return $"{ativas[0].Nome} (+{ativas.Count - 1})";
    }

    public (string CssClass, string DotClass, string Label) GetSaudeEtapa(ServicoEtapaItem e)
    {
        var hoje = new DateOnly(2026, 10, 30);
        if (e.Coluna is "Finalizada" or "Entregue")
            return ("h-blue", "s-ok", "Concluída (Azul)");
        if (hoje > e.DtPrevista)
            return ("h-red", "s-err", "Atrasada (Vermelha)");

        var totalDias = Math.Max(1, e.DtPrevista.DayNumber - e.DtInicioPrevista.DayNumber);
        var decorridos = hoje.DayNumber - e.DtInicioPrevista.DayNumber;
        var pctPrazo = (decorridos * 100.0) / totalDias;

        if (e.Coluna == "A iniciar")
        {
            if (hoje >= e.DtInicioPrevista)
                return ("h-yellow", "s-warn", "Deveria ter iniciado (Amarela)");
            return ("h-white", "s-ok", "A iniciar no prazo (Branca)");
        }

        if (pctPrazo >= KanbanAlertaPct)
            return ("h-yellow", "s-warn", $"Atenção ≥{KanbanAlertaPct}% do prazo (Amarela)");

        return ("h-green", "s-ok", "Em dia (Verde)");
    }

    public (string DotClass, string Label) GetSaudeServico(ServicoItem s)
    {
        if (s.Status == "encerrada") return ("s-ok", "Entregue");
        if (s.Status == "suspenso") return ("s-warn", "Suspenso");
        var saudes = s.Etapas.Select(GetSaudeEtapa).ToList();
        if (saudes.Any(x => x.CssClass == "h-red")) return ("s-err", "Etapa em atraso");
        if (saudes.Any(x => x.CssClass == "h-yellow")) return ("s-warn", "Atenção no prazo");
        return ("s-ok", "No prazo");
    }

    public bool TryMoverEtapaKanban(ServicoItem s, ServicoEtapaItem etapa, string destino, out string mensagem)
    {
        mensagem = "";
        if (!HasPerm("kanban.move") && !HasPerm("servicos.edit"))
        {
            mensagem = $"Seu perfil ({CurrentRole}) possui apenas visualização em Serviços/Kanban e não pode movimentar tarefas.";
            return false;
        }
        if (s.Status == "suspenso")
        {
            mensagem = "Serviço está SUSPENSO. Retome o serviço antes de movimentar tarefas no Kanban.";
            return false;
        }
        if (etapa.Coluna == destino) return true;

        var filhas = s.Etapas.Where(x => x.ParentId == etapa.Id).ToList();
        bool temFilhas = filhas.Count > 0;

        // Regra de precedência (predecessoraId)
        if (etapa.PredecessoraId is int predId && destino != "A iniciar")
        {
            var pred = s.Etapas.FirstOrDefault(x => x.Id == predId);
            if (pred is not null)
            {
                bool predTemFilhas = s.Etapas.Any(x => x.ParentId == pred.Id);
                bool predConcluida = predTemFilhas
                    ? pred.Coluna == "Entregue"
                    : pred.Coluna is "Finalizada" or "Entregue";

                if (!predConcluida)
                {
                    mensagem = $"Bloqueado: a tarefa anterior \"{pred.Nome}\" precisa estar {(predTemFilhas ? "Entregue" : "Finalizada")} antes de iniciar esta tarefa.";
                    return false;
                }
            }
        }

        // Regra de Tarefa Pai com sub-tarefas
        if (temFilhas)
        {
            if (destino is "Em andamento" or "Finalizada")
            {
                mensagem = "Esta tarefa possui sub-tarefas: ela deve permanecer na coluna 'Iniciada' enquanto as sub-tarefas avançam por 'Em andamento' e 'Finalizada'.";
                return false;
            }
            if (destino == "Entregue" && !filhas.All(f => f.Coluna is "Finalizada" or "Entregue"))
            {
                mensagem = "A tarefa pai só pode ser movida para 'Entregue' quando TODAS as suas sub-tarefas estiverem em 'Finalizada'.";
                return false;
            }
        }

        // Regra de Sub-tarefa (filha)
        if (etapa.ParentId is int parentId)
        {
            var pai = s.Etapas.FirstOrDefault(x => x.Id == parentId);
            if (pai is not null && pai.Coluna != "Iniciada")
            {
                mensagem = $"A tarefa pai \"{pai.Nome}\" precisa estar na coluna 'Iniciada' para movimentar suas sub-tarefas.";
                return false;
            }
            if (destino is "A iniciar" or "Entregue")
            {
                mensagem = "Sub-tarefas permanecem abaixo da tarefa pai em 'Iniciada' ou avançam pelas colunas 'Em andamento' e 'Finalizada'. Quando todas finalizarem, mova a tarefa pai para 'Entregue'.";
                return false;
            }
        }

        etapa.Coluna = destino;
        var hoje = new DateOnly(2026, 10, 30);
        if (destino == "Iniciada")
        {
            etapa.DtInicioReal ??= hoje;
            s.DtInicioEfetivo ??= hoje;
        }
        else if (destino is "Finalizada" or "Entregue")
        {
            etapa.DtRealizada = hoje;
        }

        NotifyStateChanged();
        return true;
    }

    public string RenderContratoTexto(int clienteId, int negociacaoId, int templateId)
    {
        var cli = GetCliente(clienteId);
        var neg = Negociacoes.FirstOrDefault(n => n.Id == negociacaoId);
        var tpl = ModelosContrato.FirstOrDefault(m => m.Id == templateId) ?? ModelosContrato.First();
        var tipo = neg is not null ? GetTipoServico(neg.TipoServicoId) : null;
        var vis = neg?.Visitas.LastOrDefault(v => v.Status == "realizada") ?? neg?.Visitas.LastOrDefault();
        var orc = neg?.Orcamentos.LastOrDefault(o => o.Status == "aceito") ?? neg?.Orcamentos.LastOrDefault();

        var txt = tpl.CorpoTexto;
        var map = new Dictionary<string, string>
        {
            ["{{cliente.nome}}"] = cli?.Nome ?? "—",
            ["{{cliente.cpf_cnpj}}"] = string.IsNullOrWhiteSpace(cli?.CpfCnpj) ? "[CPF/CNPJ PENDENTE NA ABA2]" : cli.CpfCnpj,
            ["{{cliente.rg}}"] = string.IsNullOrWhiteSpace(cli?.Rg) ? "[RG PENDENTE NA ABA2]" : $"{cli.Rg} {cli.OrgaoEmissor}".Trim(),
            ["{{cliente.estado_civil}}"] = cli?.EstadoCivil ?? "—",
            ["{{cliente.profissao}}"] = cli?.Profissao ?? "—",
            ["{{cliente.nacionalidade}}"] = cli?.Nacionalidade ?? "Brasileira",
            ["{{cliente.endereco_completo}}"] = cli?.EnderecoCompleto ?? "[ENDEREÇO PENDENTE]",
            ["{{cliente.celular}}"] = cli?.Celular ?? "—",
            ["{{cliente.email}}"] = cli?.Email ?? "—",
            ["{{servico.tipo}}"] = tipo?.Nome ?? "—",
            ["{{servico.categoria}}"] = tipo?.Categoria ?? "—",
            ["{{servico.endereco_execucao}}"] = cli?.EnderecoCompleto ?? "—",
            ["{{medicao.data}}"] = vis?.DataRealizada ?? vis?.DataAgendada ?? "—",
            ["{{medicao.ambientes_medidas}}"] = vis?.Medicoes ?? "[Sem medições registradas]",
            ["{{medicao.observacoes}}"] = vis?.Observacoes ?? "—",
            ["{{orcamento.versao}}"] = orc?.Versao.ToString() ?? "1",
            ["{{orcamento.valor_total}}"] = orc is not null ? FormatMoeda(orc.Valor) : "R$ 0,00",
            ["{{orcamento.valor_extenso}}"] = orc is not null ? $"{FormatMoeda(orc.Valor)} conforme orçamento aprovado" : "—",
            ["{{orcamento.condicoes_pagamento}}"] = orc?.CondicoesPagamento ?? "Conforme orçamento aprovado",
            ["{{orcamento.prazo_execucao_dias}}"] = orc?.PrazoExecucaoDias.ToString() ?? "30",
            ["{{orcamento.escopo_resumo}}"] = orc?.EscopoResumo ?? "Conforme memorial descritivo",
            ["{{contrato.data_emissao}}"] = "30/10/2026",
            ["{{contrato.cidade_foro}}"] = CidadeForo
        };

        foreach (var kv in map)
            txt = txt.Replace(kv.Key, kv.Value);

        return txt;
    }

    public static string FormatMoeda(long centavos) =>
        (centavos / 100.0m).ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
}

public class UsuarioSistema
{
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "Comercial";
    public bool Ativo { get; set; } = true;
}

public class TipoServicoItem
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Categoria { get; set; } = "mobiliario"; // alvenaria | mobiliario
    public bool Ativo { get; set; } = true;
    public List<TemplateEtapaItem> EtapasPadrao { get; set; } = [];
}

public class TemplateEtapaItem
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public int? PredecessoraId { get; set; }
    public string Nome { get; set; } = "";
    public int Ordem { get; set; } = 1;
    public int DuracaoDias { get; set; } = 5;
}

public class MembroEquipe
{
    public int Id { get; set; }
    public string Perfil { get; set; } = "Colaborador"; // Funcionário | Colaborador | Parceiro
    public string Nome { get; set; } = "";
    public string Funcao { get; set; } = "";
    public string TipoParceiro { get; set; } = "arquiteto";
    public string Celular { get; set; } = "";
    public string Email { get; set; } = "";
    public string CpfCnpj { get; set; } = "";
    public string Pix { get; set; } = "";
    public int? ComissaoPct { get; set; }
    public bool Ativo { get; set; } = true;
    public string MotivoInativacao { get; set; } = "";
    public string BadgeCls => Perfil switch { "Colaborador" => "b-colab", "Parceiro" => "b-parc", _ => "b-func" };
}

public class ClienteItem
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Celular { get; set; } = "";
    public string Tel2 { get; set; } = "";
    public string Email { get; set; } = "";
    public string Rua { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Complemento { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cidade { get; set; } = "São Paulo";
    public string Uf { get; set; } = "SP";
    public string Cep { get; set; } = "";
    public string Origem { get; set; } = "lp"; // lp | instagram | indicacao | parceiro | outro
    public string Observacoes { get; set; } = "";
    public string CpfCnpj { get; set; } = "";
    public string Rg { get; set; } = "";
    public string OrgaoEmissor { get; set; } = "SSP/SP";
    public string EstadoCivil { get; set; } = "";
    public string Profissao { get; set; } = "";
    public string Nacionalidade { get; set; } = "Brasileira";
    public bool IsLead { get; set; } = true;
    public string Status { get; set; } = "ativo"; // ativo | inativo | cancelado | suspenso
    public string MotivoStatus { get; set; } = "";
    public string PortalToken { get; set; } = "";
    public string PortalExpiracao { get; set; } = "90 dias pós-entrega";
    public List<EventoTimelineItem> Timeline { get; set; } = [];
    public List<MensagemCrmItem> Mensagens { get; set; } = [];

    public string EnderecoCompleto =>
        string.IsNullOrWhiteSpace(Rua)
            ? ""
            : $"{Rua}, {Numero}{(string.IsNullOrWhiteSpace(Complemento) ? "" : $" - {Complemento}")}, {Bairro}, {Cidade}/{Uf}{(string.IsNullOrWhiteSpace(Cep) ? "" : $" - CEP {Cep}")}";
}

public record EventoTimelineItem(string Quando, string Usuario, string Descricao);
public record MensagemCrmItem(string Quando, string CanalAutor, string Texto);

public class NegociacaoItem
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int TipoServicoId { get; set; }
    public string Estagio { get; set; } = "Contato"; // Novo | Contato | Visita/Medição | Orçamento | Negociação | Ganho | Perdido
    public string Status { get; set; } = "em_andamento"; // em_andamento | fechada | cancelada
    public long ValorEstimado { get; set; }
    public string Origem { get; set; } = "lp";
    public string MotivoPerda { get; set; } = "";
    public string SubmotivoPerda { get; set; } = "";
    public string ObsPerda { get; set; } = "";
    public int? NegociacaoAnteriorId { get; set; }
    public string CriadoEm { get; set; } = "30/10/2026";
    public int ParadoDias { get; set; }
    public List<VisitaItem> Visitas { get; set; } = [];
    public List<OrcamentoItem> Orcamentos { get; set; } = [];
}

public class VisitaItem
{
    public int Id { get; set; }
    public int NegociacaoId { get; set; }
    public string DataAgendada { get; set; } = "";
    public string DataRealizada { get; set; } = "";
    public string Status { get; set; } = "agendada"; // agendada | realizada | cancelada
    public string Medicoes { get; set; } = "";
    public string Observacoes { get; set; } = "";
}

public class OrcamentoItem
{
    public int Id { get; set; }
    public int NegociacaoId { get; set; }
    public int Versao { get; set; } = 1;
    public long Valor { get; set; }
    public string CondicoesPagamento { get; set; } = "";
    public int PrazoExecucaoDias { get; set; } = 30;
    public string EscopoResumo { get; set; } = "";
    public string Status { get; set; } = "enviado"; // rascunho | enviado | aceito | recusado | substituido
    public string ArquivoPdf { get; set; } = "";
    public string EnviadoEm { get; set; } = "";
    public string RespondidoEm { get; set; } = "";
}

public class ContratoTemplateItem
{
    public int Id { get; set; }
    public int TipoServicoId { get; set; }
    public string Nome { get; set; } = "";
    public string Versao { get; set; } = "v1.0";
    public bool Ativo { get; set; } = true;
    public string CorpoTexto { get; set; } = "";
}

public class ContratoEmitidoItem
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int NegociacaoId { get; set; }
    public int OrcamentoId { get; set; }
    public int? ServicoId { get; set; }
    public int TemplateId { get; set; }
    public string Status { get; set; } = "rascunho"; // rascunho | enviado | aguardando | assinado | cancelado
    public string EnviadoEm { get; set; } = "—";
    public string AssinadoEm { get; set; } = "—";
    public string HashDoc { get; set; } = "";
    public string PdfNome { get; set; } = "";
    public string TextoRenderizado { get; set; } = "";
    public string MotivoCancelamento { get; set; } = "";
}

public class ServicoItem
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int TipoServicoId { get; set; }
    public int? NegociacaoId { get; set; }
    public string Status { get; set; } = "andamento"; // andamento | suspenso | encerrada | cancelada
    public string MotivoStatus { get; set; } = "";
    public string ModoExecucao { get; set; } = "no_local"; // no_local | oficina
    public string EnderecoOrigem { get; set; } = "cliente"; // cliente | informado
    public string EnderecoExecucao { get; set; } = "";
    public string InstalacaoOrigem { get; set; } = "cliente"; // cliente | informado
    public string EnderecoInstalacao { get; set; } = "";
    public DateOnly DtInicio { get; set; } = new(2026, 10, 30);
    public DateOnly? DtInicioEfetivo { get; set; }
    public DateOnly DtEntrega { get; set; } = new(2026, 11, 30);
    public DateOnly? DtEntregaEfetiva { get; set; }
    public int PrazoDias { get; set; } = 30;
    public int DiasSuspensos { get; set; }
    public long ValorContrato { get; set; }
    public int AjustePctDelta { get; set; }
    public List<AjustePctItem> HistoricoAjustes { get; set; } = [];
    public List<ServicoEtapaItem> Etapas { get; set; } = [];
    public List<ServicoEquipeItem> Equipe { get; set; } = [];
    public List<AnexoServicoItem> Anexos { get; set; } = [];
}

public record AjustePctItem(string Quando, string Usuario, int De, int Para, int CalculadoNaHora, int Delta, string Motivo);

public class ServicoEtapaItem
{
    public int Id { get; set; }
    public int ServicoId { get; set; }
    public int? ParentId { get; set; }
    public int? PredecessoraId { get; set; }
    public string Nome { get; set; } = "";
    public int Ordem { get; set; } = 1;
    public string Coluna { get; set; } = "A iniciar"; // A iniciar | Iniciada | Em andamento | Finalizada | Entregue
    public DateOnly DtInicioPrevista { get; set; } = new(2026, 10, 30);
    public DateOnly DtPrevista { get; set; } = new(2026, 11, 5);
    public DateOnly? DtInicioReal { get; set; }
    public DateOnly? DtRealizada { get; set; }
    public int DuracaoDias { get; set; } = 5;
    public string ResponsavelNome { get; set; } = "";
}

public class ServicoEquipeItem
{
    public int MembroId { get; set; }
    public string Papel { get; set; } = "";
    public int ComissaoPct { get; set; }
}

public class AnexoServicoItem
{
    public int Id { get; set; }
    public string NomeArquivo { get; set; } = "";
    public string Tipo { get; set; } = "foto"; // foto | documento
    public string Momento { get; set; } = "andamento"; // antes | andamento | depois
    public string EtapaNome { get; set; } = "";
    public bool VisivelPortal { get; set; } = true;
    public string DataUpload { get; set; } = "30/10/2026";
    public bool Ativo { get; set; } = true;
}

public class LancamentoFinanceiro
{
    public int Id { get; set; }
    public int ServicoId { get; set; }
    public int ClienteId { get; set; }
    public bool Pagar { get; set; } // false = Receber ▼, true = Pagar ▲
    public string Natureza { get; set; } = "parcela"; // parcela | comissao | material | mao-de-obra
    public string Status { get; set; } = "pendente"; // pendente | pago | vencido | cancelado
    public string Descricao { get; set; } = "";
    public string BeneficiarioOuCliente { get; set; } = "";
    public int? MembroEquipeId { get; set; }
    public long ValorCentavos { get; set; }
    public DateOnly Vencimento { get; set; } = new(2026, 11, 10);
    public string PagoEm { get; set; } = "—";
    public string MotivoCancelamento { get; set; } = "";
}

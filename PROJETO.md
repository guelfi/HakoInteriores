# Hako - Projeto

## Descrição
Website estático institucional.

## Stack
- HTML5
- CSS3
- JavaScript (Vanilla)

## Estrutura
```
Hako/
├── index.html    # Página principal
├── index.css     # Estilos
├── index.js      # Scripts
├── images/       # Imagens
└── scripts/      # Scripts adicionais
```

## Ambiente Local

### Execução
```bash
cd /home/guelfi/Projetos/Hako
npx http-server -p 3005
```

### URL Local
- http://localhost:3005/
- http://localhost/ (via Nginx)

## Ambiente
- **Produção**: [https://www.hakointeriores.com.br](https://www.hakointeriores.com.br)
- **Local**: [http://localhost/](http://localhost/)
- **Infra**: OCI (Docker + Nginx Proxy)
- **SSL**: Cloudflare (Certificado Próprio)

### Deploy
```bash
# O Hako é estático, basta copiar os arquivos para o servidor
# Via Nginx já configurado no container hako-website
```

## Configurações
- Não requer banco de dados
- Não requer API
- Arquivos estáticos servidos pelo Nginx

# Hako - Status

## Status Atual
✅ **Funcional** - Website em produção

## Ambiente
| Ambiente | Status | URL |
|----------|--------|-----|
| Local | ✅ Funcional | http://localhost/ |
| Produção (OCI) | ✅ Concluído (15/05/2026) | https://hakointeriores.com.br/ |

## Detalhes do Deployment
- **Ambiente de Produção**: OCI (Oracle Cloud Infrastructure) - Docker Container
- **URL**: [https://www.hakointeriores.com.br](https://www.hakointeriores.com.br)
- **CI/CD**: Operacional via GitHub Actions (OCI Deploy)
- **Arquitetura**: Site estático servido por container Docker `hako-website` atrás de um Proxy Reverso Nginx centralizado.
- **SSL**: Gerenciado pela Cloudflare (Proxy On). Certificado SSL Próprio/Dedicado.

## Última Atualização
- Início da migração para o domínio próprio `hakointeriores.com.br`
- Restauração e correção do workflow de CI/CD
- Remoção do prefixo `/hako/` para servir na raiz do domínio
- Ajuste de UI/UX: Layout 100vh, scroll suave e alinhamento de seções
- Atualização do menu principal para "Contato"
- Otimização do Workflow CI/CD (Build de container e rede automática)

## Tarefas Pendentes
- Nenhuma tarefa pendente no momento

## Observações
- Projeto estático simples
- Não requer manutenção frequente

# Deploy OCI — Hako Admin (/dashboard)

Deploy do app Blazor Server `backend/Hako.Admin` em produção, respondendo em
`https://www.hakointeriores.com.br/dashboard`.

## Arquitetura aplicada (servidor 129.153.86.168)

```
Internet → Cloudflare → nginx-proxy (container, portas 80/443)
                            │
                            ├─ server hakointeriores.com.br (80 e 443)
                            │     location /           → hako-website:80
                            │     location ^~ /dashboard → hako-admin:8080
                            │
                            └─ rede Docker www_projetos-net (compartilhada)
                                  ├─ hako-website (site estático)
                                  └─ hako-admin   (Blazor Server, PathBase=/dashboard)
```

Pontos importantes do servidor:

- O `nginx-proxy` (imagem `nginx:stable-alpine`) usa **um único arquivo de
  configuração**: `/var/www/nginx/nginx.conf` no host, montado read-only em
  `/etc/nginx/nginx.conf`. **Não existe conf.d** — qualquer location nova é
  editada diretamente nesse arquivo (com backup), seguida de
  `docker exec nginx-proxy nginx -t && docker exec nginx-proxy nginx -s reload`.
- O arquivo segue o padrão `set $upstream_x http://container:porta;` +
  `proxy_pass $upstream_x;` — assim o nome Docker é resolvido em runtime pelo
  `resolver 127.0.0.11` e o nginx não falha ao subir se o container estiver
  fora do ar. A location `/dashboard` usa `set $upstream_hako_admin
  http://hako-admin:8080;` no mesmo padrão (sem rewrite: o URI completo chega
  ao app).
- O `map $http_upgrade $connection_upgrade` foi adicionado uma única vez no
  contexto `http{}` (topo do arquivo) — é o que habilita o WebSocket do
  circuito SignalR do Blazor Server. Se outro projeto já tiver adicionado um
  map equivalente, não duplique.
- A `location ^~ /dashboard` existe nos **dois** server blocks do hako
  (`listen 80` e `listen 443 ssl`). O `^~` garante precedência sobre
  `location /` sem depender de regex.
- `hako-admin` roda **sem portas publicadas no host** — só na rede
  `www_projetos-net`. TLS termina no nginx-proxy/Cloudflare.

## O que foi executado (deploy inicial, 2026-10-29)

```bash
# 1. Sync do código
rsync -avz --delete --exclude "bin/" --exclude "obj/" \
  --rsync-path="sudo rsync" \
  ./backend/ ubuntu@129.153.86.168:/var/www/Hako/backend/
rsync -avz --delete --rsync-path="sudo rsync" \
  ./deploy/ ubuntu@129.153.86.168:/var/www/Hako/deploy/

# 2. Build e run (no servidor)
sudo docker build -t hako-admin /var/www/Hako/backend/Hako.Admin
sudo docker rm -f hako-admin 2>/dev/null
sudo docker run -d --name hako-admin --restart unless-stopped \
  -e PathBase=/dashboard -e SiteUrl=/ \
  --network www_projetos-net hako-admin

# 3. Location no nginx (arquivo único — editar com backup)
sudo cp /var/www/nginx/nginx.conf /var/www/nginx/nginx.conf.bak-$(date +%Y%m%d)
# → inserir 'map $http_upgrade $connection_upgrade' no http{}
# → inserir 'location ^~ /dashboard { ... }' nos dois server{} do hako
sudo docker exec nginx-proxy nginx -t && sudo docker exec nginx-proxy nginx -s reload
```

O arquivo `deploy/hako-dashboard.nginx.conf` é a **referência** do bloco
instalado (no servidor real o `proxy_pass` usa a variável
`$upstream_hako_admin`, seguindo o padrão do nginx.conf — semântica idêntica).

## Como atualizar o admin no futuro

Push na `main` tocando `backend/**` dispara o workflow
`.github/workflows/oci-deploy-admin.yml` (secrets `SSH_HOST`, `SSH_USER`,
`SSH_PRIVATE_KEY` — os mesmos do `oci-deploy.yml`). Manualmente:

```bash
rsync -avz --delete --exclude "bin/" --exclude "obj/" \
  --rsync-path="sudo rsync" \
  ./backend/ ubuntu@129.153.86.168:/var/www/Hako/backend/
ssh ubuntu@129.153.86.168 '
  sudo docker build -t hako-admin /var/www/Hako/backend/Hako.Admin &&
  sudo docker rm -f hako-admin &&
  sudo docker run -d --name hako-admin --restart unless-stopped \
    -e PathBase=/dashboard -e SiteUrl=/ \
    --network www_projetos-net hako-admin'
```

A location `/dashboard` é permanente no `nginx.conf` — só precisa ser
reinstalada se o arquivo for recriado do zero (o workflow detecta e reinsere
automaticamente).

## Comandos úteis

```bash
sudo docker logs hako-admin --tail 50
sudo docker restart hako-admin
sudo docker exec hako-admin curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/dashboard
# Rollback da conf do nginx:
sudo cp /var/www/nginx/nginx.conf.bak-YYYYMMDD /var/www/nginx/nginx.conf
sudo docker exec nginx-proxy nginx -t && sudo docker exec nginx-proxy nginx -s reload
```

## Observações

- **Sem autenticação real**: o app é um mock — dados fictícios e a tela de
  login não valida credenciais. Não colocar dados reais até haver auth.
- **Imagem runtime**: o Dockerfile usa `sdk:10.0` também como runtime (imagem
  maior) por conta de pull lento do `aspnet:10.0` no servidor. Melhoria
  futura: trocar o stage final para `mcr.microsoft.com/dotnet/aspnet:10.0`.
- HTTP interno apenas (porta 8080); TLS termina no nginx-proxy/Cloudflare.
- Backup do nginx.conf anterior à instalação:
  `/var/www/nginx/nginx.conf.bak-20261029`.

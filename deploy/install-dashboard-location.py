#!/usr/bin/env python3
"""Insere a location /dashboard do Hako Admin no nginx.conf de arquivo único.

O nginx-proxy de producao NAO usa conf.d: /var/www/nginx/nginx.conf e o
arquivo inteiro (montado ro no container). Este script replica a edicao
manual aplicada em 2026-10-29:

  1. Adiciona 'map $http_upgrade $connection_upgrade' no contexto http{}
     (logo apos a diretiva 'resolver', que ja existe no arquivo).
  2. Adiciona 'location ^~ /dashboard { ... }' dentro de CADA server{}
     cujo server_name e 'hakointeriores.com.br www.hakointeriores.com.br',
     imediatamente antes do 'location / {' do bloco. Segue o padrao do
     arquivo: 'set $upstream_hako_admin http://hako-admin:8080;' +
     'proxy_pass $upstream_hako_admin;' (DNS resolvido em runtime pelo
     resolver 127.0.0.11 — sem rewrite do URI).

Idempotente: se a location (e o map) ja existirem, nao altera nada.

Uso:  sudo python3 install-dashboard-location.py /var/www/nginx/nginx.conf
Depois: docker exec nginx-proxy nginx -t && docker exec nginx-proxy nginx -s reload
"""
import re
import sys

HAKO_SERVER_NAME = "hakointeriores.com.br www.hakointeriores.com.br;"

MAP_BLOCK = """
  # WebSocket upgrade (hako-admin /dashboard — Blazor Server usa SignalR).
  map $http_upgrade $connection_upgrade {
    default upgrade;
    ''      close;
  }
"""

LOCATION_TEMPLATE = """    # Hako Admin (Blazor Server) — PathBase=/dashboard no app; sem rewrite.
    location ^~ /dashboard {{
      set $upstream_hako_admin http://hako-admin:8080;
      proxy_pass $upstream_hako_admin;
      proxy_http_version 1.1;
      proxy_set_header Upgrade $http_upgrade;
      proxy_set_header Connection $connection_upgrade;
      proxy_set_header Host $host;
      proxy_set_header X-Real-IP $remote_addr;
      proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
      proxy_set_header X-Forwarded-Proto {xfp};
      proxy_read_timeout 86400;
      proxy_send_timeout 86400;
    }}
"""


def split_http_level_blocks(lines):
    """Retorna lista de (start_idx, end_idx) dos blocos '  server { ... }'
    no nivel do http{} (indentacao de 2 espacos)."""
    blocks = []
    start = None
    for i, line in enumerate(lines):
        if re.match(r"^  server \{", line):
            start = i
        elif start is not None and re.match(r"^  \}", line):
            blocks.append((start, i))
            start = None
    return blocks


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else "/var/www/nginx/nginx.conf"
    with open(path, "r", encoding="utf-8") as f:
        src = f.read()

    out = src
    changed = []

    # --- 1. map no contexto http{} --------------------------------------
    if "map $http_upgrade $connection_upgrade" not in out:
        m = re.search(r"^  resolver\s+[^;]+;\s*$", out, re.M)
        if not m:
            sys.exit(
                "ERRO: diretiva 'resolver' nao encontrada no http{} "
                "(necessaria como ancora para o map)."
            )
        out = out[: m.end()] + MAP_BLOCK + out[m.end():]
        changed.append("map $http_upgrade")

    # --- 2. location nos server{} do hako --------------------------------
    lines = out.splitlines(keepends=True)
    inserts = []  # (line_index, text) — aplicados de tras para frente
    for start, end in split_http_level_blocks(lines):
        block = "".join(lines[start : end + 1])
        if f"server_name {HAKO_SERVER_NAME}" not in block:
            continue
        if "location ^~ /dashboard" in block:
            continue  # ja instalado neste server{}
        xfp = "https" if re.search(r"listen\s+443", block) else "$scheme"
        for i in range(start, end + 1):
            if re.match(r"^    location / \{", lines[i]):
                inserts.append((i, LOCATION_TEMPLATE.format(xfp=xfp)))
                break
        else:
            sys.exit(
                f"ERRO: server{{}} do hako (linhas {start + 1}-{end + 1}) "
                "sem 'location / {{' para ancorar a insercao."
            )

    for idx, text in sorted(inserts, reverse=True):
        lines.insert(idx, text + "\n")
        changed.append(f"location /dashboard (server linha {idx + 1})")

    if not changed:
        print("Nada a fazer: map e location /dashboard ja presentes.")
        return

    with open(path, "w", encoding="utf-8") as f:
        f.write("".join(lines))
    print("Inserido: " + ", ".join(changed))
    print("Proximo passo: docker exec nginx-proxy nginx -t && "
          "docker exec nginx-proxy nginx -s reload")


if __name__ == "__main__":
    main()

#!/usr/bin/env bash
set -euo pipefail

DOMAIN=""
IP=""
PORTS="3005"
REPORT_PATH="/tmp/oci-validate-report.txt"
REMOTE_DIR="/var/www/hako-website"
SERVICE_USER="opc"

while [ $# -gt 0 ]; do
  case "$1" in
    --domain) DOMAIN="$2"; shift 2;;
    --ip) IP="$2"; shift 2;;
    --ports) PORTS="$2"; shift 2;;
    --report-path) REPORT_PATH="$2"; shift 2;;
    --remote-dir) REMOTE_DIR="$2"; shift 2;;
    --service-user) SERVICE_USER="$2"; shift 2;;
    *) shift 1;;
  esac
done

rc=0
ts() { date +"%Y-%m-%d %H:%M:%S"; }
out() { printf "%s %s\n" "$(ts)" "$1" | tee -a "$REPORT_PATH"; }
hdr() { printf "\n%s === %s ===\n" "$(ts)" "$1" | tee -a "$REPORT_PATH"; }

> "$REPORT_PATH"

hdr "Sistema"
out "Hostname: $(hostname)"
out "Usuário: $(id -un)"
out "Kernel: $(uname -r)"

hdr "Nginx"
if command -v nginx >/dev/null 2>&1; then
  if nginx -t >/tmp/nginx_test.out 2>&1; then
    out "nginx -t: OK"
  else
    out "nginx -t: FALHA"
    cat /tmp/nginx_test.out >> "$REPORT_PATH" || true
    rc=16
  fi
  conf_path=""
  if [ -d /etc/nginx/conf.d ]; then conf_path="/etc/nginx/conf.d"; fi
  if [ -z "$conf_path" ] && [ -d /etc/nginx/sites-enabled ]; then conf_path="/etc/nginx/sites-enabled"; fi
  if [ -n "$conf_path" ]; then
    out "Conf-dir: $conf_path"
    find "$conf_path" -maxdepth 1 -type f -name "*.conf" -print0 | xargs -0 -r sed -n '1,120p' | tee -a "$REPORT_PATH" >/dev/null
  fi
else
  out "Nginx não encontrado"
  rc=${rc:-1}
fi

hdr "Portas"
ports_listen_ok=1
for p in $(echo "$PORTS" | tr ',' ' '); do
  if ss -tlnp 2>/dev/null | grep -q ":$p"; then
    out "LISTEN: $p OK"
  else
    out "LISTEN: $p FALHA"
    ports_listen_ok=0
    [ "$rc" -eq 0 ] && rc=10
  fi
done

hdr "Firewall"
ufw_state=""
if command -v ufw >/dev/null 2>&1; then
  ufw_state=$(ufw status | head -n1 | awk '{print $2}')
  out "ufw: $ufw_state"
  if [ "$ufw_state" = "active" ]; then
    ufw_rules=$(ufw status)
    echo "$ufw_rules" >> "$REPORT_PATH"
    for p in $(echo "$PORTS" | tr ',' ' '); do
      if echo "$ufw_rules" | grep -qE "\b$p(/tcp)?\b"; then
        out "UFW allow: $p OK"
      else
        out "UFW allow: $p AUSENTE"
        [ "$rc" -eq 0 ] && rc=11
      fi
    done
  fi
else
  out "ufw não instalado"
fi

hdr "Arquivos"
if sudo -n true 2>/dev/null; then sudo_cmd="sudo"; else sudo_cmd=""; fi
if $sudo_cmd -u www-data test -r "$REMOTE_DIR/index.html" 2>/dev/null; then
  out "www-data lê index.html: OK"
else
  out "www-data lê index.html: FALHA"
  [ "$rc" -eq 0 ] && rc=13
fi
own=$($sudo_cmd stat -c '%U:%G' "$REMOTE_DIR" 2>/dev/null || echo "?")
out "Owner dir: $own"

hdr "IP público"
if [ -z "$IP" ]; then
  IP=$(curl -s https://api.ipify.org || true)
fi
out "IP: ${IP:-desconhecido}"

hdr "Conectividade externa"
ext_ok=0
for p in $(echo "$PORTS" | tr ',' ' '); do
  if [ -n "$DOMAIN" ]; then
    code=$(curl --max-time 10 --connect-timeout 5 -s -o /dev/null -w "%{http_code}" "http://$DOMAIN:$p/" || true)
    out "HTTP $DOMAIN:$p -> $code"
    [ "$code" = "200" ] || [ "$code" = "301" ] || [ "$code" = "302" ] && ext_ok=1
  fi
  if [ -n "$IP" ]; then
    code=$(curl --max-time 10 --connect-timeout 5 -s -o /dev/null -w "%{http_code}" "http://$IP:$p/" || true)
    out "HTTP $IP:$p -> $code"
    [ "$code" = "200" ] || [ "$code" = "301" ] || [ "$code" = "302" ] && ext_ok=1
  fi
done
if [ "$ext_ok" -eq 0 ]; then
  [ "$rc" -eq 0 ] && rc=15
fi

hdr "OCI CLI"
if command -v oci >/dev/null 2>&1; then
  out "oci presente"
  if [ -f "$HOME/.oci/config" ]; then
    out "oci configurado"
  else
    out "oci sem config"
    [ "$rc" -eq 0 ] && rc=19
  fi
else
  out "oci ausente"
fi

hdr "Resumo"
out "Código: $rc"
exit "$rc"


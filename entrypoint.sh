#!/usr/bin/env bash

set -o errexit
set -o pipefail
set -o nounset

# Check if $DOMAIN is set
if [ -z "$DOMAIN" ]; then
  echo -e "You did not set \$DOMAIN variable at run time. No certificate will be registered.\n"
  echo -e "If you want to define it on command line here is an example:\n"
  echo -e "docker run -d -p 80:80 -p 443:443 -e DOMAIN=example.com\n"
else
  if [[ ! -f "/root/.acme.sh/${DOMAIN}/${DOMAIN}.cer" ]] || ! openssl x509 -checkend 0 -in "/root/.acme.sh/${DOMAIN}/${DOMAIN}.cer"; then
    # Generate SSL cert
    /root/.acme.sh/acme.sh --issue --standalone -d "${DOMAIN}" -d "www.${DOMAIN}"
    # Generate pfx
    openssl pkcs12 -export -out /webminerpool/certificate.pfx -inkey "/root/.acme.sh/${DOMAIN}/${DOMAIN}.key" -in "/root/.acme.sh/${DOMAIN}/${DOMAIN}.cer" -certfile "/root/.acme.sh/${DOMAIN}/fullchain.cer" -passin pass:miner -passout pass:miner
  fi
fi

# Start server
pushd /webminerpool
exec /usr/bin/mono server.exe

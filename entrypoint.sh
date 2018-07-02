#!/usr/bin/env bash

# Check if $DOMAIN is set
if [ -z $DOMAIN ]; then
	echo -e "You need to set \$DOMAIN variable at run time\n"
	echo -e "For example: docker run -d -p 80:80 -p 443:443 -e DOMAIN=example.com\n"
	exit 1
else
	# Install acme.sh
	apt-get -qq update
	apt-get install -qq \
		cron \
		openssl \
		curl \
		coreutils \
		socat \
		git
	git clone https://github.com/Neilpang/acme.sh.git /root/acme.sh && \
	cd /root/acme.sh && \
	git checkout 2.7.8 && \
	/root/acme.sh/acme.sh --install

	# Generate SSL cert
	/root/.acme.sh/acme.sh --issue --standalone -d ${DOMAIN} -d www.${DOMAIN}

	# Generate pfx
	openssl pkcs12 -export -out /webminerpool/certificate.pfx -inkey /root/.acme.sh/${DOMAIN}/${DOMAIN}.key -in /root/.acme.sh/${DOMAIN}/${DOMAIN}.cer -certfile /root/.acme.sh/${DOMAIN}/fullchain.cer -passin pass:miner -passout pass:miner

	# Start server
	pushd /webminerpool
	exec /usr/bin/mono server.exe 

fi

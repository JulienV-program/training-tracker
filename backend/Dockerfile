FROM php:8.3-fpm-alpine

ARG UID=1000
ARG GID=1000

# deps & extensions
RUN apk add --no-cache icu-libs libzip postgresql-libs \
    # deps de build pour compiler les ext
    && apk add --no-cache --virtual .build-deps $PHPIZE_DEPS icu-dev libzip-dev postgresql-dev \
    && docker-php-ext-configure intl \
    && docker-php-ext-install -j"$(nproc)" intl opcache pdo_pgsql zip \
    && apk del .build-deps
# composer
ENV COMPOSER_HOME=/tmp/composer-cache
COPY --from=composer:2 /usr/bin/composer /usr/bin/composer

# utilisateur non-root
RUN addgroup -g ${GID} -S app && adduser -u ${UID} -S -G app app
USER app

WORKDIR /var/www/html
<?php

use Symfony\Component\Dotenv\Dotenv;

require dirname(__DIR__).'/vendor/autoload.php';

if (method_exists(Dotenv::class, 'bootEnv')) {
    if (!isset($_SERVER['APP_ENV'])) {
        (new Dotenv())->bootEnv(dirname(__DIR__).'/.env');
    }

    if ($_SERVER['APP_ENV'] === 'test' && file_exists(dirname(__DIR__).'/.env.test')) {
        (new Dotenv())->bootEnv(dirname(__DIR__).'/.env.test');
    }
}

if (($_SERVER['APP_ENV'] ?? null) === 'test') {
    // reset sqlite
    @unlink(dirname(__DIR__).'/var/test.db');

    $exit = 0;
    system('php bin/console doctrine:migrations:migrate -n --env=test', $exit);
    if ($exit !== 0) {
        passthru('php bin/console doctrine:schema:create --env=test');
    }
}

if ($_SERVER['APP_DEBUG']) {
    umask(0000);
}

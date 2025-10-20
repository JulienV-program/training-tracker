SHELL := /bin/bash

up:        ## build & start
	docker compose up -d --build

down:      ## stop & nuke
	docker compose down -v

logs:
	docker compose logs -f --tail=200

api-sh:
	docker compose exec api sh

front-sh:
	docker compose exec front sh

migrate:
	docker compose exec api php bin/console doctrine:migrations:migrate -n

reseed:
	-docker compose exec api php bin/console doctrine:database:drop --force
	docker compose exec api php bin/console doctrine:database:create
	docker compose exec api php bin/console doctrine:migrations:migrate -n
	# docker compose exec api php bin/console doctrine:fixtures:load -n

worker:
	docker compose up worker

prune:
	docker system prune -af --volumes
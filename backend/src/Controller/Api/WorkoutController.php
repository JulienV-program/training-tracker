<?php

namespace App\Controller\Api;

use App\Entity\Sport;
use App\Entity\Workout;
use App\Repository\SportRepository;
use Doctrine\ORM\EntityManagerInterface;
use Symfony\Component\Routing\Attribute\Route;
use Symfony\Component\HttpFoundation\JsonResponse;
use Symfony\Bundle\FrameworkBundle\Controller\AbstractController;

#[Route('api/workouts')]
final class WorkoutController extends AbstractController
{
    public function __construct(
        private EntityManagerInterface $em,
        private SportRepository $sportRepository
        ) {}

    #[Route('', methods: ['GET'])]
    public function list(): JsonResponse {
        $items = $this->em->getRepository(Workout::class)->findBy([], ['date'=>'DESC'], 50);
        $data = array_map(fn(Workout $w) => [
            'id'=>$w->getId(),
            'date'=>$w->getDate()->format(DATE_ATOM),
            'sport'=>$w->getSport()->getName(),
            'name'=>$w->getName(),
            'duration'=>$w->getDuration(),
            'notes'=>$w->getNotes(),
        ], $items);
        return new JsonResponse($data);
    }

    #[Route('', methods: ['POST'])]
    public function create(): JsonResponse {
        $payload = json_decode(file_get_contents('php://input'), true) ?? [];
        $sport = $this->sportRepository->findOneByName($payload['sport']);

        if($payload == [] || $sport == null) {
            return new JsonResponse('bad payload', 300);
        }

        $w = (new Workout())
            ->setDate(new \DateTimeImmutable($payload['date'] ?? 'now'))
            ->setName($payload['name'])
            ->setSport(($sport))
            ->setDuration((int)($payload['duration'] ?? 60))
            ->setNotes($payload['notes'] ?? null);

        $this->em->persist($w);
        $this->em->flush();

        return new JsonResponse(['id'=>$w->getId()], 201);

    }
}

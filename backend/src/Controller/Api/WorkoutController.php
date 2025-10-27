<?php

namespace App\Controller\Api;

use App\Entity\Sport;
use App\Dto\WorkoutDto;
use App\Entity\Workout;
use App\Mapper\WorkoutMapper;
use App\Repository\SportRepository;
use Doctrine\ORM\EntityManagerInterface;
use Symfony\Component\HttpFoundation\Request;
use Symfony\Component\Routing\Attribute\Route;
use Symfony\Component\HttpFoundation\JsonResponse;
use Symfony\Component\Serializer\SerializerInterface;
use Symfony\Component\Validator\Validator\ValidatorInterface;
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
        $workouts = $this->em->getRepository(Workout::class)->findBy([], ['date'=>'DESC'], 50);
        $output = array_map(fn (Workout $w) => WorkoutMapper::toDto($w), $workouts);
        return new JsonResponse($output);
    }

 #[Route('', methods: ['POST'])]
    public function create(
        Request $request,
        SerializerInterface $serializer,
        ValidatorInterface $validator,
        EntityManagerInterface $em
    ): JsonResponse {
        /** @var WorkoutDto $dto */
        $dto = $serializer->deserialize($request->getContent(), WorkoutDto::class, 'json');

        $errors = $validator->validate($dto);
        if (count($errors) > 0) {
            return $this->json($errors, 400);
        }

        $workout = WorkoutMapper::fromDto($this->sportRepository, $dto);

        $em->persist($workout);
        $em->flush();

        return $this->json(WorkoutMapper::toDto($workout), 201);
    }

}

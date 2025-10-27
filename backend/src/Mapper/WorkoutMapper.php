<?php

namespace App\Mapper;

use App\Dto\WorkoutDto;
use App\Entity\Workout;
use App\Repository\SportRepository;

final class WorkoutMapper
{
    public static function fromDto(SportRepository $sportRepository, WorkoutDto $dto, ?Workout $workout = null): Workout
    {
        $workout ??= new Workout();

        $workout->setName($dto->name);
        $workout->setDuration($dto->duration);
        $workout->setDate($dto->date ?? new \DateTimeImmutable());
        $workout->setNotes($dto->notes);
        $workout->setSport($sportRepository->findOneByName($dto->sport));

        return $workout;
    }

    public static function toDto(Workout $workout): WorkoutDto
    {
        $dto = new WorkoutDto();
        $dto->id = $workout->getId();
        $dto->name = $workout->getName();
        $dto->date = $workout->getDate();
        $dto->duration = $workout->getDuration();
        $dto->notes = $workout->getNotes();
        $dto->sport = $workout->getSport()->getName();

        return $dto;
    }
}
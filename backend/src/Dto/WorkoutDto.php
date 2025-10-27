<?php

namespace App\Dto;

use App\Entity\Sport;
use Symfony\Component\Validator\Constraints as Assert;

final class WorkoutDto
{
    public int $id;
    
    public ?\DateTimeInterface $date = null;

    #[Assert\NotBlank]
    #[Assert\Length(max: 255)]
    public string $name;

    #[Assert\NotNull]
    #[Assert\Positive]
    public int $duration;

    public ?string $notes = null;

    public ?string $sport = null;
}
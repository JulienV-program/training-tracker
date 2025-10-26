<?php
namespace App\Tests;
use Symfony\Bundle\FrameworkBundle\Test\WebTestCase;

class WorkoutControllerTest extends WebTestCase
{
    public function testList(): void {
        $client = static::createClient();
        $client->request('GET', '/api/workouts');
        $this->assertResponseIsSuccessful();
        $this->assertResponseHeaderSame('content-type', 'application/json');
    }
}
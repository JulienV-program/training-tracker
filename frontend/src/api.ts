export type Workout = { id:number; date:string; name:string; sport:string; duration:number; notes?:string|null }

export async function fetchWorkouts(): Promise<Workout[]> {
  const r = await fetch('http://localhost:8080/api/workouts');
  if (!r.ok) throw new Error('API '+r.status);
  return r.json();
}

export async function createWorkout(w: Partial<Workout>) {
  const r = await fetch('http://localhost:8080/api/workouts', {
    method:'POST', headers:{'Content-Type':'application/json'},
    body: JSON.stringify(w),
  });
  if (!r.ok) throw new Error('API '+r.status);
  return r.json();
}
import { useEffect, useState } from "react";
import { fetchWorkouts, createWorkout } from "./api";
import type { Workout } from "./api";

export default function App() {
  const [list, setList] = useState<Workout[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => { fetchWorkouts().then(d=>{setList(d); setLoading(false);}).catch(()=>setLoading(false)); }, []);

  const add = async () => {
    await createWorkout({ sport:'Running', name:'Tempo', duration:45, date:new Date().toISOString() });
    setList(await fetchWorkouts());
  };

  return (
    <div style={{padding:16, fontFamily:'system-ui'}}>
      <h1>Workouts</h1>
      <button onClick={add}>+ Quick add</button>
      {loading ? <p>Loading…</p> :
        <ul>{list.map(w => (
          <li key={w.id}>{new Date(w.date.date).toLocaleString()} — {w.sport} -- {w.name} — {w.duration}′</li>
        ))}</ul>
      }
    </div>
  );
}
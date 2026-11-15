import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 50,
  duration: '2m',
};

export default function () {
  const payload = JSON.stringify({
    query: 'Find details about return policy',
    topK: 5
  });
  const headers = { 'Content-Type': 'application/json' };
  const res = http.post('http://localhost:8088/query', payload, { headers });
  check(res, {
    'status is 200': (r) => r.status === 200,
    'has matches': (r) => r.json('matches') && r.json('matches').length >= 0,
  });
  sleep(0.5);
}

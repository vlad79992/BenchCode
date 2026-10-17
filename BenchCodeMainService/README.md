# BenchCode Main Service

The service currently uses in-memory demonstration data. Groups, students,
competitions, tasks, and submissions reset when the service restarts.

## Student API

- `GET /groups` and `GET /users` list the sample groups and students.
- `GET /competitions` lists all competitions.
- `GET /competitions?userId=1` lists only competitions assigned to that student's group.
- `GET /competitions/{id}?userId=1` returns a competition if it is assigned to the student's group.
- `POST /tasks/{taskId}/submissions` submits code. Example:

```json
{
  "userId": 1,
  "code": "// MOCK_ACCEPTED\n",
  "language": "CSharp"
}
```

- `GET /users/{userId}/submissions` returns that student's submissions.

Submission responses include a status, message, and timestamp. The current
checker is only a mock: it accepts code containing `// MOCK_ACCEPTED` and
rejects other non-empty code. It does not compile or execute submitted code.
Use an isolated code-runner service before enabling real code evaluation.
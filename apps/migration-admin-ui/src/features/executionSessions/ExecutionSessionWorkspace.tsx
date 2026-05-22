import { useEffect, useState } from 'react';
import { queryOperationalEvents } from '../events/operationalEventTimelineApi';
import type { OperationalEventRecord } from '../events/operationalEventTimelineTypes';
import {
  fetchExecutionPhaseHistory,
  fetchExecutionPhases,
  transitionExecutionPhase,
} from './executionLifecycleApi';
import type { ExecutionPhaseHistoryRecord } from './executionLifecycleTypes';
import {
  fetchExecutionPlan,
  seedExecutionPlan,
} from './executionPlanApi';
import type { ExecutionPlanStepRecord } from './executionPlanTypes';
import {
  createExecutionSession,
  fetchRecentExecutionSessions,
  recordExecutionSessionSnapshot,
} from './executionSessionApi';
import type { ExecutionSessionRecord } from './executionSessionTypes';
import {
  expandExecutionPlanToWorkItems,
  fetchExecutionWorkItemQueueSummary,
  fetchRecentExecutionWorkItems,
  leaseExecutionWorkItems,
} from './executionWorkItemApi';
import type {
  ExecutionWorkItemQueueSummary,
  ExecutionWorkItemRecord,
} from './executionWorkItemTypes';

export function ExecutionSessionWorkspace() {
  const [sessions, setSessions] = useState<ExecutionSessionRecord[]>([]);
  const [selectedSession, setSelectedSession] = useState<ExecutionSessionRecord | null>(null);
  const [sessionEvents, setSessionEvents] = useState<OperationalEventRecord[]>([]);
  const [phaseHistory, setPhaseHistory] = useState<ExecutionPhaseHistoryRecord[]>([]);
  const [planSteps, setPlanSteps] = useState<ExecutionPlanStepRecord[]>([]);
  const [workItems, setWorkItems] = useState<ExecutionWorkItemRecord[]>([]);
  const [queueSummary, setQueueSummary] = useState<ExecutionWorkItemQueueSummary | null>(null);
  const [phases, setPhases] = useState<string[]>([]);
  const [selectedPhase, setSelectedPhase] = useState('validating');
  const [transitionReason, setTransitionReason] = useState('');
  const [workerId, setWorkerId] = useState('local-ui-worker');
  const [leaseTake, setLeaseTake] = useState(5);
  const [name, setName] = useState('');
  const [sourceConnector, setSourceConnector] = useState('');
  const [targetConnector, setTargetConnector] = useState('');
  const [notes, setNotes] = useState('');
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function loadSessions() {
    try {
      const response = await fetchRecentExecutionSessions(25);
      setSessions(response.sessions);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load execution sessions.');
    }
  }

  async function loadSessionDetails(session: ExecutionSessionRecord) {
    try {
      const [eventsResponse, historyResponse, planResponse, queueResponse, queueSummaryResponse] = await Promise.all([
        queryOperationalEvents({
          executionSessionId: session.executionSessionId,
          take: 25,
        }),
        fetchExecutionPhaseHistory(session.executionSessionId, 25),
        fetchExecutionPlan(session.executionSessionId),
        fetchRecentExecutionWorkItems(session.executionSessionId, 100),
        fetchExecutionWorkItemQueueSummary(session.executionSessionId),
      ]);

      setSelectedSession(session);
      setSessionEvents(eventsResponse.events);
      setPhaseHistory(historyResponse.history);
      setPlanSteps(planResponse.steps);
      setWorkItems(queueResponse.items);
      setQueueSummary(queueSummaryResponse);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load execution session details.');
    }
  }

  useEffect(() => {
    async function loadInitialState() {
      await loadSessions();

      try {
        const response = await fetchExecutionPhases();
        setPhases(response.phases);

        if (response.phases.length > 0) {
          setSelectedPhase(response.phases[0]);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load execution phases.');
      }
    }

    void loadInitialState();
  }, []);

  async function submitSession() {
    try {
      const session = await createExecutionSession({
        name: name || `Execution Session ${new Date().toLocaleString()}`,
        sourceConnector: sourceConnector || null,
        targetConnector: targetConnector || null,
        notes: notes || null,
      });

      setStatusMessage(`Created execution session ${session.executionSessionId}`);
      setName('');
      setSourceConnector('');
      setTargetConnector('');
      setNotes('');
      await loadSessions();
      await loadSessionDetails(session);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create execution session.');
    }
  }

  async function recordSnapshot(session: ExecutionSessionRecord) {
    try {
      await recordExecutionSessionSnapshot(session.executionSessionId, session.migrationRunId);
      setStatusMessage(`Recorded snapshot for ${session.executionSessionId}`);
      await loadSessionDetails(session);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to record correlated snapshot.');
    }
  }

  async function transitionSelectedSession() {
    if (!selectedSession) {
      return;
    }

    try {
      await transitionExecutionPhase({
        executionSessionId: selectedSession.executionSessionId,
        newPhase: selectedPhase,
        reason: transitionReason || null,
      });

      setStatusMessage(`Transitioned ${selectedSession.name} to ${selectedPhase}.`);
      setTransitionReason('');
      await loadSessions();
      await loadSessionDetails({ ...selectedSession, status: selectedPhase });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to transition execution phase.');
    }
  }

  async function seedSelectedPlan() {
    if (!selectedSession) {
      return;
    }

    try {
      const response = await seedExecutionPlan({
        executionSessionId: selectedSession.executionSessionId,
        sourceConnector: selectedSession.sourceConnector,
        targetConnector: selectedSession.targetConnector,
      });

      setPlanSteps(response.steps);
      setStatusMessage(`Seeded ${response.steps.length} execution plan step(s).`);
      await loadSessionDetails(selectedSession);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to seed execution plan.');
    }
  }

  async function expandSelectedPlanToQueue() {
    if (!selectedSession) {
      return;
    }

    try {
      const response = await expandExecutionPlanToWorkItems({
        executionSessionId: selectedSession.executionSessionId,
      });

      setWorkItems(response.items);
      setStatusMessage(`Expanded ${response.items.length} work item(s).`);
      await loadSessionDetails(selectedSession);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to expand execution plan to work items.');
    }
  }

  async function leaseSelectedWorkItems() {
    if (!selectedSession) {
      return;
    }

    try {
      const response = await leaseExecutionWorkItems({
        executionSessionId: selectedSession.executionSessionId,
        workerId,
        take: leaseTake,
        leaseSeconds: 300,
      });

      setStatusMessage(`Leased ${response.items.length} work item(s) to ${workerId}.`);
      await loadSessionDetails(selectedSession);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to lease execution work items.');
    }
  }

  return (
    <section className="workspace-card">
      <div className="workspace-card__header">
        <div>
          <p className="eyebrow">Execution</p>
          <h2>Migration execution sessions</h2>
        </div>
        <span className="status-pill">{sessions.length} recent</span>
      </div>

      {error ? <p className="error-text">{error}</p> : null}
      {statusMessage ? <p>{statusMessage}</p> : null}

      <div className="filter-row">
        <label>
          Name
          <input value={name} onChange={(event) => setName(event.target.value)} placeholder="April production dry run" />
        </label>
        <label>
          Source
          <input value={sourceConnector} onChange={(event) => setSourceConnector(event.target.value)} placeholder="AEM" />
        </label>
        <label>
          Target
          <input value={targetConnector} onChange={(event) => setTargetConnector(event.target.value)} placeholder="Bynder" />
        </label>
        <label>
          Notes
          <input value={notes} onChange={(event) => setNotes(event.target.value)} placeholder="Operator notes" />
        </label>
        <button type="button" onClick={submitSession}>Create session</button>
      </div>

      <div className="table-shell">
        <table>
          <thead>
            <tr>
              <th>Created</th>
              <th>Name</th>
              <th>Status</th>
              <th>Source</th>
              <th>Target</th>
              <th>Execution session ID</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {sessions.length === 0 ? (
              <tr>
                <td colSpan={7}>No execution sessions have been created yet.</td>
              </tr>
            ) : (
              sessions.map((session) => (
                <tr key={session.executionSessionId}>
                  <td>{new Date(session.createdUtc).toLocaleString()}</td>
                  <td>{session.name}</td>
                  <td>{session.status}</td>
                  <td>{session.sourceConnector ?? '—'}</td>
                  <td>{session.targetConnector ?? '—'}</td>
                  <td><code>{session.executionSessionId}</code></td>
                  <td>
                    <button type="button" onClick={() => loadSessionDetails(session)}>View</button>
                    <button type="button" onClick={() => recordSnapshot(session)}>Snapshot</button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {selectedSession ? (
        <>
          <div className="filter-row">
            <label>
              Next phase
              <select value={selectedPhase} onChange={(event) => setSelectedPhase(event.target.value)}>
                {phases.map((phase) => (
                  <option key={phase} value={phase}>{phase}</option>
                ))}
              </select>
            </label>
            <label>
              Reason
              <input value={transitionReason} onChange={(event) => setTransitionReason(event.target.value)} placeholder="Reason for transition" />
            </label>
            <button type="button" onClick={transitionSelectedSession}>Transition selected session</button>
            <button type="button" onClick={seedSelectedPlan}>Seed execution plan</button>
            <button type="button" onClick={expandSelectedPlanToQueue}>Expand work queue</button>
          </div>

          <div className="metric-grid">
            <article>
              <span>Total work</span>
              <strong>{queueSummary?.total ?? 0}</strong>
            </article>
            <article>
              <span>Pending</span>
              <strong>{queueSummary?.pending ?? 0}</strong>
            </article>
            <article>
              <span>Leased</span>
              <strong>{queueSummary?.leased ?? 0}</strong>
            </article>
            <article>
              <span>Completed</span>
              <strong>{queueSummary?.completed ?? 0}</strong>
            </article>
            <article>
              <span>Failed</span>
              <strong>{queueSummary?.failed ?? 0}</strong>
            </article>
            <article>
              <span>Dead-lettered</span>
              <strong>{queueSummary?.deadLettered ?? 0}</strong>
            </article>
          </div>

          <div className="filter-row">
            <label>
              Worker ID
              <input value={workerId} onChange={(event) => setWorkerId(event.target.value)} />
            </label>
            <label>
              Lease count
              <input type="number" min="1" max="100" value={leaseTake} onChange={(event) => setLeaseTake(Number(event.target.value))} />
            </label>
            <button type="button" onClick={leaseSelectedWorkItems}>Lease work</button>
          </div>

          <div className="table-shell">
            <h3>Execution work items for {selectedSession.name}</h3>
            <table>
              <thead>
                <tr>
                  <th>Priority</th>
                  <th>Type</th>
                  <th>Name</th>
                  <th>Status</th>
                  <th>Worker</th>
                  <th>Retry</th>
                  <th>Lease expires</th>
                </tr>
              </thead>
              <tbody>
                {workItems.length === 0 ? (
                  <tr>
                    <td colSpan={7}>No execution work items have been expanded yet.</td>
                  </tr>
                ) : (
                  workItems.map((item) => (
                    <tr key={item.executionWorkItemId}>
                      <td>{item.priority}</td>
                      <td>{item.workItemType}</td>
                      <td>{item.workItemName}</td>
                      <td>{item.status}</td>
                      <td>{item.workerId ?? '—'}</td>
                      <td>{item.retryCount}/{item.maxRetries}</td>
                      <td>{item.leaseExpiresUtc ? new Date(item.leaseExpiresUtc).toLocaleString() : '—'}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          <div className="table-shell">
            <h3>Execution plan for {selectedSession.name}</h3>
            <table>
              <thead>
                <tr>
                  <th>Order</th>
                  <th>Type</th>
                  <th>Name</th>
                  <th>Status</th>
                  <th>Source</th>
                  <th>Target</th>
                </tr>
              </thead>
              <tbody>
                {planSteps.length === 0 ? (
                  <tr>
                    <td colSpan={6}>No execution plan has been seeded yet.</td>
                  </tr>
                ) : (
                  planSteps.map((step) => (
                    <tr key={step.executionPlanStepId}>
                      <td>{step.stepOrder}</td>
                      <td>{step.stepType}</td>
                      <td>{step.stepName}</td>
                      <td>{step.status}</td>
                      <td>{step.sourceConnector ?? '—'}</td>
                      <td>{step.targetConnector ?? '—'}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          <div className="table-shell">
            <h3>Phase history for {selectedSession.name}</h3>
            <table>
              <thead>
                <tr>
                  <th>Created</th>
                  <th>Previous</th>
                  <th>New</th>
                  <th>Reason</th>
                </tr>
              </thead>
              <tbody>
                {phaseHistory.length === 0 ? (
                  <tr>
                    <td colSpan={4}>No phase transitions have been recorded yet.</td>
                  </tr>
                ) : (
                  phaseHistory.map((item) => (
                    <tr key={item.executionPhaseHistoryId}>
                      <td>{new Date(item.createdUtc).toLocaleString()}</td>
                      <td>{item.previousPhase ?? '—'}</td>
                      <td>{item.newPhase}</td>
                      <td>{item.reason ?? '—'}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          <div className="table-shell">
            <h3>Events for {selectedSession.name}</h3>
            <table>
              <thead>
                <tr>
                  <th>Created</th>
                  <th>Severity</th>
                  <th>Category</th>
                  <th>Event type</th>
                  <th>Message</th>
                </tr>
              </thead>
              <tbody>
                {sessionEvents.length === 0 ? (
                  <tr>
                    <td colSpan={5}>No events are correlated to this execution session yet.</td>
                  </tr>
                ) : (
                  sessionEvents.map((event) => (
                    <tr key={event.operationalEventId}>
                      <td>{new Date(event.createdUtc).toLocaleString()}</td>
                      <td>{event.severity}</td>
                      <td>{event.category}</td>
                      <td>{event.eventType}</td>
                      <td>{event.message}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </>
      ) : null}
    </section>
  );
}

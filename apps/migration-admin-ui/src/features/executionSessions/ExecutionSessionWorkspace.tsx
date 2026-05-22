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
  createExecutionSession,
  fetchRecentExecutionSessions,
  recordExecutionSessionSnapshot,
} from './executionSessionApi';
import type { ExecutionSessionRecord } from './executionSessionTypes';

export function ExecutionSessionWorkspace() {
  const [sessions, setSessions] = useState<ExecutionSessionRecord[]>([]);
  const [selectedSession, setSelectedSession] = useState<ExecutionSessionRecord | null>(null);
  const [sessionEvents, setSessionEvents] = useState<OperationalEventRecord[]>([]);
  const [phaseHistory, setPhaseHistory] = useState<ExecutionPhaseHistoryRecord[]>([]);
  const [phases, setPhases] = useState<string[]>([]);
  const [selectedPhase, setSelectedPhase] = useState('validating');
  const [transitionReason, setTransitionReason] = useState('');
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

  async function loadSessionEvents(session: ExecutionSessionRecord) {
    try {
      const [eventsResponse, historyResponse] = await Promise.all([
        queryOperationalEvents({
          executionSessionId: session.executionSessionId,
          take: 25,
        }),
        fetchExecutionPhaseHistory(session.executionSessionId, 25),
      ]);

      setSelectedSession(session);
      setSessionEvents(eventsResponse.events);
      setPhaseHistory(historyResponse.history);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load execution session events.');
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
      await loadSessionEvents(session);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create execution session.');
    }
  }

  async function recordSnapshot(session: ExecutionSessionRecord) {
    try {
      await recordExecutionSessionSnapshot(session.executionSessionId, session.migrationRunId);
      setStatusMessage(`Recorded snapshot for ${session.executionSessionId}`);
      await loadSessionEvents(session);
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
      await loadSessionEvents({ ...selectedSession, status: selectedPhase });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to transition execution phase.');
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
                    <button type="button" onClick={() => loadSessionEvents(session)}>View</button>
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

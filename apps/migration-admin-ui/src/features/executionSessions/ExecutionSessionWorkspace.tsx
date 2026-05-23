import { useEffect, useState } from 'react';
import { queryOperationalEvents } from '../events/operationalEventTimelineApi';
import type { OperationalEventRecord } from '../events/operationalEventTimelineTypes';
import {
  cancelExecutionSession,
  pauseExecutionSession,
  resumeExecutionSession,
} from './executionControlApi';
import { buildExecutionDiagnosticBundleUrl } from './executionDiagnosticExportApi';
import { analyzeExecutionReplayReadiness } from './executionReplayApi';
import { prepareExecutionReplayManifest } from './executionReplayPreparationApi';
import { evaluateExecutionReplayPolicy } from './executionReplayPolicyApi';
import type { ExecutionReplayPolicyEvaluationResult } from './executionReplayPolicyTypes';
import { approveExecutionReplay, fetchExecutionReplayApprovalHistory } from './executionReplayApprovalApi';
import type { ExecutionReplayApprovalRecord, ExecutionReplayApprovalResult } from './executionReplayApprovalTypes';
import { materializeExecutionReplay } from './executionReplayMaterializationApi';
import { fetchExecutionReplayLineage } from './executionReplayLineageApi';
import type { ExecutionReplayLineageResult } from './executionReplayLineageTypes';
import type { ExecutionReplayMaterializationResult } from './executionReplayMaterializationTypes';
import type { ExecutionReplayPreparationResult } from './executionReplayPreparationTypes';
import type { ExecutionReplayAnalysisResult } from './executionReplayTypes';
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
  renewExecutionWorkItemLease,
  requeueExecutionWorkItems,
} from './executionWorkItemApi';
import type {
  ExecutionWorkItemQueueSummary,
  ExecutionWorkItemRecord,
} from './executionWorkItemTypes';

const terminalStatuses = new Set(['cancelled', 'completed', 'failed']);

export function ExecutionSessionWorkspace() {
  const [sessions, setSessions] = useState<ExecutionSessionRecord[]>([]);
  const [selectedSession, setSelectedSession] = useState<ExecutionSessionRecord | null>(null);
  const [sessionEvents, setSessionEvents] = useState<OperationalEventRecord[]>([]);
  const [phaseHistory, setPhaseHistory] = useState<ExecutionPhaseHistoryRecord[]>([]);
  const [planSteps, setPlanSteps] = useState<ExecutionPlanStepRecord[]>([]);
  const [workItems, setWorkItems] = useState<ExecutionWorkItemRecord[]>([]);
  const [queueSummary, setQueueSummary] = useState<ExecutionWorkItemQueueSummary | null>(null);
  const [replayAnalysis, setReplayAnalysis] = useState<ExecutionReplayAnalysisResult | null>(null);
  const [replayPreparation, setReplayPreparation] = useState<ExecutionReplayPreparationResult | null>(null);
  const [replayPolicy, setReplayPolicy] = useState<ExecutionReplayPolicyEvaluationResult | null>(null);
  const [replayMaterialization, setReplayMaterialization] = useState<ExecutionReplayMaterializationResult | null>(null);
  const [replayApproval, setReplayApproval] = useState<ExecutionReplayApprovalResult | null>(null);
  const [replayApprovalHistory, setReplayApprovalHistory] = useState<ExecutionReplayApprovalRecord[]>([]);
  const [replayApprovedBy, setReplayApprovedBy] = useState('operator');
  const [replayApprovalMinutes, setReplayApprovalMinutes] = useState(60);
  const [replayLineage, setReplayLineage] = useState<ExecutionReplayLineageResult | null>(null);
  const [replayApprovalNote, setReplayApprovalNote] = useState('');
  const [replayScope, setReplayScope] = useState('failed-only');
  const [phases, setPhases] = useState<string[]>([]);
  const [selectedPhase, setSelectedPhase] = useState('validating');
  const [transitionReason, setTransitionReason] = useState('');
  const [controlReason, setControlReason] = useState('');
  const [workerId, setWorkerId] = useState('local-ui-worker');
  const [leaseTake, setLeaseTake] = useState(5);
  const [leaseSeconds, setLeaseSeconds] = useState(300);
  const [includeFailed, setIncludeFailed] = useState(true);
  const [includeExpiredLeases, setIncludeExpiredLeases] = useState(true);
  const [name, setName] = useState('');
  const [sourceConnector, setSourceConnector] = useState('');
  const [targetConnector, setTargetConnector] = useState('');
  const [notes, setNotes] = useState('');
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const selectedIsTerminal = selectedSession ? terminalStatuses.has(selectedSession.status) : false;
  const selectedCanLease = selectedSession ? selectedSession.status !== 'paused' && !selectedIsTerminal : false;

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
      const [eventsResponse, historyResponse, planResponse, queueResponse, queueSummaryResponse, lineageResponse] = await Promise.all([
        queryOperationalEvents({
          executionSessionId: session.executionSessionId,
          take: 25,
        }),
        fetchExecutionPhaseHistory(session.executionSessionId, 25),
        fetchExecutionPlan(session.executionSessionId),
        fetchRecentExecutionWorkItems(session.executionSessionId, 100),
        fetchExecutionWorkItemQueueSummary(session.executionSessionId),
        fetchExecutionReplayLineage(session.executionSessionId),
      ]);

      setSelectedSession(session);
      setSessionEvents(eventsResponse.events);
      setPhaseHistory(historyResponse.history);
      setPlanSteps(planResponse.steps);
      setWorkItems(queueResponse.items);
      setQueueSummary(queueSummaryResponse);
      setReplayLineage(lineageResponse);
      const approvalHistoryResponse = await fetchExecutionReplayApprovalHistory(session.executionSessionId, 25);
      setReplayApprovalHistory(approvalHistoryResponse.approvals);
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

  function exportSelectedDiagnostics() {
    if (!selectedSession) {
      return;
    }

    window.open(buildExecutionDiagnosticBundleUrl(selectedSession.executionSessionId), '_blank', 'noopener,noreferrer');
  }

    async function analyzeSelectedReplayReadiness() {
    if (!selectedSession) {
      return;
    }

    try {
      const result = await analyzeExecutionReplayReadiness(selectedSession.executionSessionId);
      setReplayAnalysis(result);
      setStatusMessage(`Replay analysis complete. Risk score: ${result.riskScore}.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to analyze replay readiness.');
    }
  }
      async function approveSelectedReplay() {
    if (!selectedSession) {
      return;
    }

    try {
      const result = await approveExecutionReplay({
        sourceExecutionSessionId: selectedSession.executionSessionId,
        scope: replayScope,
        approvedBy: replayApprovedBy,
        approvalNote: replayApprovalNote,
        expiresInMinutes: replayApprovalMinutes,
      });

      setReplayApproval(result);
      setStatusMessage(`Replay approved until ${new Date(result.approval.expiresUtc).toLocaleString()}.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to approve replay.');
    }
  }
async function materializeSelectedReplay() {
    if (!selectedSession) {
      return;
    }

    try {
      const result = await materializeExecutionReplay({
        sourceExecutionSessionId: selectedSession.executionSessionId,
        scope: replayScope,
        approvalNote: replayApprovalNote,
      });

      setReplayMaterialization(result);
      setReplayApprovalNote('');
      setStatusMessage(`Replay session materialized: ${result.replayExecutionSessionId}`);
      await loadSessions();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to materialize replay session.');
    }
  }
  async function evaluateSelectedReplayPolicy() {
    if (!selectedSession) {
      return;
    }

    try {
      const result = await evaluateExecutionReplayPolicy(selectedSession.executionSessionId, replayScope);
      setReplayPolicy(result);
      setStatusMessage(`Replay policy decision: ${result.decision}.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to evaluate replay policy.');
    }
  }
async function prepareSelectedReplayManifest() {
    if (!selectedSession) {
      return;
    }

    try {
      const result = await prepareExecutionReplayManifest({
        executionSessionId: selectedSession.executionSessionId,
        scope: replayScope,
        reason: controlReason || null,
      });

      setReplayPreparation(result);
      setStatusMessage(`Replay manifest prepared with ${result.items.length} item(s).`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to prepare replay manifest.');
    }
  }
async function pauseSelectedSession() {
    if (!selectedSession) {
      return;
    }

    try {
      await pauseExecutionSession(selectedSession.executionSessionId, controlReason || undefined);
      const updatedSession = { ...selectedSession, status: 'paused' };
      setControlReason('');
      setStatusMessage(`Paused ${selectedSession.name}.`);
      await loadSessions();
      await loadSessionDetails(updatedSession);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to pause execution session.');
    }
  }

  async function resumeSelectedSession() {
    if (!selectedSession) {
      return;
    }

    try {
      await resumeExecutionSession(selectedSession.executionSessionId, controlReason || undefined);
      const updatedSession = { ...selectedSession, status: 'queued' };
      setControlReason('');
      setStatusMessage(`Resumed ${selectedSession.name}.`);
      await loadSessions();
      await loadSessionDetails(updatedSession);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to resume execution session.');
    }
  }

  async function cancelSelectedSession() {
    if (!selectedSession) {
      return;
    }

    try {
      await cancelExecutionSession(selectedSession.executionSessionId, controlReason || undefined);
      const updatedSession = { ...selectedSession, status: 'cancelled' };
      setControlReason('');
      setStatusMessage(`Cancelled ${selectedSession.name}.`);
      await loadSessions();
      await loadSessionDetails(updatedSession);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to cancel execution session.');
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
        leaseSeconds,
      });

      setStatusMessage(`Leased ${response.items.length} work item(s) to ${workerId}.`);
      await loadSessionDetails(selectedSession);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to lease execution work items.');
    }
  }

  async function requeueSelectedWorkItems() {
    if (!selectedSession) {
      return;
    }

    try {
      const response = await requeueExecutionWorkItems({
        executionSessionId: selectedSession.executionSessionId,
        includeFailed,
        includeExpiredLeases,
      });

      setStatusMessage(`Requeued ${response.requeued} work item(s).`);
      await loadSessionDetails(selectedSession);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to requeue execution work items.');
    }
  }

  async function renewWorkItem(item: ExecutionWorkItemRecord) {
    if (!selectedSession || !item.leaseId || !item.workerId) {
      return;
    }

    try {
      await renewExecutionWorkItemLease({
        executionWorkItemId: item.executionWorkItemId,
        leaseId: item.leaseId,
        workerId: item.workerId,
        leaseSeconds,
      });

      setStatusMessage(`Renewed lease for ${item.workItemName}.`);
      await loadSessionDetails(selectedSession);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to renew work-item lease.');
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
                  <td>{session.sourceConnector ?? 'â€”'}</td>
                  <td>{session.targetConnector ?? 'â€”'}</td>
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
              Control reason
              <input value={controlReason} onChange={(event) => setControlReason(event.target.value)} placeholder="Pause/resume/cancel reason" />
            </label>
            <button type="button" onClick={pauseSelectedSession} disabled={selectedSession.status === 'paused' || selectedIsTerminal}>Pause session</button>
            <button type="button" onClick={resumeSelectedSession} disabled={selectedSession.status !== 'paused'}>Resume session</button>
            <button type="button" onClick={cancelSelectedSession} disabled={selectedIsTerminal}>Cancel session</button>
            <button type="button" onClick={exportSelectedDiagnostics}>Export diagnostics</button>`r`n            <button type="button" onClick={analyzeSelectedReplayReadiness}>Analyze replay</button>
            <label>
              Replay scope
              <select value={replayScope} onChange={(event) => setReplayScope(event.target.value)}>
                <option value="failed-only">failed-only</option>
                <option value="dead-letter-only">dead-letter-only</option>
                <option value="incomplete-only">incomplete-only</option>
                <option value="all">all</option>
              </select>
            </label>
            <button type="button" onClick={evaluateSelectedReplayPolicy}>Evaluate policy</button>
            <button type="button" onClick={prepareSelectedReplayManifest}>Prepare replay manifest</button>
            <label>Replay approval<input value={replayApprovalNote} onChange={(event) => setReplayApprovalNote(event.target.value)} placeholder="Required approval note" /></label>
            <label>Approved by<input value={replayApprovedBy} onChange={(event) => setReplayApprovedBy(event.target.value)} placeholder="Operator" /></label>
            <label>Approval minutes<input type="number" min="5" max="1440" value={replayApprovalMinutes} onChange={(event) => setReplayApprovalMinutes(Number(event.target.value))} /></label>
            <button type="button" onClick={approveSelectedReplay} disabled={!replayApprovalNote.trim() || !replayApprovedBy.trim()}>Approve replay</button>
            <button type="button" onClick={materializeSelectedReplay} disabled={!replayApprovalNote.trim()}>Materialize replay</button>
            <span className="status-pill">Selected: {selectedSession.status}</span>
          </div>

                                                  {replayLineage ? (
            <div className="table-shell">
              <h3>Replay lineage</h3>
              <div className="metric-grid">
                <article><span>Root session</span><strong>{replayLineage.rootExecutionSessionId}</strong></article>
                <article><span>Source session</span><strong>{replayLineage.sourceExecutionSessionId ?? 'â€”'}</strong></article>
                <article><span>Replay depth</span><strong>{replayLineage.replayDepth}</strong></article>
                <article><span>Children</span><strong>{replayLineage.children.length}</strong></article>
              </div>
              <table>
                <thead><tr><th>Type</th><th>Name</th><th>Status</th><th>Scope</th><th>Session</th></tr></thead>
                <tbody>
                  {replayLineage.ancestors.map((node) => (
                    <tr key={`ancestor-${node.executionSessionId}`}>
                      <td>ancestor</td>
                      <td>{node.name}</td>
                      <td>{node.status}</td>
                      <td>{node.replayScope ?? 'â€”'}</td>
                      <td><code>{node.executionSessionId}</code></td>
                    </tr>
                  ))}
                  {replayLineage.children.map((node) => (
                    <tr key={`child-${node.executionSessionId}`}>
                      <td>child</td>
                      <td>{node.name}</td>
                      <td>{node.status}</td>
                      <td>{node.replayScope ?? 'â€”'}</td>
                      <td><code>{node.executionSessionId}</code></td>
                    </tr>
                  ))}
                  {replayLineage.ancestors.length === 0 && replayLineage.children.length === 0 ? (
                    <tr><td colSpan={5}>No replay ancestors or children found.</td></tr>
                  ) : null}
                </tbody>
              </table>
            </div>
          ) : null}
                    <div className="table-shell">
            <h3>Replay approval audit trail</h3>
            <table>
              <thead><tr><th>Created</th><th>Status</th><th>Scope</th><th>Approved by</th><th>Expires</th><th>Replay session</th></tr></thead>
              <tbody>
                {replayApprovalHistory.length === 0 ? (
                  <tr><td colSpan={6}>No replay approvals have been recorded for this session.</td></tr>
                ) : (
                  replayApprovalHistory.map((approval) => (
                    <tr key={approval.replayApprovalId}>
                      <td>{new Date(approval.createdUtc).toLocaleString()}</td>
                      <td>{approval.status}</td>
                      <td>{approval.scope}</td>
                      <td>{approval.approvedBy}</td>
                      <td>{new Date(approval.expiresUtc).toLocaleString()}</td>
                      <td>{approval.replayExecutionSessionId ? <code>{approval.replayExecutionSessionId}</code> : 'â€”'}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
{replayApproval ? (
            <div className="table-shell">
              <h3>Replay approval</h3>
              <div className="metric-grid">
                <article><span>Approval</span><strong>{replayApproval.approval.replayApprovalId}</strong></article>
                <article><span>Status</span><strong>{replayApproval.approval.status}</strong></article>
                <article><span>Approved by</span><strong>{replayApproval.approval.approvedBy}</strong></article>
                <article><span>Expires</span><strong>{new Date(replayApproval.approval.expiresUtc).toLocaleString()}</strong></article>
              </div>
            </div>
          ) : null}
{replayMaterialization ? (
            <div className="table-shell">
              <h3>Replay materialized</h3>
              <div className="metric-grid">
                <article><span>Replay session</span><strong>{replayMaterialization.replayExecutionSessionId}</strong></article>
                <article><span>Scope</span><strong>{replayMaterialization.scope}</strong></article>
                <article><span>Depth</span><strong>{replayMaterialization.replayDepth}</strong></article>
                <article><span>Work items</span><strong>{replayMaterialization.workItemCount}</strong></article>
              </div>
            </div>
          ) : null}
          {replayPolicy ? (
            <div className="table-shell">
              <h3>Replay policy</h3>
              <div className="metric-grid">
                <article><span>Decision</span><strong>{replayPolicy.decision}</strong></article>
                <article><span>Policy score</span><strong>{replayPolicy.policyScore}</strong></article>
                <article><span>Prepared items</span><strong>{replayPolicy.metrics.preparedItemCount}</strong></article>
                <article><span>Dead-letter %</span><strong>{replayPolicy.metrics.deadLetteredPercent}</strong></article>
              </div>
              <table>
                <thead><tr><th>Severity</th><th>Code</th><th>Message</th></tr></thead>
                <tbody>
                  {replayPolicy.violations.length === 0 ? (
                    <tr><td colSpan={3}>No replay policy violations.</td></tr>
                  ) : (
                    replayPolicy.violations.map((violation) => (
                      <tr key={`${violation.severity}-${violation.code}`}>
                        <td>{violation.severity}</td>
                        <td><code>{violation.code}</code></td>
                        <td>{violation.message}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          ) : null}
{replayPreparation ? (
            <div className="table-shell">
              <h3>Replay preparation manifest</h3>
              <div className="metric-grid">
                <article><span>Scope</span><strong>{replayPreparation.scope}</strong></article>
                <article><span>Can prepare</span><strong>{replayPreparation.canPrepareReplay ? 'Yes' : 'No'}</strong></article>
                <article><span>Approval</span><strong>{replayPreparation.requiresApproval ? 'Required' : 'Not required'}</strong></article>
                <article><span>Items</span><strong>{replayPreparation.items.length}</strong></article>
              </div>
              <p>{replayPreparation.recommendation}</p>
              <table>
                <thead><tr><th>Order</th><th>Type</th><th>Name</th><th>Source status</th></tr></thead>
                <tbody>
                  {replayPreparation.items.length === 0 ? (
                    <tr><td colSpan={4}>No replay items matched the selected scope.</td></tr>
                  ) : (
                    replayPreparation.items.map((item) => (
                      <tr key={`${item.replayOrder}-${item.sourceExecutionWorkItemId ?? item.replayName}`}>
                        <td>{item.replayOrder}</td>
                        <td>{item.replayType}</td>
                        <td>{item.replayName}</td>
                        <td>{item.sourceStatus}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          ) : null}
{replayAnalysis ? (
            <div className="table-shell">
              <h3>Replay readiness</h3>
              <div className="metric-grid">
                <article><span>Risk score</span><strong>{replayAnalysis.riskScore}</strong></article>
                <article><span>Recommended</span><strong>{replayAnalysis.replayRecommended ? 'Yes' : 'No'}</strong></article>
                <article><span>Findings</span><strong>{replayAnalysis.findings.length}</strong></article>
                <article><span>Events</span><strong>{replayAnalysis.stateSummary.operationalEventCount}</strong></article>
              </div>
              <p>{replayAnalysis.recommendation}</p>
              <table>
                <thead><tr><th>Severity</th><th>Code</th><th>Finding</th></tr></thead>
                <tbody>
                  {replayAnalysis.findings.map((finding) => (
                    <tr key={`${finding.severity}-${finding.code}-${finding.message}`}>
                      <td>{finding.severity}</td>
                      <td><code>{finding.code}</code></td>
                      <td>{finding.message}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}
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
            <button type="button" onClick={transitionSelectedSession} disabled={selectedIsTerminal}>Transition selected session</button>
            <button type="button" onClick={seedSelectedPlan} disabled={selectedIsTerminal}>Seed execution plan</button>
            <button type="button" onClick={expandSelectedPlanToQueue} disabled={selectedIsTerminal}>Expand work queue</button>
          </div>

          <div className="metric-grid">
            <article><span>Total work</span><strong>{queueSummary?.total ?? 0}</strong></article>
            <article><span>Pending</span><strong>{queueSummary?.pending ?? 0}</strong></article>
            <article><span>Leased</span><strong>{queueSummary?.leased ?? 0}</strong></article>
            <article><span>Completed</span><strong>{queueSummary?.completed ?? 0}</strong></article>
            <article><span>Failed</span><strong>{queueSummary?.failed ?? 0}</strong></article>
            <article><span>Dead-lettered</span><strong>{queueSummary?.deadLettered ?? 0}</strong></article>
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
            <label>
              Lease seconds
              <input type="number" min="30" max="3600" value={leaseSeconds} onChange={(event) => setLeaseSeconds(Number(event.target.value))} />
            </label>
            <button type="button" onClick={leaseSelectedWorkItems} disabled={!selectedCanLease}>Lease work</button>
          </div>

          <div className="filter-row">
            <label>
              <input type="checkbox" checked={includeFailed} onChange={(event) => setIncludeFailed(event.target.checked)} />
              Include failed
            </label>
            <label>
              <input type="checkbox" checked={includeExpiredLeases} onChange={(event) => setIncludeExpiredLeases(event.target.checked)} />
              Include expired leases
            </label>
            <button type="button" onClick={requeueSelectedWorkItems} disabled={selectedIsTerminal}>Requeue recoverable work</button>
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
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {workItems.length === 0 ? (
                  <tr>
                    <td colSpan={8}>No execution work items have been expanded yet.</td>
                  </tr>
                ) : (
                  workItems.map((item) => (
                    <tr key={item.executionWorkItemId}>
                      <td>{item.priority}</td>
                      <td>{item.workItemType}</td>
                      <td>{item.workItemName}</td>
                      <td>{item.status}</td>
                      <td>{item.workerId ?? 'â€”'}</td>
                      <td>{item.retryCount}/{item.maxRetries}</td>
                      <td>{item.leaseExpiresUtc ? new Date(item.leaseExpiresUtc).toLocaleString() : 'â€”'}</td>
                      <td>
                        <button
                          type="button"
                          onClick={() => renewWorkItem(item)}
                          disabled={!item.leaseId || !item.workerId || item.status !== 'leased' || selectedIsTerminal}
                        >
                          Renew lease
                        </button>
                      </td>
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








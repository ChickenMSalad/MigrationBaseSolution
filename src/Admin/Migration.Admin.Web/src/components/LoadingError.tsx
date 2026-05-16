type LoadingErrorProps = {
  loading?: boolean;
  error?: string | null;
  message?: string | null;
  onRetry?: () => void;
};

export function LoadingError({ loading, error, message, onRetry }: LoadingErrorProps) {
  if (loading) {
    return <div className="notice">Loading…</div>;
  }

  const text = error ?? message;

  if (!text) {
    return null;
  }

  return (
    <div className="notice error">
      <span>{text}</span>
      {onRetry && (
        <button type="button" onClick={onRetry}>
          Retry
        </button>
      )}
    </div>
  );
}

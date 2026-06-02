type LoadingErrorProps = {
  loading?: boolean;
  title?: string;
  error?: string | null;
  message?: string | null;
  onRetry?: () => void;
};

export function LoadingError({ loading, title, error, message, onRetry }: LoadingErrorProps) {
  if (loading) {
    return <p className="muted">Loadingâ€¦</p>;
  }

  const text = error ?? message;

  if (!text) {
    return null;
  }

  return (
    <div className="error-state">
      {title && <h3>{title}</h3>}
      <p>{text}</p>
      {onRetry && (
        <button type="button" onClick={onRetry}>
          Retry
        </button>
      )}
    </div>
  );
}

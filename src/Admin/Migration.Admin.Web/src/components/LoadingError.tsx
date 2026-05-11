export function LoadingError({ loading, error }: { loading?: boolean; error?: string | null }) {
  if (loading) return <div className="notice">Loading…</div>;
  if (error) return <div className="notice error">{error}</div>;
  return null;
}

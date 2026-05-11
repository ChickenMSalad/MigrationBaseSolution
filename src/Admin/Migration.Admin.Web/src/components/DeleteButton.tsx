type DeleteButtonProps = {
  label?: string;
  confirmText: string;
  onDelete: () => Promise<void>;
};

export function DeleteButton({ label = "Delete", confirmText, onDelete }: DeleteButtonProps) {
  async function handleClick() {
    if (!confirm(confirmText)) return;
    await onDelete();
  }

  return (
    <button className="danger" type="button" onClick={handleClick}>
      {label}
    </button>
  );
}

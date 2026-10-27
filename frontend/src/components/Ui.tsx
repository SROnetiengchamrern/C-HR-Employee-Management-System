import type { ReactNode } from "react";

export function Modal({ title, onClose, children, footer }: { title: string; onClose: () => void; children: ReactNode; footer?: ReactNode }) {
  return (
    <div className="modal-backdrop-custom" onMouseDown={onClose}>
      <div className="modal-card" role="dialog" aria-modal="true" aria-label={title} onMouseDown={(event) => event.stopPropagation()}>
        <header>
          <h2>{title}</h2>
          <button className="icon-btn" onClick={onClose} aria-label="Close">×</button>
        </header>
        <div className="modal-body">{children}</div>
        {footer && <footer>{footer}</footer>}
      </div>
    </div>
  );
}

export function ErrorNote({ message }: { message: string }) {
  if (!message) return null;
  return <div className="alert-error">{message}</div>;
}

export function StatusBadge({ status }: { status: string }) {
  const tone = status === "Active" || status === "Approved" || status === "Completed"
    ? "ok"
    : status === "Probation" || status === "Pending" || status === "Draft"
      ? "wait"
      : status === "Rejected" || status === "Terminated" || status === "Resigned" || status === "Cancelled"
        ? "bad"
        : "info";
  return <span className={`badge-status ${tone}`}>{status === "OnLeave" ? "On leave" : status}</span>;
}

import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import { ErrorNote } from "../components/Ui";
import { formatWhen } from "../lib/format";
import type { NotificationItem } from "../types";

export function NotificationsPage() {
  const [items, setItems] = useState<NotificationItem[]>([]);
  const [error, setError] = useState("");

  async function load() {
    setItems(await api<NotificationItem[]>("/api/notifications"));
  }

  useEffect(() => {
    load().catch((caught: Error) => setError(caught.message));
  }, []);

  async function markRead(id: number) {
    await api(`/api/notifications/${id}/read`, { method: "POST" });
    await load();
  }

  return (
    <>
      <div className="crumb"><Link to="/">Home</Link> <span>›</span> Notifications</div>
      <ErrorNote message={error} />
      <div className="panel">
        {items.map((item) => (
          <div className="leave-mini" key={item.id}>
            <div>
              <strong>{item.title}</strong>
              <div>{item.message}</div>
              <div className="muted">{formatWhen(item.createdAt)}</div>
            </div>
            {item.isRead ? <span className="muted">Read</span> : <button className="btn btn-sm btn-outline-primary" onClick={() => markRead(item.id).catch((caught: Error) => setError(caught.message))}>Mark read</button>}
          </div>
        ))}
        {items.length === 0 && <div className="empty">No notifications.</div>}
      </div>
    </>
  );
}

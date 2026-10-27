import { FormEvent, useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import { useAuth } from "../auth";
import { Column, DataGrid } from "../components/DataGrid";
import { ErrorNote, StatusBadge } from "../components/Ui";
import { leaveDays, toQuery } from "../lib/format";
import type { LeaveRequest, LeaveStatus, LeaveType } from "../types";

export function LeavePage() {
  const { canApprove, isHr } = useAuth();
  const [types, setTypes] = useState<LeaveType[]>([]);
  const [rows, setRows] = useState<LeaveRequest[]>([]);
  const [status, setStatus] = useState("");
  const [error, setError] = useState("");
  const [comment, setComment] = useState("");
  const [form, setForm] = useState({ leaveTypeId: "", startDate: "", endDate: "", reason: "" });
  const commentRef = useRef(comment);
  const reviewRef = useRef<(id: number, action: "approve" | "reject" | "cancel") => void>(() => undefined);
  commentRef.current = comment;

  async function load(nextStatus = status) {
    setRows(await api<LeaveRequest[]>(`/api/leave?${toQuery({ status: nextStatus })}`));
  }

  useEffect(() => {
    api<LeaveType[]>("/api/leave/types").then((items) => {
      setTypes(items);
      setForm((current) => ({ ...current, leaveTypeId: String(items[0]?.id ?? "") }));
    }).catch((caught: Error) => setError(caught.message));
    load().catch((caught: Error) => setError(caught.message));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError("");
    try {
      await api("/api/leave", {
        method: "POST",
        body: JSON.stringify({ ...form, leaveTypeId: Number(form.leaveTypeId) })
      });
      setForm({ ...form, startDate: "", endDate: "", reason: "" });
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not submit leave.");
    }
  }

  async function review(id: number, action: "approve" | "reject" | "cancel") {
    setError("");
    try {
      await api(`/api/leave/${id}/${action}`, { method: "POST", body: JSON.stringify({ comment: commentRef.current }) });
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not update the request.");
    }
  }

  reviewRef.current = review;

  const days = leaveDays(form.startDate, form.endDate);

  return (
    <>
      <div className="crumb"><Link to="/">Home</Link> <span>›</span> Leave Management</div>
      <ErrorNote message={error} />
      <div className="row g-3">
        <div className="col-lg-4">
          <form className="panel" onSubmit={submit}>
            <h2 className="mb-3">Apply for leave</h2>
            <label className="form-label">Type</label>
            <select className="form-select mb-3" value={form.leaveTypeId} onChange={(event) => setForm({ ...form, leaveTypeId: event.target.value })}>
              {types.map((type) => <option key={type.id} value={type.id}>{type.name} · {type.defaultDays} days</option>)}
            </select>
            <label className="form-label">Start</label>
            <input className="form-control mb-3" type="date" required value={form.startDate} onChange={(event) => setForm({ ...form, startDate: event.target.value })} />
            <label className="form-label">End</label>
            <input className="form-control mb-3" type="date" required value={form.endDate} onChange={(event) => setForm({ ...form, endDate: event.target.value })} />
            <label className="form-label">Reason</label>
            <textarea className="form-control mb-3" required value={form.reason} onChange={(event) => setForm({ ...form, reason: event.target.value })} />
            <p className="muted">{days > 0 ? `${days} day${days === 1 ? "" : "s"}` : "Choose a valid date range."}</p>
            <button className="btn btn-primary">Submit request</button>
          </form>
        </div>
        <div className="col-lg-8">
          <div className="page-head">
            <h2>Leave history</h2>
            <select className="form-select w-auto" value={status} onChange={(event) => { setStatus(event.target.value); load(event.target.value).catch((caught: Error) => setError(caught.message)); }}>
              <option value="">All statuses</option>
              {(["Pending", "Approved", "Rejected", "Cancelled"] as LeaveStatus[]).map((item) => <option key={item}>{item}</option>)}
            </select>
          </div>
          {(canApprove || !isHr) && (
            <input className="form-control mb-2" placeholder="Comment for approve or reject" value={comment} onChange={(event) => setComment(event.target.value)} />
          )}
          <DataGrid data={rows} empty="No leave requests.">
            <Column title="Employee" data={(row: LeaveRequest) => `${row.employeeName} ${row.department}`}>
              {(_value: string, _type: string, row: LeaveRequest) => <>{row.employeeName}<div className="muted">{row.department}</div></>}
            </Column>
            <Column title="Type" data={(row: LeaveRequest) => `${row.leaveType} ${row.reason}`}>
              {(_value: string, _type: string, row: LeaveRequest) => <>{row.leaveType}<div className="muted">{row.reason}</div></>}
            </Column>
            <Column title="Dates" data="startDate">{(_value: string, _type: string, row: LeaveRequest) => `${row.startDate} → ${row.endDate}`}</Column>
            <Column title="Days" data="days" />
            <Column title="Status" data="status">
              {(value: string, _type: string, row: LeaveRequest) => (
                <>
                  <StatusBadge status={value} />
                  {row.reviewComment && <div className="muted">{row.reviewComment}</div>}
                </>
              )}
            </Column>
            <Column title="" options={{ orderable: false, searchable: false }}>
              {(_value: unknown, type: string, row: LeaveRequest) => type !== "display" ? "" : (
                <span className="text-nowrap">
                  {row.status === "Pending" && canApprove && (
                    <>
                      <button className="btn btn-sm btn-success me-1" onClick={() => reviewRef.current(row.id, "approve")}>Approve</button>
                      <button className="btn btn-sm btn-outline-danger me-1" onClick={() => reviewRef.current(row.id, "reject")}>Reject</button>
                    </>
                  )}
                  {row.status === "Pending" && <button className="btn btn-sm btn-outline-secondary" onClick={() => reviewRef.current(row.id, "cancel")}>Cancel</button>}
                </span>
              )}
            </Column>
          </DataGrid>
        </div>
      </div>
    </>
  );
}

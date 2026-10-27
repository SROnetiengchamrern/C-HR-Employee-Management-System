import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { api, downloadCsv } from "../api";
import { Column, DataGrid } from "../components/DataGrid";
import { ErrorNote, StatusBadge } from "../components/Ui";
import { monthStartIso, todayIso, toQuery } from "../lib/format";
import type { AttendanceReport, HeadcountReport, LeaveReport, LeaveRequest } from "../types";

type Tab = "headcount" | "attendance" | "leave";

export function ReportsPage() {
  const [params, setParams] = useSearchParams();
  const tab = (params.get("tab") as Tab) || "headcount";
  const [from, setFrom] = useState(monthStartIso());
  const [to, setTo] = useState(todayIso());
  const [headcount, setHeadcount] = useState<HeadcountReport | null>(null);
  const [attendance, setAttendance] = useState<AttendanceReport | null>(null);
  const [leave, setLeave] = useState<LeaveReport | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    const range = toQuery({ from, to });
    const request = tab === "headcount"
      ? api<HeadcountReport>("/api/reports/headcount").then((result) => { if (active) setHeadcount(result); })
      : tab === "attendance"
        ? api<AttendanceReport>(`/api/reports/attendance?${range}`).then((result) => { if (active) setAttendance(result); })
        : api<LeaveReport>(`/api/reports/leave?${range}`).then((result) => { if (active) setLeave(result); });
    request.catch((caught: Error) => { if (active) setError(caught.message); });
    return () => { active = false; };
  }, [tab, from, to]);

  async function exportCsv() {
    try {
      const range = toQuery({ from, to });
      if (tab === "headcount") await downloadCsv("/api/reports/headcount.csv", "headcount.csv");
      if (tab === "attendance") await downloadCsv(`/api/reports/attendance.csv?${range}`, "attendance.csv");
      if (tab === "leave") await downloadCsv(`/api/reports/leave.csv?${range}`, "leave.csv");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Download failed.");
    }
  }

  return (
    <>
      <div className="crumb"><Link to="/">Home</Link> <span>›</span> Reports</div>
      <div className="page-head">
        <div className="btn-group">
          {(["headcount", "attendance", "leave"] as Tab[]).map((item) => (
            <button key={item} className={`btn ${tab === item ? "btn-primary" : "btn-outline-primary"}`} onClick={() => setParams({ tab: item })}>
              {item[0].toUpperCase() + item.slice(1)}
            </button>
          ))}
        </div>
        <div className="filters">
          {tab !== "headcount" && (
            <>
              <input className="form-control" type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
              <input className="form-control" type="date" value={to} onChange={(event) => setTo(event.target.value)} />
            </>
          )}
          <button className="btn btn-outline-secondary" onClick={exportCsv}>Export CSV</button>
        </div>
      </div>
      <ErrorNote message={error} />
      {tab === "headcount" && headcount && (
        <>
          <div className="stat-grid mb-3">
            <Mini label="Total" value={headcount.total} />
            <Mini label="Active" value={headcount.active} />
            <Mini label="Probation" value={headcount.probation} />
            <Mini label="On leave" value={headcount.onLeave} />
          </div>
          <DataGrid data={headcount.byDepartment} empty="No departments.">
            <Column title="Department" data="name" />
            <Column title="Code" data="code" />
            <Column title="Employees" data="count" />
          </DataGrid>
        </>
      )}
      {tab === "attendance" && attendance && (
        <>
          <p className="muted mb-2">{attendance.records} records · average {attendance.averageHours} hours</p>
          <DataGrid data={attendance.rows} empty="No attendance in this range.">
            <Column title="Date" data="workDate" />
            <Column title="Employee" data="employeeName" />
            <Column title="Department" data="department" />
            <Column title="In" data="checkIn">{(value: string | null) => value ? value.slice(11, 16) : "—"}</Column>
            <Column title="Out" data="checkOut">{(value: string | null) => value ? value.slice(11, 16) : "—"}</Column>
            <Column title="Hours" data="hours">{(value: number | null) => value ?? "—"}</Column>
          </DataGrid>
        </>
      )}
      {tab === "leave" && leave && (
        <>
          <p className="muted mb-2">Pending {leave.pending} · Approved {leave.approved} · Rejected {leave.rejected} · {leave.totalDays} approved days</p>
          <DataGrid data={leave.rows} empty="No leave in this range.">
            <Column title="Employee" data="employeeName" />
            <Column title="Type" data="leaveType" />
            <Column title="Dates" data="startDate">{(_value: string, _type: string, row: LeaveRequest) => `${row.startDate} → ${row.endDate}`}</Column>
            <Column title="Days" data="days" />
            <Column title="Status" data="status">{(value: string) => <StatusBadge status={value} />}</Column>
          </DataGrid>
        </>
      )}
    </>
  );
}

function Mini({ label, value }: { label: string; value: number }) {
  return <article className="panel"><div className="muted">{label}</div><div className="stat-value text-dark">{value}</div></article>;
}

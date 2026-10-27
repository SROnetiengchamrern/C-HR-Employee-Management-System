import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import { useAuth } from "../auth";
import { Column, DataGrid } from "../components/DataGrid";
import { ErrorNote } from "../components/Ui";
import { formatWhen, monthStartIso, todayIso, toQuery } from "../lib/format";
import type { AttendanceRow, Department, Employee, Paged } from "../types";

export function AttendancePage() {
  const { user, isHr } = useAuth();
  const [rows, setRows] = useState<AttendanceRow[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [from, setFrom] = useState(monthStartIso());
  const [to, setTo] = useState(todayIso());
  const [departmentId, setDepartmentId] = useState("");
  const [employeeId, setEmployeeId] = useState(user?.employeeId ? String(user.employeeId) : "");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  async function load(nextFrom = from, nextTo = to, nextEmployee = employeeId, nextDepartment = departmentId) {
    const query = toQuery({
      from: nextFrom,
      to: nextTo,
      employeeId: isHr ? nextEmployee : undefined,
      departmentId: isHr ? nextDepartment : undefined
    });
    setRows(await api<AttendanceRow[]>(`/api/attendance?${query}`));
  }

  useEffect(() => {
    load().catch((caught: Error) => setError(caught.message));
    if (isHr) {
      api<Department[]>("/api/departments").then(setDepartments).catch(() => undefined);
      api<Paged<Employee>>("/api/employees?pageSize=1000").then((page) => setEmployees(page.items)).catch(() => undefined);
    }
    // Initial history load for the signed-in employee.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isHr]);

  const focusId = employeeId || (user?.employeeId != null ? String(user.employeeId) : "");
  const todayRow = rows.find((row) => row.workDate === todayIso() && String(row.employeeId) === focusId);

  async function punch(path: "check-in" | "check-out") {
    setBusy(true);
    setError("");
    setMessage("");
    try {
      await api(`/api/attendance/${path}`, {
        method: "POST",
        body: JSON.stringify({ employeeId: employeeId ? Number(employeeId) : null })
      });
      setMessage(path === "check-in" ? "Checked in." : "Checked out.");
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Attendance could not be saved.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <div className="crumb"><Link to="/">Home</Link> <span>›</span> Attendance</div>
      <ErrorNote message={error} />
      {message && <div className="alert alert-success">{message}</div>}
      <section className="panel mb-3">
        <div className="panel-head"><h2>Today</h2></div>
        <p className="muted mb-3">
          {todayRow?.checkIn ? `Checked in ${formatWhen(todayRow.checkIn)}` : "Not checked in yet."}
          {todayRow?.checkOut ? ` · Checked out ${formatWhen(todayRow.checkOut)}` : ""}
        </p>
        {isHr && (
          <select className="form-select mb-3" style={{ maxWidth: 360 }} value={employeeId} onChange={(event) => setEmployeeId(event.target.value)}>
            <option value="">My linked profile</option>
            {employees.map((employee) => <option key={employee.id} value={employee.id}>{employee.employeeCode} · {employee.fullName}</option>)}
          </select>
        )}
        <div className="d-flex gap-2">
          <button className="btn btn-primary" disabled={busy || Boolean(todayRow?.checkIn)} onClick={() => punch("check-in")}>Check in</button>
          <button className="btn btn-success" disabled={busy || !todayRow?.checkIn || Boolean(todayRow?.checkOut)} onClick={() => punch("check-out")}>Check out</button>
        </div>
      </section>
      <form className="filters mb-3" onSubmit={(event) => { event.preventDefault(); load().catch((caught: Error) => setError(caught.message)); }}>
        <input className="form-control" type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
        <input className="form-control" type="date" value={to} onChange={(event) => setTo(event.target.value)} />
        {isHr && (
          <select className="form-select" value={departmentId} onChange={(event) => setDepartmentId(event.target.value)}>
            <option value="">All departments</option>
            {departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
          </select>
        )}
        <button className="btn btn-outline-primary">Show history</button>
      </form>
      <DataGrid data={rows} empty="No attendance in this range.">
        <Column title="Date" data="workDate" />
        <Column title="Employee" data={(row: AttendanceRow) => `${row.employeeName} ${row.employeeCode}`}>
          {(_value: string, _type: string, row: AttendanceRow) => (
            <>{row.employeeName}<div className="muted">{row.employeeCode}</div></>
          )}
        </Column>
        <Column title="Department" data="department" />
        <Column title="Check in" data="checkIn">{(value: string | null) => formatWhen(value)}</Column>
        <Column title="Check out" data="checkOut">{(value: string | null) => formatWhen(value)}</Column>
        <Column title="Hours" data="hours">{(value: number | null) => value ?? "—"}</Column>
      </DataGrid>
    </>
  );
}

import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { api } from "../api";
import { useAuth } from "../auth";
import { Column, DataGrid } from "../components/DataGrid";
import { ErrorNote, Modal, StatusBadge } from "../components/Ui";
import { toQuery } from "../lib/format";
import type { Department, Employee, EmployeeDetail, EmploymentStatus, Paged, Position } from "../types";

const statuses: EmploymentStatus[] = ["Active", "Probation", "OnLeave", "Resigned", "Terminated"];
const noEmployees: Employee[] = [];
const emptyForm = {
  firstName: "",
  lastName: "",
  email: "",
  phone: "",
  dateOfBirth: "",
  hireDate: "",
  status: "Active",
  departmentId: "",
  positionId: "",
  managerId: "",
  createLogin: false,
  password: ""
};

export function EmployeesPage() {
  const { isHr } = useAuth();
  const [params, setParams] = useSearchParams();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [positions, setPositions] = useState<Position[]>([]);
  const [page, setPage] = useState<Paged<Employee> | null>(null);
  const [directory, setDirectory] = useState<Employee[]>([]);
  const [reloadToken, setReloadToken] = useState(0);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState<Employee | null>(null);
  const [creating, setCreating] = useState(params.get("new") === "1");
  const [detail, setDetail] = useState<EmployeeDetail | null>(null);
  const [form, setForm] = useState(emptyForm);
  const departmentId = params.get("departmentId") ?? "";
  const status = params.get("status") ?? "";
  const actions = useRef<{ openDetail: (id: number) => void; openEdit: (employee: Employee) => void; isHr: boolean }>({
    openDetail: () => undefined,
    openEdit: () => undefined,
    isHr
  });
  actions.current.isHr = isHr;

  useEffect(() => {
    api<Department[]>("/api/departments").then(setDepartments).catch((caught: Error) => setError(caught.message));
    api<Position[]>("/api/positions").then(setPositions).catch((caught: Error) => setError(caught.message));
  }, []);

  useEffect(() => {
    let active = true;
    const query = toQuery({ departmentId, status, page: 1, pageSize: 1000 });
    api<Paged<Employee>>(`/api/employees?${query}`)
      .then((result) => { if (active) setPage(result); })
      .catch((caught: Error) => { if (active) setError(caught.message); });
    api<Paged<Employee>>("/api/employees?pageSize=1000")
      .then((result) => { if (active) setDirectory(result.items); })
      .catch(() => undefined);
    return () => { active = false; };
  }, [departmentId, status, reloadToken]);

  const filteredPositions = useMemo(
    () => positions.filter((position) => String(position.departmentId) === form.departmentId),
    [form.departmentId, positions]
  );

  function updateQuery(next: Record<string, string>) {
    const updated = new URLSearchParams(params);
    for (const [key, value] of Object.entries(next)) {
      if (value) updated.set(key, value);
      else updated.delete(key);
    }
    if (!("page" in next)) updated.delete("page");
    setParams(updated);
  }

  function openCreate() {
    setEditing(null);
    setForm({
      ...emptyForm,
      departmentId: departmentId || String(departments[0]?.id ?? ""),
      hireDate: new Date().toISOString().slice(0, 10)
    });
    setCreating(true);
  }

  function openEdit(employee: Employee) {
    setCreating(false);
    setEditing(employee);
    setForm({
      firstName: employee.firstName,
      lastName: employee.lastName,
      email: employee.email,
      phone: employee.phone ?? "",
      dateOfBirth: employee.dateOfBirth,
      hireDate: employee.hireDate,
      status: employee.status,
      departmentId: String(employee.departmentId),
      positionId: String(employee.positionId),
      managerId: employee.managerId ? String(employee.managerId) : "",
      createLogin: false,
      password: ""
    });
  }

  async function openDetail(id: number) {
    try {
      setDetail(await api<EmployeeDetail>(`/api/employees/${id}`));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not load the profile.");
    }
  }

  actions.current.openDetail = openDetail;
  actions.current.openEdit = openEdit;

  async function save(event: FormEvent) {
    event.preventDefault();
    const payload = {
      ...form,
      departmentId: Number(form.departmentId),
      positionId: Number(form.positionId),
      managerId: form.managerId ? Number(form.managerId) : null,
      phone: form.phone || null,
      password: form.password || null
    };
    try {
      if (editing) await api(`/api/employees/${editing.id}`, { method: "PUT", body: JSON.stringify(payload) });
      else await api("/api/employees", { method: "POST", body: JSON.stringify(payload) });
      setCreating(false);
      setEditing(null);
      setReloadToken((current) => current + 1);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not save the employee.");
    }
  }

  return (
    <>
      <div className="crumb"><Link to="/">Home</Link> <span>›</span> People Management</div>
      <div className="page-head">
        <div className="filters">
          <select className="form-select" value={departmentId} onChange={(event) => updateQuery({ departmentId: event.target.value })}>
            <option value="">All departments</option>
            {departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
          </select>
          <select className="form-select" value={status} onChange={(event) => updateQuery({ status: event.target.value })}>
            <option value="">All statuses</option>
            {statuses.map((item) => <option key={item}>{item}</option>)}
          </select>
        </div>
        {isHr && <button className="btn btn-primary" onClick={openCreate}>Add Employee</button>}
      </div>
      <ErrorNote message={error} />
      <DataGrid data={page?.items ?? noEmployees} empty="No employees match these filters.">
        <Column title="Code" data="employeeCode" />
        <Column title="Employee" data={(row: Employee) => `${row.fullName} ${row.email} ${row.employeeCode}`}>
          {(_value: string, _type: string, row: Employee) => (
            <>
              <button className="text-btn" onClick={() => actions.current.openDetail(row.id)}>{row.fullName}</button>
              <div className="muted">{row.email}</div>
            </>
          )}
        </Column>
        <Column title="Department" data="department" />
        <Column title="Position" data="position" />
        <Column title="Status" data="status">
          {(value: string) => <StatusBadge status={value} />}
        </Column>
        <Column title="Hired" data="hireDate" />
        <Column title="" options={{ orderable: false, searchable: false }}>
          {(_value: unknown, type: string, row: Employee) => type !== "display" ? "" : actions.current.isHr && (
            <button className="btn btn-sm btn-outline-secondary" onClick={() => actions.current.openEdit(row)}>Edit</button>
          )}
        </Column>
      </DataGrid>

      {(creating || editing) && (
        <Modal title={editing ? `Edit ${editing.fullName}` : "Add employee"} onClose={() => { setCreating(false); setEditing(null); }} footer={
          <>
            <button className="btn btn-outline-secondary" onClick={() => { setCreating(false); setEditing(null); }}>Cancel</button>
            <button className="btn btn-primary" form="employee-form">Save</button>
          </>
        }>
          <form id="employee-form" className="row g-3" onSubmit={save}>
            <div className="col-md-6"><label className="form-label">First name</label><input className="form-control" required value={form.firstName} onChange={(event) => setForm({ ...form, firstName: event.target.value })} /></div>
            <div className="col-md-6"><label className="form-label">Last name</label><input className="form-control" required value={form.lastName} onChange={(event) => setForm({ ...form, lastName: event.target.value })} /></div>
            <div className="col-md-6"><label className="form-label">Email</label><input className="form-control" type="email" required value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></div>
            <div className="col-md-6"><label className="form-label">Phone</label><input className="form-control" value={form.phone} onChange={(event) => setForm({ ...form, phone: event.target.value })} /></div>
            <div className="col-md-6"><label className="form-label">Date of birth</label><input className="form-control" type="date" required value={form.dateOfBirth} onChange={(event) => setForm({ ...form, dateOfBirth: event.target.value })} /></div>
            <div className="col-md-6"><label className="form-label">Hire date</label><input className="form-control" type="date" required value={form.hireDate} onChange={(event) => setForm({ ...form, hireDate: event.target.value })} /></div>
            <div className="col-md-6">
              <label className="form-label">Department</label>
              <select className="form-select" required value={form.departmentId} onChange={(event) => setForm({ ...form, departmentId: event.target.value, positionId: "" })}>
                <option value="">Select</option>
                {departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
              </select>
            </div>
            <div className="col-md-6">
              <label className="form-label">Position</label>
              <select className="form-select" required value={form.positionId} onChange={(event) => setForm({ ...form, positionId: event.target.value })}>
                <option value="">Select</option>
                {filteredPositions.map((position) => <option key={position.id} value={position.id}>{position.title}</option>)}
              </select>
            </div>
            <div className="col-md-6">
              <label className="form-label">Status</label>
              <select className="form-select" value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}>
                {statuses.map((item) => <option key={item}>{item}</option>)}
              </select>
            </div>
            <div className="col-md-6">
              <label className="form-label">Manager</label>
              <select className="form-select" value={form.managerId} onChange={(event) => setForm({ ...form, managerId: event.target.value })}>
                <option value="">None</option>
                {directory.filter((employee) => employee.id !== editing?.id).map((employee) => <option key={employee.id} value={employee.id}>{employee.fullName}</option>)}
              </select>
            </div>
            {!editing && (
              <div className="col-12">
                <div className="form-check">
                  <input id="createLogin" className="form-check-input" type="checkbox" checked={form.createLogin} onChange={(event) => setForm({ ...form, createLogin: event.target.checked })} />
                  <label className="form-check-label" htmlFor="createLogin">Create an employee login</label>
                </div>
              </div>
            )}
            {form.createLogin && <div className="col-md-6"><label className="form-label">Temporary password</label><input className="form-control" type="password" placeholder="Employee@123456" value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} /></div>}
          </form>
        </Modal>
      )}

      {detail && (
        <Modal title={detail.employee.fullName} onClose={() => setDetail(null)}>
          <div className="profile">
            <div className="profile-side">
              <div className="avatar-lg mx-auto">{detail.employee.firstName[0]}{detail.employee.lastName[0]}</div>
              <h3 className="mt-3">{detail.employee.fullName}</h3>
              <div className="muted">{detail.employee.employeeCode}</div>
              <div className="mt-2"><StatusBadge status={detail.employee.status} /></div>
            </div>
            <div>
              <div className="kv">
                <span>Email</span><strong>{detail.employee.email}</strong>
                <span>Phone</span><strong>{detail.employee.phone ?? "—"}</strong>
                <span>Department</span><strong>{detail.employee.department}</strong>
                <span>Position</span><strong>{detail.employee.position}</strong>
                <span>Manager</span><strong>{detail.employee.managerName ?? "—"}</strong>
                <span>Hired</span><strong>{detail.employee.hireDate}</strong>
                <span>Birthday</span><strong>{detail.employee.dateOfBirth}</strong>
              </div>
              <h3 className="mini-title">Leave balance this year</h3>
              {detail.leaveBalances.map((balance) => (
                <div className="leave-mini" key={balance.leaveTypeId}>
                  <span>{balance.leaveType}</span>
                  <strong>{balance.usedDays} / {balance.allowance} days</strong>
                </div>
              ))}
            </div>
          </div>
        </Modal>
      )}
    </>
  );
}
